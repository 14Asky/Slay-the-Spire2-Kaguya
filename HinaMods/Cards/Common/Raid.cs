using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.ValueProps;
using Kaguya.HinaMods;
using Kaguya.HinaMods.Cards;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.SupportCards.Common;

/// <summary>
/// 突袭（Raid）
/// 本地化键：KAGUYA-GENERATE_SUPPORT_STRIKE.title / .description
/// 1费 · 攻击牌 · 普通 · 目标：单个敌人 · 标签：支援牌
/// 卡面：造成6点伤害。将1张[blue]支援打击[/blue]加入[gold]手牌[/gold]。
/// 代码实测：Damage=6
/// 升级：Damage +1（6 → 7）；获得的支援打击为【升级后】的。
/// </summary>
public sealed class Raid : HinaModsCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(6m, ValueProp.Move)
    };

    /// <summary>支援牌标签（本牌会获得支援牌）。</summary>
    public override HashSet<string> CustomTags => new HashSet<string>() { CustomCardTags.SUPPORTCARD };

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
           HoverTipFactory.FromCard<SupportStrike>()
        };
    }

    public Raid()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 造成伤害
        await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);

        // 2. 获得 1 张支援打击
        CardModel supportCard = ModelDb.Card<SupportStrike>();
        if (supportCard == null) return;

        CardModel newCard = CombatState.CreateCard(supportCard, Owner);

        // 升级版：获得的支援打击自带升级
        if (IsUpgraded && newCard.IsMutable && newCard.IsUpgradable && !newCard.IsUpgraded)
        {
            newCard.UpgradeInternal();
            newCard.FinalizeUpgradeInternal();
        }

        await CardPileCmd.AddGeneratedCardToCombat(newCard, PileType.Hand, Owner);
    }

    /// <summary>升级：伤害 6 → 7。</summary>
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1m);
    }
}

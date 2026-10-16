using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.SupportCards.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 协同作战（CoordinatedOps）
/// 本地化键：KAGUYA-SUPPORT_STRIKE_AURA_CARD.title / .description
/// 1费 · 攻击牌 · 罕见 · 目标：单个敌人 · 标签：支援牌
/// 卡面：造成{Damage:diff()}点伤害。获得1张[blue]支援打击[/blue]和1张[blue]招架[/blue]，
///       并将1张[blue]疾驰[/blue]加入[gold]抽牌堆[/gold]。
/// 代码实测：Damage=5
/// 升级：Damage +3（5→8）
/// </summary>
public sealed class CoordinatedOps : HinaModsCard
{
    /// <summary>支援牌标签（本牌会获得支援牌）。</summary>
    public override HashSet<string> CustomTags => new HashSet<string>() { CustomCardTags.SUPPORTCARD };

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(5m, ValueProp.Move)];

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            HoverTipFactory.FromCard<SupportStrike>(),
            HoverTipFactory.FromCard<SupportParry>(),
            HoverTipFactory.FromCard<SupportDash>()
        };
    }

    public CoordinatedOps()
        : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);

        // 获得 1 张支援打击 + 1 张招架（加入手牌）
        await SupportStrike.CreateInHand(Owner, CombatState);
        await SupportParry.CreateInHand(Owner, CombatState);

        // 将 1 张疾驰洗入抽牌堆
        CardModel dash = CombatState.CreateCard<SupportDash>(Owner);
        await CardPileCmd.AddGeneratedCardToCombat(dash, PileType.Draw, Owner, CardPilePosition.Random);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}

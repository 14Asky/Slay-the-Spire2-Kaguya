using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.ValueProps;
// 请按你实际的满月横扫卡牌命名空间修改
using Kaguya.HinaMods.Cards;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 下冲（DownRush）
/// 本地化键：KAGUYA-FULL_MOON_STRIKE.title / .description
/// 2费 · 攻击牌 · 普通 · 目标：全体敌人
/// 卡面：对所有敌人造成10点伤害。
///       将1张[blue]乱舞[/blue]加入[gold]手牌[/gold]。
/// 代码实测：Damage=10
/// 升级：Damage +3
/// </summary>
public sealed class DownRush : HinaModsCard
{

    /// <summary>支援牌标签（本牌会获得支援牌）。</summary>
    public override HashSet<string> CustomTags => new HashSet<string>() { CustomCardTags.SUPPORTCARD };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(6m, ValueProp.Move)
    };

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            HoverTipFactory.FromCard<WildDance>()
        };
    }

    // 构造：1费 攻击牌 普通 全体目标
    public DownRush()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
    { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 对全体敌人造成伤害
        await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);

        // 2. 生成 满月横扫 并加入手牌
        CardModel fullMoonCard = CardScope.CreateCard<WildDance>(Owner);
        await CardPileCmd.AddGeneratedCardToCombat(fullMoonCard, PileType.Hand, Owner);
    }

    // 升级：伤害 6 → 8
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
    }
}

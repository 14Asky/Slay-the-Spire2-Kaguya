using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Legacy.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.ValueProps;
// 请按你实际的满月横扫卡牌命名空间修改
using Kaguya.HinaMods.Legacy.Cards;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Cards;

/// <summary>
/// 满月冲击
/// 2费 攻击牌 | 全体目标
/// 对所有敌人造成3点伤害，将1张【满月横扫】加入手牌。
/// 升级：对所有敌人造成5点伤害，获得的满月横扫会升级。
/// </summary>
public sealed class LegacyFullMoonStrike : HinaModsCard
{
    // 基础群体伤害：3点
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(8m, ValueProp.Move)
    };

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            HoverTipFactory.FromCard<LegacyFullMoonSweepCard>()
        };
    }

    // 构造：2费 攻击牌 普通 全体目标
    public LegacyFullMoonStrike()
        : base(2, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
    { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 对全体敌人造成伤害
        await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);

        // 2. 生成 满月横扫 并加入手牌
        CardModel fullMoonCard = CardScope.CreateCard<LegacyFullMoonSweepCard>(Owner);
        await CardPileCmd.Add(fullMoonCard, PileType.Hand);
    }

    // 升级：伤害 3 → 5（+2）
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(5m);
    }
}

using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards.Uncommon;

/// <summary>
/// 月扉（MoonGate）
/// 本地化键：KAGUYA-MOON_GATE.title / .description
/// 1费 · 攻击牌 · 罕见 · 目标：全体敌人 · 无关键词
/// 卡面：对所有敌人造成{Damage:diff()}点伤害。
///       随机将一张可以获得[gold]月夜[/gold]的牌从抽牌堆/弃牌堆/消耗堆移到手牌。
/// 升级：伤害 +3（9 → 12）
/// 检索池：<see cref="MoonlightPool.FromCombatPiles"/>（按当前战斗牌堆实时判定，不再是硬编码清单）
/// </summary>
public sealed class MoonGate : HinaModsCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(9m, ValueProp.Move)
    ];

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            HoverTipFactory.FromPower<MoonlightPower>()
        };
    }

    public MoonGate()
        : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
    { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(CombatState)
            .Execute(choiceContext);

        // 从 抽牌堆 / 弃牌堆 / 消耗堆 里随机抽一张「可以获得月夜」的牌到手牌
        List<CardModel> candidates = MoonlightPool.FromCombatPiles(Owner);
        if (candidates.Count == 0)
            return;

        CardModel? picked = Owner.RunState.Rng.CombatCardSelection.NextItem(candidates);
        if (picked == null)
            return;

        await CardPileCmd.Add(picked, PileType.Hand, CardPilePosition.Top);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}

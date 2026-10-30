using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Kaguya.HinaMods;
using Kaguya.HinaMods.Cards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards.Uncommon;

/// <summary>
/// 完美连击（PerfectCombo）
/// 本地化键：KAGUYA-SINGER_COMB.title / .description
/// 1费 · 攻击牌 · 稀有 · 目标：随机敌人 · 关键词：歌声
/// 卡面：对随机敌人造成5点伤害1次。
///       本场战斗中每打出过1次这张牌，攻击次数+1。{InCombat:（本次命中{CalculatedHits:diff()}次）|}
/// 代码实测：Damage=5；CalculationBase=1
/// 升级：Damage +1（5 → 6）
/// </summary>
public class PerfectCombo : HinaModsCard
{
    private const string _calculatedHitsKey = "CalculatedHits";

    /// <summary>歌声关键词（打出后进消耗堆，下回合抽牌前洗回抽牌堆）。</summary>
    public override HashSet<string> CustomTags => new HashSet<string>() { CustomCardTags.ECHO };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        // 基础伤害：5点
        new DamageVar(5m, ValueProp.Move),
        // 基础命中次数：1次
        new CalculationBaseVar(1m),
        // 每次额外命中：1次
        new CalculationExtraVar(1m),
        // 统计本场战斗中这张牌被打出过的次数
        new CalculatedVar(_calculatedHitsKey).WithMultiplier(static (CardModel card, Creature _) =>
            CombatManager.Instance.History.Entries
                .OfType<CardPlayFinishedEntry>()
                .Count(e => e.CardPlay.Card.Owner == card.Owner && e.CardPlay.Card.Id == card.Id)
        )
    };

    public PerfectCombo()
        : base(1, CardType.Attack, CardRarity.Rare, TargetType.RandomEnemy)
    { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int hitCount = (int)((CalculatedVar)base.DynamicVars[_calculatedHitsKey]).Calculate(Owner.Creature);

        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
            .WithHitCount(hitCount)
            .FromCard(this, cardPlay)
            .TargetingRandomOpponents(base.CombatState)
            .WithHitFx("vfx/vfx_attack_slash", null, SfxHitName)   // 每段命中都响声
            .Execute(choiceContext);
    }

    /// <summary>升级：伤害 5 → 6（+1）。</summary>
    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(1m);
    }
}

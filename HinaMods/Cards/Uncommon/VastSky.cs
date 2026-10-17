using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 长空（VastSky）
/// 本地化键：KAGUYA-VAST_SKY.title / .description
/// 2费 · 攻击牌 · 罕见 · 目标：单个敌人 · 无关键词
/// 卡面：造成{Damage:diff()}点伤害，对其余敌人造成本次伤害一半的伤害。
///       此牌受到[gold]力量[/gold]的加成为{StrengthMult:diff()}倍。
/// 卡图：dragon_ride.png
/// 代码实测：CalculationBase=12；ExtraDamage=2（倍率-1）；StrengthMult=3
/// 升级：伤害 12→15；力量倍率 3→5
/// </summary>
public sealed class VastSky : HinaModsCard
{
    private const string StrengthMultKey = "StrengthMult";

    /// <summary>基础：本牌受到的力量加成倍率。</summary>
    private const decimal StrengthMultiplier = 3m;

    /// <summary>升级后：本牌受到的力量加成倍率。</summary>
    private const decimal UpgradedStrengthMultiplier = 5m;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        // CalculatedDamage = 基础值 + ExtraDamage × 力量层数
        //   ExtraDamage = 倍率 - 1（基础 2 份 / 升级 4 份）
        //   主目标走伤害管线时还会再加 1 次力量
        //   ⇒ 最终伤害 = 基础值 + 力量 × 倍率（升级前 ×3、升级后 ×5）
        new CalculationBaseVar(12m),
        new ExtraDamageVar(StrengthMultiplier - 1m),
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier(StrengthExtraDamage),
        new DynamicVar(StrengthMultKey, StrengthMultiplier)
    ];

    /// <summary>
    /// 乘数只取【力量层数】；「额外的份数」由 ExtraDamage（= 倍率-1）承担。
    /// ⚠ 不能在这里再乘一次 (倍率-1)：那会和 ExtraDamage 相乘变成 (倍率-1)²，
    ///   3 点力量 + 升级后 5 倍会算成 15 + 4×(3×4) = 63，再被管线加 3 力量 → 66（应为 30）。
    /// </summary>
    private static decimal StrengthExtraDamage(CardModel card, Creature _)
    {
        return card.Owner.Creature.GetPowerAmount<StrengthPower>();
    }

    public VastSky()
        : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null)
            return;

        // 主目标：走正常伤害管线（力量会再叠 1 次，凑成「原来的 N 倍」）
        await DamageCmd.Attack(DynamicVars.CalculatedDamage)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);

        // 本次伤害 = 基础值 + 力量 × 倍率
        int strength = Owner.Creature.GetPowerAmount<StrengthPower>();
        decimal mainDamage = DynamicVars.CalculatedDamage.Calculate(cardPlay.Target) + strength;
        decimal splash = decimal.Floor(mainDamage / 2m);
        if (splash <= 0m)
            return;

        List<Creature> others = CombatState.HittableEnemies
            .Where(e => e != cardPlay.Target)
            .ToList();
        if (others.Count == 0)
            return;

        // 溅射伤害里已包含力量，故标 Unpowered 避免被重复叠加
        await CreatureCmd.Damage(
            choiceContext,
            others,
            splash,
            ValueProp.Move | ValueProp.Unpowered,
            Owner.Creature,
            this,
            cardPlay);
    }

    protected override void OnUpgrade()
    {
        // 12 → 15
        DynamicVars.CalculationBase.UpgradeValueBy(3m);
        // 额外力量份数 1 → 2
        DynamicVars.ExtraDamage.UpgradeValueBy(UpgradedStrengthMultiplier - StrengthMultiplier);
        // 展示用倍率 2 → 3
        DynamicVars[StrengthMultKey].UpgradeValueBy(UpgradedStrengthMultiplier - StrengthMultiplier);
    }
}

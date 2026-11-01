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
/// 卡面：造成{CalculatedDamage:diff()}点伤害，对其余敌人造成本次伤害一半的伤害。
///       此牌受到[gold]力量[/gold]的加成为{StrengthMult:diff()}倍。
/// 卡图：dragon_ride.png
/// 代码实测：CalculationBase=12；ExtraDamage=3（= 完整倍率）；StrengthMult=3；管线 Unpowered
/// 实际伤害 = 基础 + 力量 × 倍率（例：基础 20、力量 5 → 35），卡面显示值与实际一致
/// 升级：基础 12→15；力量倍率 3→5
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
        // CalculatedDamage = 基础值 + 力量 × 倍率
        //   ExtraDamage 直接取完整倍率（基础 3 份 / 升级 5 份）
        //   伤害管线用 Unpowered，不再额外叠一次力量
        //   ⇒ 卡面显示值 == 实际伤害（基础 20、力量 5 时就是 35）
        new CalculationBaseVar(12m),
        new ExtraDamageVar(StrengthMultiplier),
        new CalculatedDamageVar(ValueProp.Move | ValueProp.Unpowered).WithMultiplier(StrengthExtraDamage),
        new DynamicVar(StrengthMultKey, StrengthMultiplier)
    ];

    /// <summary>
    /// 乘数只取【力量层数】；完整倍率由 ExtraDamage(=3 / 升级 5) 承担，管线用 Unpowered 不再叠加力量。
    /// ⚠ 不能在这里再乘倍率：那会和 ExtraDamage 相乘变成 倍率²，伤害会炸。
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

        // 主目标：CalculatedDamage 已含力量（管线 Unpowered，不会再加）
        await DamageCmd.Attack(DynamicVars.CalculatedDamage)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);

        // 本次伤害就是卡面显示值（基础 + 力量 × 倍率）
        decimal mainDamage = DynamicVars.CalculatedDamage.Calculate(cardPlay.Target);
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
        // 力量倍率 3 → 5
        DynamicVars.ExtraDamage.UpgradeValueBy(UpgradedStrengthMultiplier - StrengthMultiplier);
        // 展示用倍率 3 → 5
        DynamicVars[StrengthMultKey].UpgradeValueBy(UpgradedStrengthMultiplier - StrengthMultiplier);
    }
}

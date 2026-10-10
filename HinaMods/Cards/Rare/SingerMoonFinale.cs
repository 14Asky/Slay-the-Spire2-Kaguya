using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 月夜终曲
/// 2费 攻击牌 | 单体目标
/// 消耗5层月夜，造成35点伤害。
/// 若再消耗10层月夜，对目标施加5层易伤和5层虚弱。
/// 若再消耗10层月夜，眩晕目标。
/// 升级：伤害变为45点。
/// </summary>
public sealed class SingerMoonFinale : HinaModsCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(35m, ValueProp.Move),
        new PowerVar<VulnerablePower>(5m),
        new PowerVar<WeakPower>(5m)
    };

    public SingerMoonFinale()
        : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    { }

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
           HoverTipFactory.FromPower<VulnerablePower>(),
            HoverTipFactory.FromPower<WeakPower>(),
            HoverTipFactory.FromPower<FortunePower>()
        };
    }

    protected override bool IsPlayable
    {
        get
        {
            if (!base.IsPlayable)
                return false;

            FortunePower fortunePower = Owner.Creature.GetPower<FortunePower>();
            return fortunePower != null && fortunePower.Amount >= 5;
        }
    }

    protected override bool ShouldGlowGoldInternal => IsPlayable;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        decimal damage = DynamicVars.Damage.BaseValue;
        decimal vulnerable = DynamicVars[nameof(VulnerablePower)].BaseValue;
        decimal weak = DynamicVars[nameof(WeakPower)].BaseValue;

        Creature target = cardPlay.Target;
        FortunePower moonPower = Owner.Creature.GetPower<FortunePower>();
        int currentMoon = moonPower?.Amount ?? 0;

        if (currentMoon >= 5)
        {
            // 修复：添加 choiceContext 参数
            await PowerCmd.ModifyAmount(
                choiceContext,
                moonPower,
                -5m,
                Owner.Creature,
                this);
            await DamageCmd.Attack(damage)
                .FromCard(this, cardPlay)
                .Targeting(target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
            currentMoon -= 5;
        }

        if (currentMoon >= 10)
        {
            // 修复：添加 choiceContext 参数
            await PowerCmd.ModifyAmount(
                choiceContext,
                moonPower,
                -10m,
                Owner.Creature,
                this);
            // 修复：添加 choiceContext 参数
            await PowerCmd.Apply<VulnerablePower>(
                choiceContext,
                target,
                vulnerable,
                Owner.Creature,
                this);
            // 修复：添加 choiceContext 参数
            await PowerCmd.Apply<WeakPower>(
                choiceContext,
                target,
                weak,
                Owner.Creature,
                this);
            currentMoon -= 10;
        }

        if (currentMoon >= 10)
        {
            // 修复：添加 choiceContext 参数
            await PowerCmd.ModifyAmount(
                choiceContext,
                moonPower,
                -10m,
                Owner.Creature,
                this);
            await CreatureCmd.Stun(target);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(10m);
    }
}
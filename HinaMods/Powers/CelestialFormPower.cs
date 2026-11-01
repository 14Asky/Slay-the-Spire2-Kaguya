using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Powers;

/// <summary>
/// 天人形态（CelestialFormPower）
/// 本地化键：KAGUYA-CELESTIAL_FORM_POWER.title / .description
/// 卡面：每当你获得[gold]月夜[/gold]时，对所有敌方造成等层数的伤害。
///       每回合开始时获得{Amount}层[gold]月夜[/gold]。
/// </summary>
public sealed class CelestialFormPower : CustomPowerModel
{
    public override string CustomPackedIconPath => "res://images/hinamods/Powers/tsukimi_form.png";
    public override string CustomBigIconPath => "res://images/hinamods/Powers/tsukimi_form.png";

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerInstanceType InstanceType => PowerInstanceType.None;
    public override bool AllowNegative => false;
    public override int DisplayAmount => (int)Amount;

    /// <summary>正在结算「等层数伤害」期间置位，避免被自身触发的月夜变化递归。</summary>
    private bool _isDealingDamage;

    /// <summary>每当你【获得】月夜时，对所有敌方造成等层数的伤害。</summary>
    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature applier, CardModel cardSource)
    {
        await base.AfterPowerAmountChanged(choiceContext, power, amount, applier, cardSource);

        if (Owner == null || _isDealingDamage) return;
        if (power is not MoonlightPower || power.Owner != Owner) return;
        if (amount <= 0m) return;

        IReadOnlyList<Creature>? enemies = Owner.CombatState?.HittableEnemies;
        if (enemies == null || enemies.Count == 0) return;

        _isDealingDamage = true;
        try
        {
            Flash();
            await CreatureCmd.Damage(
                choiceContext: choiceContext,
                targets: enemies.ToList(),
                amount: amount,
                props: ValueProp.Unpowered,
                cardSource: null,
                dealer: Owner,
                cardPlay: null);
        }
        finally
        {
            _isDealingDamage = false;
        }
    }

    /// <summary>每回合开始时获得月夜。</summary>
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterPlayerTurnStart(choiceContext, player);

        if (Owner == null || Owner.Player != player || !Owner.IsAlive) return;

        await PowerCmd.Apply<MoonlightPower>(choiceContext, Owner, Amount, Owner, null, false);
    }
}

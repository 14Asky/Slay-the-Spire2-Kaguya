using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Powers;

/// <summary>
/// 注视（GazePower）
/// 本地化键：KAGUYA-GAZE_MOON_POWER.title / .description
/// 卡面：本回合内，你每造成一次伤害就获得{Amount}层[gold]月夜[/gold]。回合结束时移除。
/// </summary>
public sealed class GazePower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
    public override PowerInstanceType InstanceType => PowerInstanceType.None;
    public override int DisplayAmount => (int)Amount;

    public override string CustomPackedIconPath => "res://images/hinamods/Powers/gaze_moon_power.png";
    public override string CustomBigIconPath => "res://images/hinamods/Powers/gaze_moon_power.png";

    /// <summary>本回合内每造成一次伤害，就获得 Amount 层月夜。</summary>
    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature dealer, DamageResult result,
        ValueProp props, Creature target, CardModel cardSource)
    {
        await base.AfterDamageGiven(choiceContext, dealer, result, props, target, cardSource);

        if (Owner == null || !Owner.IsAlive)
            return;
        if (dealer != Owner || target == Owner)
            return;
        if (result.TotalDamage <= 0)
            return;

        await PowerCmd.Apply<MoonlightPower>(
            choiceContext,
            Owner,
            Amount,
            Owner,
            null);
    }

    /// <summary>回合结束移除（本回合内有效）。</summary>
    public override async Task AfterSideTurnEnd(PlayerChoiceContext ctx, CombatSide side, IEnumerable<Creature> participants)
    {
        await base.AfterSideTurnEnd(ctx, side, participants);

        if (Owner == null || side != Owner.Side)
            return;

        Flash();
        await PowerCmd.Remove(this);
    }
}

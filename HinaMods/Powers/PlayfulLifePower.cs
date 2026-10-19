using BaseLib.Abstracts;
using Kaguya.HinaMods.Cards;
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
/// 游戏人间（PlayfulLifePower）
/// 本地化键：KAGUYA-PLAYFUL_LIFE_POWER.title / .description
/// 效果：本回合每获得 1 层[gold]月夜[/gold]，获得等同于本能力层数的[gold]格挡[/gold]；回合结束时移除。
/// 层数：1（升级后 2）
/// </summary>
public sealed class PlayfulLifePower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerInstanceType InstanceType => PowerInstanceType.None;
    public override bool AllowNegative => false;
    public override int DisplayAmount => (int)Amount;

    // 沿用月夜的图标（暂无专属新图）
    public override string CustomPackedIconPath => "res://images/hinamods/Powers/fortune_power.png";
    public override string CustomBigIconPath => "res://images/hinamods/Powers/fortune_power.png";

    /// <summary>每次「获得月夜」都会走这里（amount 是本次变化量）。</summary>
    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (power is not MoonlightPower moon || moon.Owner != Owner)
            return;
        if (amount <= 0m || Amount <= 0)
            return;

        await CreatureCmd.GainBlock(Owner, amount * Amount, ValueProp.Unpowered, null, fast: true);
    }

    /// <summary>本回合有效：自己这侧回合结束时移除。</summary>
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        await base.AfterSideTurnEnd(choiceContext, side, participants);
        if (side == Owner.Side)
            await PowerCmd.Remove(this);
    }
}

using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using System;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Powers;

/// <summary>
/// 天人形态（CelestialFormPower）
/// 本地化键：KAGUYA-TSUKIMI_FORM.title / .description
/// 卡面：每当你获得[gold]月夜[/gold]时，额外获得该次获得量{CelestialFormPower:diff()}%的[gold]月夜[/gold]。
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

    /// <summary>
    /// 正在发放「额外月夜」期间置位。
    /// 额外月夜本身也会触发一次 AfterPowerAmountChanged，不设这道闸会无限递归。
    /// </summary>
    private bool _isGrantingBonus;

    /// <summary>
    /// 监听月夜层数变化：只在【获得】月夜（amount &gt; 0）时按比例额外再给。
    /// </summary>
    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature applier, CardModel cardSource)
    {
        await base.AfterPowerAmountChanged(choiceContext, power, amount, applier, cardSource);

        if (Owner == null || _isGrantingBonus) return;
        if (power is not MoonlightPower || power.Owner != this.Owner) return;
        if (amount <= 0m) return;

        int percent = (int)Amount;
        if (percent <= 0) return;

        int bonus = (int)Math.Floor(amount * percent / 100m);
        if (bonus <= 0) return;

        _isGrantingBonus = true;
        try
        {
            Flash();
            await PowerCmd.Apply<MoonlightPower>(choiceContext, Owner, bonus, Owner, null);
        }
        finally
        {
            _isGrantingBonus = false;
        }
    }
}

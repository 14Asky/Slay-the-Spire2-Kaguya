using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Powers;

public sealed class LegacyMoonlightBlockPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    public override string CustomPackedIconPath => "res://images/hinamods/Powers/moonlight_block.png";
    public override string CustomBigIconPath => "res://images/hinamods/Powers/moonlight_block.png";
    public override int DisplayAmount => (int)Amount;
    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.Static(StaticHoverTip.Block);
        }
    }

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature applier, CardModel cardSource)
    {
        await base.AfterPowerAmountChanged(choiceContext, power, amount, applier, cardSource);
        if (Owner == null) return;

        // 消耗月夜时触发
        if (power is LegacyFortunePower && power.Owner == this.Owner && amount < 0)
        {
            // 🔥 严格按照你要求的参数调用，直接使用 Amount
            await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null, fast: true);
        }
    }
}
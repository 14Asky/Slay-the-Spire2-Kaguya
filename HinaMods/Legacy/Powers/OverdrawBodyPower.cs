using BaseLib.Abstracts;
using Kaguya.CardPools;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.Powers;

public sealed class LegacyOverdrawBodyPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string CustomPackedIconPath => "res://images/packed/card_portraits/kaguya/OverdrawBodyPower.png";
    public override string CustomBigIconPath => "res://images/packed/card_portraits/kaguya/OverdrawBodyPower.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new[]
    {
        HoverTipFactory.FromPower<Overwork>()
    };

    // 修正：添加 PlayerChoiceContext 作为第一个参数
    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature applier, CardModel cardSource)
    {
        if (amount > 0 && power is Overwork && applier == Owner)
        {
            Flash();
            int drawAmount = (int)Amount;
            await CardPileCmd.Draw(choiceContext, drawAmount, Owner.Player);
        }
    }
}
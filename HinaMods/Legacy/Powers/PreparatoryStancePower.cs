using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using System.Threading.Tasks;

namespace Kaguya.Powers;

public sealed class LegacyPreparatoryStancePower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // BaseLib 风格的自定义图标路径
    public override string CustomPackedIconPath => "res://images/packed/card_portraits/kaguya/PreparatoryStancePower.png";
    public override string CustomBigIconPath => "res://images/packed/card_portraits/kaguya/PreparatoryStancePower.png";

    public override int ModifyCardPlayCount(CardModel card, Creature target, int playCount)
    {
        if (card.Owner.Creature != Owner) return playCount;
        if (card.Type != CardType.Attack) return playCount;
        return playCount + 1; // 额外打出1次
    }

    public override async Task AfterModifyingCardPlayCount(CardModel card)
    {
        if (card.Type == CardType.Attack)
        {
            await PowerCmd.Decrement(this);
        }
    }

    // 移除回合结束自动移除的能力，使效果可以跨回合保留直到被使用
}
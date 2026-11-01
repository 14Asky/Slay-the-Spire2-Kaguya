//using BaseLib.Abstracts;
//using MegaCrit.Sts2.Core.Combat;
//using MegaCrit.Sts2.Core.Commands;
//using MegaCrit.Sts2.Core.Entities.Cards;
//using MegaCrit.Sts2.Core.Entities.Creatures;
//using MegaCrit.Sts2.Core.Entities.Powers;
//using MegaCrit.Sts2.Core.GameActions.Multiplayer;
//using MegaCrit.Sts2.Core.Models;
//using Kaguya.HinaMods;
//using Kaguya.HinaMods.Legacy.Cards;
//using System.Threading.Tasks;

//namespace Kaguya.HinaMods.Legacy.Powers;

//public sealed class LegacySupportBurstPower : CustomPowerModel
//{
//    public override PowerType Type => PowerType.Buff;
//    public override PowerStackType StackType => PowerStackType.Counter;
//    public override bool AllowNegative => false;

//    public override string CustomPackedIconPath => "res://images/hinamods/Powers/support_burst_power.png";
//    public override string CustomBigIconPath => "res://images/hinamods/Powers/support_burst_power.png";

//    // 核心：支援牌额外生效 1 次
//    public override int ModifyCardPlayCount(CardModel card, Creature target, int playCount)
//    {
//        if (card.Owner.Creature != base.Owner)
//            return playCount;

//        if (card is not HinaModsCard hinaCard || !hinaCard.CustomTags.Contains(LegacyCustomCardTags.SUPPORT))
//            return playCount;

//        // 原版+1 → 改为+2（额外生效两次）
//        return playCount + 1;
//    }

//    // 触发后消耗1层
//    public override async Task AfterModifyingCardPlayCount(CardModel card)
//    {
//        await PowerCmd.Decrement(this);
//    }

//    // 回合结束移除
//    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
//    {
//        if (side == base.Owner.Side)
//        {
//            await PowerCmd.Remove(this);
//        }
//    }
//}
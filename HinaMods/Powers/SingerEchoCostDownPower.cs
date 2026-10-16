using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Kaguya.HinaMods;
using Kaguya.HinaMods.Cards;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Powers;

/// <summary>
/// 等待（SingerEchoCostDownPower）
/// 本地化键：KAGUYA-SINGER_ECHO_COST_DOWN_POWER.title / .description
/// 本回合内，下一张打出的【歌声】牌费用减少 Amount 点；打出该牌后或回合结束时移除本能力。
/// 由「等待」（Wait）施加。
/// </summary>
public sealed class SingerEchoCostDownPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerInstanceType InstanceType => PowerInstanceType.None;
    public override bool AllowNegative => false;
    public override int DisplayAmount => 0;

    public override string CustomPackedIconPath => "res://images/hinamods/Powers/singer_echo_cost_down.png";
    public override string CustomBigIconPath => "res://images/hinamods/Powers/singer_echo_cost_down.png";

    // 减费：仅对玩家自己的【回音】牌生效
    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;

        bool isPlayerEcho = card.Owner.Creature == Owner
            && card is HinaModsCard hinaCard && hinaCard.HasEcho;

        if (isPlayerEcho)
        {
            modifiedCost = originalCost - Amount;
            modifiedCost = modifiedCost < 0m ? 0m : modifiedCost;
            return true;
        }
        return false;
    }

    // 打出回音牌后消耗本能力
    public override async Task BeforeCardPlayed(CardPlay cardPlay)
    {
        await base.BeforeCardPlayed(cardPlay);
        if (cardPlay.Card == null) return;

        bool isPlayerEcho = cardPlay.Card.Owner.Creature == Owner
            && cardPlay.Card is HinaModsCard hinaCard && hinaCard.HasEcho;

        if (isPlayerEcho)
        {
            await PowerCmd.Remove(this);
        }
    }

    // 回合结束移除
    public override async Task AfterSideTurnEnd(PlayerChoiceContext ctx, CombatSide side, IEnumerable<Creature> participants)
    {
        await base.AfterSideTurnEnd(ctx, side, participants);
        await PowerCmd.Remove(this);
    }
}

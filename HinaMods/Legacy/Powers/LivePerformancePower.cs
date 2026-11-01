using BaseLib.Abstracts;
using Kaguya.CardPools;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.Powers;

public sealed class LegacyLivePerformancePower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override string CustomPackedIconPath => "res://images/packed/card_portraits/kaguya/LivePerformancePower.png";
    public override string CustomBigIconPath => "res://images/packed/card_portraits/kaguya/LivePerformancePower.png";

    private class LegacyData
    {
        public bool cardsPlayedThisTurn;
    }
    protected override object InitInternalData()
    {
        return new LegacyData();
    }
    public override async Task AfterApplied(Creature applier, CardModel cardSource)
    {
        var overwork = Owner.GetPower<Overwork>();
        if (overwork != null)
        {
            await PowerCmd.Remove(overwork);
        }
        GetInternalData<LegacyData>().cardsPlayedThisTurn = CombatManager.Instance.History.CardPlaysStarted.Count((CardPlayStartedEntry e) => e.Actor == Owner && e.CardPlay.IsFirstInSeries && e.HappenedThisTurn(CombatState) && e.CardPlay.Card.Tags.Contains((CardTag)1001)) > 0;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;
        GetInternalData<LegacyData>().cardsPlayedThisTurn = false;
        await Task.CompletedTask;
    }

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        if (card.Owner.Creature != Owner || !card.Tags.Contains((CardTag)1001) || GetInternalData<LegacyData>().cardsPlayedThisTurn)
        {
            modifiedCost = originalCost;
            return false;
        }

        modifiedCost = default;
        return true;
    }

    public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner.Player || !cardPlay.Card.Tags.Contains((CardTag)1001))
        {
            return Task.CompletedTask;
        }

        GetInternalData<LegacyData>().cardsPlayedThisTurn = true;
        return Task.CompletedTask;
    }
}
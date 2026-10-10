using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using System.Threading.Tasks;

namespace Kaguya.Enchantments
{
    public sealed class ThoughtEnchantment : CustomEnchantmentModel
    {
        private bool _usedThisTurn;

        public override bool ShowAmount => false;

        protected override string CustomIconPath => "res://images/enchantments/thought_enchantment.png";

        public override CardLocation ModifyCardPlayResultLocation(
            CardModel card, bool isAutoPlay, ResourceInfo resources, CardLocation cardLocation)
        {
            if (card != Card) return cardLocation;
            if (_usedThisTurn) return cardLocation;
            if (cardLocation.pileType == PileType.Discard || cardLocation.pileType == PileType.Exhaust)
            {
                return new CardLocation(cardLocation.player, PileType.Hand, cardLocation.position);
            }
            return cardLocation;
        }

        public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            if (_usedThisTurn) return;
            if (cardPlay.Card != Card) return;
            _usedThisTurn = true;
        }

        public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
        {
            if (Card == null) return;
            if (Card.Owner != player) return;
            _usedThisTurn = false;
        }
    }
}
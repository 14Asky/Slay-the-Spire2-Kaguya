using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.Powers
{
    public sealed class LegacyGluttonyPower : CustomPowerModel
    {
        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Counter;
        public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

        public override string CustomPackedIconPath => "res://images/powers/GluttonyPower.png";
        public override string CustomBigIconPath => "res://images/powers/GluttonyPower.png";

        public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            if (cardPlay.Card.Owner?.Creature != Target) return;

            int newAmount = Amount + 1;

            if (newAmount >= 7)
            {
                await ExhaustRandomCardFromHand(cardPlay.Card.Owner, choiceContext);
                await PowerCmd.ModifyAmount(choiceContext, this, 1 - Amount, null, null);
            }
            else
            {
                await PowerCmd.ModifyAmount(choiceContext, this, 1, null, null);
            }
        }

        private async Task ExhaustRandomCardFromHand(Player player, PlayerChoiceContext context)
        {
            var hand = PileType.Hand.GetPile(player).Cards.ToList();
            if (hand.Count == 0) return;

            var rng = player.RunState.Rng.CombatCardSelection;
            CardModel cardToExhaust = rng.NextItem(hand);

            if (cardToExhaust != null)
            {
                await CardCmd.Exhaust(context, cardToExhaust);
                Flash();
            }
        }
    }
}
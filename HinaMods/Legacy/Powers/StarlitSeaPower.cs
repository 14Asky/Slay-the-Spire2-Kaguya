using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya
{
    public sealed class LegacyStarlitSeaPower : PowerModel
    {
        private readonly HashSet<CardModel> _cardsSelectedBefore = new();

        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Counter;
        public override int DisplayAmount => base.Amount;

        // 新版中 BeforeHandDraw 仍然可用，参数类型需要改为 ICombatState
        public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
        {
            if (player != Owner.Player) return;

            var exhaustPile = PileType.Exhaust.GetPile(player);
            var exhaustCards = exhaustPile.Cards.ToList();
            if (exhaustCards.Count == 0) return;

            var prefs = new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 1);
            var selected = await CardSelectCmd.FromSimpleGrid(choiceContext, exhaustCards, player, prefs);
            var card = selected.FirstOrDefault();
            if (card == null) return;

            bool isFirstTime = !_cardsSelectedBefore.Contains(card);
            if (!isFirstTime && !card.EnergyCost.CostsX && card.EnergyCost.GetWithModifiers(CostModifiers.None) >= 0)
            {
                card.EnergyCost.AddThisCombat(1);
            }

            _cardsSelectedBefore.Add(card);
            await CardPileCmd.Add(card, PileType.Hand);
            await CardPileCmd.Draw(choiceContext, base.Amount, player);
        }
    }
}
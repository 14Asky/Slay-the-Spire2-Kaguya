using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace Kaguya
{
    public sealed class LegacyTransientSymphonyPower : PowerModel
    {
        private readonly List<CardModel> _cardsToMark = new List<CardModel>();

        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Counter;

        private int MaxRetain => base.Amount == 2 ? 2 : 1;
        private bool RetainFilter(CardModel card) => !card.ShouldRetainThisTurn;

        // 每回合开始时：只为之前记录的卡牌添加虚无
        public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
        {
            if (side != CombatSide.Player) return;

            foreach (var card in _cardsToMark.ToList())
            {
                if (card.Pile?.Type == PileType.Hand || card.Pile?.Type == PileType.Draw || card.Pile?.Type == PileType.Discard)
                {
                    card.AddKeyword(CardKeyword.Ethereal);
                }
            }
            _cardsToMark.Clear();
        }

        // 回合结束前：保留最多 MaxRetain 张手牌
        public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
        {
            if (side != CombatSide.Player) return;
            var player = Owner.Player;
            if (player == null) return;

            var handCards = PileType.Hand.GetPile(player).Cards.ToList();
            if (handCards.Count == 0) return;

            var prefs = new CardSelectorPrefs(base.SelectionScreenPrompt, 0, base.Amount);
            var selected = await CardSelectCmd.FromHand(choiceContext, player, prefs, RetainFilter, this);
            var chosen = selected.ToList();

            foreach (var card in chosen)
            {
                card.GiveSingleTurnRetain();
                _cardsToMark.Add(card);
            }
        }
    }
}

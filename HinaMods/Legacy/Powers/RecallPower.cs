using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace Kaguya
{
    public sealed class LegacyRecallPower : PowerModel
    {
        private readonly List<CardModel> _cardsToRecall = new();

        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Counter;
        public override int DisplayAmount => _cardsToRecall.Count;

        public void AddCard(CardModel card) => _cardsToRecall.Add(card);

        public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
        {
            if (side != CombatSide.Player || _cardsToRecall.Count == 0) return;

            var cards = new List<CardModel>(_cardsToRecall);
            _cardsToRecall.Clear();

            foreach (var card in cards)
            {
                if (card.Pile?.Type != PileType.Exhaust) continue;
                // 使用无上下文的 Add 重载（参数顺序：卡牌, 牌堆类型）
                await CardPileCmd.Add(card, PileType.Hand);
                card.AddKeyword(CardKeyword.Retain);
            }

            await PowerCmd.Remove(this);
        }
    }
}

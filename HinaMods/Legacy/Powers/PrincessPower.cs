using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using System.Threading.Tasks;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace Kaguya
{
    public sealed class LegacyPrincessPower : PowerModel
    {
        private int _cardsPlayedThisTurn = 0;

        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Counter;
        public override int DisplayAmount => _cardsPlayedThisTurn;

        // 每打出一张牌，增加计数
        public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
        {
            if (cardPlay.Card.Owner != Owner.Player) return;

            _cardsPlayedThisTurn++;
            Flash();

            if (_cardsPlayedThisTurn >= 8)
            {
                // 获得3点力量和3点敏捷
                await PowerCmd.Apply<StrengthPower>(context, Owner, 3, Owner, null);
                await PowerCmd.Apply<DexterityPower>(context, Owner, 3, Owner, null);
                _cardsPlayedThisTurn = 0;
            }
            InvokeDisplayAmountChanged();
        }

        // 回合结束时重置计数器（能力常驻，不移除自身）
        public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
        {
            if (side == CombatSide.Player)
            {
                _cardsPlayedThisTurn = 0;
                InvokeDisplayAmountChanged();
            }
            await Task.CompletedTask;
        }
    }
}

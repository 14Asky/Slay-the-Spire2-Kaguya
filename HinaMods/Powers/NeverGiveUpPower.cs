using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Kaguya.Powers
{
    public sealed class NeverGiveUpPower : CustomPowerModel
    {
        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Counter; // 层数每回合多一张牌

        public override string CustomPackedIconPath => "res://images/powers/never_give_up.png";
        public override string CustomBigIconPath => "res://images/powers/never_give_up.png";

        // 使用新版回合开始钩子
        public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
        {
            if (side != Owner.Side) return;
            var player = Owner.Player;
            if (player == null) return;

            var allCards = player.Character.CardPool
                .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
                .ToList();

            if (allCards.Count == 0) return;

            int amount = (int)Amount;
            for (int i = 0; i < amount; i++)
            {
                var rng = player.RunState.Rng.CombatCardGeneration;
                var card = CardFactory.GetDistinctForCombat(player, allCards, 1, rng).FirstOrDefault();
                if (card == null) continue;

                // 若要本回合免费，取消注释下一行
                // card.SetToFreeThisTurn();

                // 正确调用：无 addedByPlayer 参数
                await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player);
            }

            Flash();
        }
    }
}

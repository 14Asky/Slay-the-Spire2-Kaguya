using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Kaguya.Powers
{
    public sealed class LegacyOppressivePressurePower : CustomPowerModel
    {
        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Counter;
        public override PowerInstanceType InstanceType => PowerInstanceType.None;
        public override bool AllowNegative => false;

        private const int THRESHOLD = 30;
        private bool _isHandling = false;

        public override string CustomPackedIconPath => "res://images/powers/yapogan.png";
        public override string CustomBigIconPath => "res://images/powers/yapogan.png";

        // 每张牌打出后增加1层
        public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            if (cardPlay.Card.Owner.Creature != Owner) return;
            await PowerCmd.Apply<LegacyOppressivePressurePower>(choiceContext, Owner, 1, Owner, null);
        }

        // 能力层数变化时触发（达到阈值则结束回合）
        public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature applier, CardModel cardSource)
        {
            if (power != this) return;
            if (_isHandling) return;
            if (Amount < THRESHOLD) return;

            _isHandling = true;

            // 移除当前实例
            await PowerCmd.Remove(this);
            // 重新添加，重置为0层
            await PowerCmd.Apply<LegacyOppressivePressurePower>(choiceContext, Owner, 1, Owner, null);

            await Cmd.Wait(0.01f);

            var player = Owner.Player;
            if (player != null && Owner.Side == CombatSide.Player)
            {
                // 直接结束回合（新版 API 会自动判断是否可结束）
                PlayerCmd.EndTurn(player, false);
            }

            _isHandling = false;
            Flash();
        }

        // 每回合开始时重置层数为0
        public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
        {
            if (side != Owner.Side) return;
            if (Amount != 0)
            {
                var choiceContext = new ThrowingPlayerChoiceContext();
                await PowerCmd.Remove(this);
                await PowerCmd.Apply<LegacyOppressivePressurePower>(choiceContext, Owner, 1, Owner, null);
            }
        }
    }
}

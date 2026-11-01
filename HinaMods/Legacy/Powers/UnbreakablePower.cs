using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Kaguya.Powers
{
    public sealed class LegacyUnbreakablePower : CustomPowerModel
    {
        private class LegacyData
        {
            public decimal damageReceivedThisTurn;
        }

        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Counter;

        // 多人模式缩放
        public override bool ShouldScaleInMultiplayer => true;

        public override string CustomPackedIconPath => "res://images/powers/VajraPower.png";
        public override string CustomBigIconPath => "res://images/powers/VajraPower.png";

        // 确保能力不会因怪物死亡/复活而被移除
        public override bool ShouldPowerBeRemovedAfterOwnerDeath() => false;

        // 显示剩余可吸收伤害
        public override int DisplayAmount => (int)Math.Max(0m, (decimal)base.Amount - GetInternalData<LegacyData>().damageReceivedThisTurn);

        protected override object InitInternalData() => new LegacyData();

        // 限制每回合受到的伤害不超过 Amount
        public override decimal ModifyHpLostBeforeOstyLate(Creature target, decimal amount, ValueProp props, Creature dealer, CardModel cardSource)
        {
            if (target != Owner) return amount;
            if (amount == 0m) return amount;
            decimal remaining = (decimal)base.Amount - GetInternalData<LegacyData>().damageReceivedThisTurn;
            if (remaining <= 0m) return 0m;
            return Math.Min(amount, remaining);
        }

        // 记录已造成的未格挡伤害
        public override Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature dealer, CardModel cardSource)
        {
            if (target != Owner) return Task.CompletedTask;
            if (result.WasFullyBlocked) return Task.CompletedTask;
            GetInternalData<LegacyData>().damageReceivedThisTurn += (decimal)result.UnblockedDamage;
            InvokeDisplayAmountChanged();
            return Task.CompletedTask;
        }

        // 玩家回合开始时重置计数器（新版签名）
        public override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
        {
            if (side != CombatSide.Player) return Task.CompletedTask;
            GetInternalData<LegacyData>().damageReceivedThisTurn = default(decimal);
            InvokeDisplayAmountChanged();
            return Task.CompletedTask;
        }
    }
}

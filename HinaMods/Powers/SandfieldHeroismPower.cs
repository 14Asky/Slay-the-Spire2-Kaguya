using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.Powers
{
    public sealed class SandfieldHeroismPower : CustomPowerModel
    {
        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Counter;

        public override string CustomPackedIconPath => "res://images/powers/SandfieldHeroismPower.png";
        public override string CustomBigIconPath => "res://images/powers/SandfieldHeroismPower.png";

        protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { HoverTipFactory.FromPower<StrengthPower>() };

        public override async Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand command)
        {
            if (command.Attacker != Owner || command.TargetSide == Owner.Side || !command.DamageProps.IsPoweredAttack())
                return;

            // 与 SuckPower 完全一致的命中统计逻辑
            var hits = command.Results.ToList();
            int unblockedHitCount = 0;
            foreach (var hitGroup in hits)
            {
                // 排除宠物受击导致的玩家伤害记录
                var petReceivers = hitGroup.Where(r => r.Receiver.IsPet).ToList();
                foreach (var petHit in petReceivers)
                {
                    hitGroup.RemoveAll(r => r.Receiver == petHit.Receiver.PetOwner?.Creature);
                }
                if (hitGroup.Any(r => r.UnblockedDamage > 0))
                    unblockedHitCount++;
            }

            if (unblockedHitCount > 0)
            {
                Flash();
                await PowerCmd.Apply<StrengthPower>(choiceContext, Owner, Amount * unblockedHitCount, Owner, null);
            }
        }
    }
}
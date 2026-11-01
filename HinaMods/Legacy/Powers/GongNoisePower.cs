using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.Powers
{
    public sealed class LegacyGongNoisePower : CustomPowerModel
    {
        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Single;

        public override string CustomPackedIconPath => "res://images/powers/GongNoisePower.png";
        public override string CustomBigIconPath => "res://images/powers/GongNoisePower.png";

        public override async Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand command)
        {
            if (command.Attacker != Owner || command.TargetSide == Owner.Side || !command.DamageProps.IsPoweredAttack())
                return;

            // 与 SuckPower 完全相同的命中计算逻辑
            var hits = command.Results.ToList();
            int validHits = 0;
            foreach (var hitGroup in hits)
            {
                // 排除宠物造成的伤害影响（同 SuckPower）
                var petReceivers = hitGroup.Where(r => r.Receiver.IsPet).ToList();
                foreach (var petHit in petReceivers)
                {
                    hitGroup.RemoveAll(r => r.Receiver == petHit.Receiver.PetOwner?.Creature);
                }
                if (hitGroup.Any(r => r.UnblockedDamage > 0))
                    validHits++;
            }

            if (validHits == 0) return;

            Flash();

            var players = Owner.CombatState?.Players
                .Where(p => p.Creature.IsAlive)
                .Select(p => p.Creature)
                .ToList();
            if (players == null) return;

            foreach (var player in players)
            {
                // 每个玩家抽牌堆和弃牌堆各塞一张 Dazed
                await CardPileCmd.AddToCombatAndPreview<Dazed>(player, PileType.Draw, 1, null);
                await CardPileCmd.AddToCombatAndPreview<Dazed>(player, PileType.Discard, 1, null);
            }
        }
    }
}
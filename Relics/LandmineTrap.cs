using BaseLib.Abstracts;
using BaseLib.Utils;
using Kaguya.RelicPools;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.Relics
{
    [Pool(typeof(KaguyaRelicPool))]
    public sealed class LandmineTrap : CustomRelicModel
    {
        private bool _hasTriggeredThisCombat = false;

        public override RelicRarity Rarity => RelicRarity.Ancient;

        protected override IEnumerable<DynamicVar> CanonicalVars => System.Array.Empty<DynamicVar>();

        public override string PackedIconPath => "res://images/relics/landmine_trap.png";
        protected override string PackedIconOutlinePath => "res://images/relics/landmine_trap_outline.png";
        protected override string BigIconPath => "res://images/relics/landmine_trap_big.png";

        public override Task BeforeCombatStart()
        {
            _hasTriggeredThisCombat = false;
            return Task.CompletedTask;
        }

        public override Task AfterCombatEnd(CombatRoom _)
        {
            _hasTriggeredThisCombat = false;
            return Task.CompletedTask;
        }

        public override decimal ModifyHpLostBeforeOsty(Creature target, decimal amount, ValueProp props, Creature dealer, CardModel cardSource)
        {
            // 只在战斗中进行中生效
            if (!CombatManager.Instance.IsInProgress) return amount;
            // 只处理遗物拥有者受到的伤害
            if (target != Owner.Creature) return amount;
            if (_hasTriggeredThisCombat) return amount;
            if (amount <= 0) return amount;

            // 触发陷阱
            _hasTriggeredThisCombat = true;
            Flash();

            // 对全体敌人造成一半伤害
            decimal halfDamage = amount;
            var enemies = Owner.Creature.CombatState.HittableEnemies;
            if (enemies.Count > 0)
            {
                // 创建临时上下文，确保伤害命令能够执行
                var tempContext = new BlockingPlayerChoiceContext();
                _ = CreatureCmd.Damage(tempContext, enemies, halfDamage, ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.SkipHurtAnim, Owner.Creature, null, null);
            }

            // 免疫本次伤害
            return 0;
        }
    }
}
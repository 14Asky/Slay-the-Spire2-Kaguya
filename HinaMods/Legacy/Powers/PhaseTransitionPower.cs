using BaseLib.Abstracts;
using Godot;
using Kaguya.Monsters;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using System.Threading.Tasks;

namespace Kaguya.Powers
{
    public sealed class LegacyPhaseTransitionPower : CustomPowerModel
    {
        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Single;

        // 自定义图标路径（请根据实际资源位置调整）
        public override string CustomPackedIconPath => "res://images/ui/run_history/tsukito_tenchu_boss_encounter.png";
        public override string CustomBigIconPath => "res://images/ui/run_history/tsukito_tenchu_boss_encounter.png";

        private bool _isReviving;

        public void DoRevive()
        {
            _isReviving = false;
        }

        public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
        {
            if (!wasRemovalPrevented && creature == Owner && Owner.Monster is TsukitoTenchu tenchu)
            {
                _isReviving = true;
                await tenchu.TriggerRevive();
            }
        }

        public override bool ShouldAllowHitting(Creature creature)
        {
            if (creature != Owner) return true;
            return !_isReviving;
        }

        public override bool ShouldStopCombatFromEnding() => true;

        public override bool ShouldCreatureBeRemovedFromCombatAfterDeath(Creature creature)
        {
            if (creature != Owner) return true;
            return false;
        }

        public override bool ShouldPowerBeRemovedAfterOwnerDeath() => false;
    }
}
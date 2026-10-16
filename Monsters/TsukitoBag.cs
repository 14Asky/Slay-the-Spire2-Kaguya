using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using Kaguya.Powers;
using Kaguya.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.Monsters
{
    public sealed class TsukitoBag : CustomMonsterModel
    {
        public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 280, 270);
        public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 300, 280);

        private int SmashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 32, 29);
        private const int VulnerableAmount = 2;

        private int ChargeStrength => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 5, 4);

        private int ScatterDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 2, 1);
        private int ScatterHitCount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 3);

        public override NCreatureVisuals CreateCustomVisuals()
            => NodeFactory<NCreatureVisuals>.CreateFromScene("res://scenes/monsters/tsukito_bag.tscn");

        protected override MonsterMoveStateMachine GenerateMoveStateMachine()
        {
            var smashState = new MoveState("SMASH", SmashMove,
                new SingleAttackIntent(() => SmashDamage), new DebuffIntent());

            var chargeState = new MoveState("CHARGE", ChargeMove,
                new BuffIntent());

            var scatterState = new MoveState("SCATTER", ScatterMove,
                new MultiAttackIntent(ScatterDamage, ScatterHitCount));

            var randomChoice = new RandomBranchState("RANDOM_SMASH_OR_CHARGE");
            randomChoice.AddBranch(smashState, 1, MoveRepeatType.CannotRepeat, 0.5f);
            randomChoice.AddBranch(chargeState, 0, MoveRepeatType.CanRepeatForever, 0.5f);

            smashState.FollowUpState = randomChoice;
            chargeState.FollowUpState = scatterState;
            scatterState.FollowUpState = randomChoice;

            var states = new List<MonsterState> { smashState, chargeState, scatterState, randomChoice };
            return new MonsterMoveStateMachine(states, smashState);
        }

        private async Task SmashMove(IReadOnlyList<Creature> targets)
        {
            await DamageCmd.Attack(SmashDamage)
                .FromMonster(this)
                .WithAttackerFx(null, AttackSfx)
                .WithHitFx("vfx/vfx_attack_blunt")
                .Execute(null);

            var choiceContext = new ThrowingPlayerChoiceContext();
            foreach (var target in targets)
            {
                await PowerCmd.Apply<VulnerablePower>(choiceContext, target, VulnerableAmount, Creature, null);
            }
        }

        private async Task ChargeMove(IReadOnlyList<Creature> targets)
        {
            var choiceContext = new ThrowingPlayerChoiceContext();
            await PowerCmd.Apply<StrengthPower>(choiceContext, Creature, ChargeStrength, Creature, null);
        }

        private async Task ScatterMove(IReadOnlyList<Creature> targets)
        {
            await DamageCmd.Attack(ScatterDamage)
                .WithHitCount(ScatterHitCount)
                .FromMonster(this)
                .WithAttackerFx(null, AttackSfx)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(null);
        }

        // ---------- 死亡时升级记忆碎片 ----------
        public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
        {
            await base.AfterDeath(choiceContext, creature, wasRemovalPrevented, deathAnimLength);

            var players = Creature.CombatState?.Players.Where(p => p.Creature.IsAlive).ToList();
            if (players != null)
            {
                foreach (var player in players)
                {
                    await UpgradeMemoryFragment(player);
                }
            }
        }

        private async Task UpgradeMemoryFragment(Player player)
        {
            var currentRelic = player.Relics.FirstOrDefault(r =>
                r is MemoryFragmentStart ||
                r is MemoryFragmentDaily ||
                r is MemoryFragmentConcert);
            if (currentRelic == null) return;

            RelicModel newRelic = null;
            if (currentRelic is MemoryFragmentStart)
                newRelic = ModelDb.Relic<MemoryFragmentDaily>().ToMutable();
            else if (currentRelic is MemoryFragmentDaily)
                newRelic = ModelDb.Relic<MemoryFragmentConcert>().ToMutable();
            else if (currentRelic is MemoryFragmentConcert)
                newRelic = ModelDb.Relic<MemoryFragmentResolution>().ToMutable();

            if (newRelic == null) return;

            await RelicCmd.Remove(currentRelic);
            await RelicCmd.Obtain(newRelic, player);
        }

        public override async Task AfterAddedToRoom()
        {
            await base.AfterAddedToRoom();
            var choiceContext = new ThrowingPlayerChoiceContext();
            await PowerCmd.Apply<HighDimensionalBeingPower>(choiceContext, Creature, 3, Creature, null);
        }
    }
}
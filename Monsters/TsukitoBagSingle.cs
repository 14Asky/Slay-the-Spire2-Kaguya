using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using Kaguya.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.Monsters
{
    public sealed class TsukitoBagSingle : CustomMonsterModel
    {
        // 固定血量 500，不受进阶影响
        public override int MinInitialHp => 450;
        public override int MaxInitialHp => 500;

        private int SmashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 22, 19);
        private const int VulnerableAmount = 2;

        private int ChargeStrength => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 2);

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

        public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
        {
            await base.AfterDeath(choiceContext, creature, wasRemovalPrevented, deathAnimLength);
        }

        public override async Task AfterAddedToRoom()
        {
            await base.AfterAddedToRoom();
            var choiceContext = new ThrowingPlayerChoiceContext();
            await PowerCmd.Apply<HigherDimensionalBeingPower>(choiceContext, Creature, 2, Creature, null);
        }
    }
}
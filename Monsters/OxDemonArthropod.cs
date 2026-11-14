using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.Monsters
{
    public sealed class OxDemonArthropod : CustomMonsterModel
    {
        private const string SlashSfx = "event:/sfx/enemy/enemy_attacks/test_subject/test_subject_slash";
        private const string BluntSfx = "event:/sfx/enemy/enemy_attacks/test_subject/test_subject_bite";

        public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 195, 180);
        public override int MaxInitialHp => MinInitialHp;

        private int PlatingAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 16, 13);
        private int ComboDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 9, 7);
        private const int ComboHitCount = 2;
        private const int MutilateDamage = 10;
        private const int VulnerableAmount = 2;
        private const int RipDamage = 14;
        private const int WeakAmount = 2;

        protected override string VisualsPath => "res://scenes/monsters/ox_demon_arthropod.tscn";

        public override NCreatureVisuals CreateCustomVisuals()
            => NodeFactory<NCreatureVisuals>.CreateFromScene(VisualsPath);

        public override async Task AfterAddedToRoom()
        {
            var choiceContext = new ThrowingPlayerChoiceContext();
            await PowerCmd.Apply<PlatingPower>(choiceContext, Creature, PlatingAmount, Creature, null);
        }

        protected override MonsterMoveStateMachine GenerateMoveStateMachine()
        {
            var comboState = new MoveState("COMBO", ComboMove,
                new MultiAttackIntent(ComboDamage, ComboHitCount), new BuffIntent());
            var mutilateState = new MoveState("MUTILATE", MutilateMove,
                new SingleAttackIntent(MutilateDamage), new DebuffIntent());
            var ripState = new MoveState("RIP", RipMove,
                new SingleAttackIntent(RipDamage), new DebuffIntent());

            mutilateState.FollowUpState = comboState;
            ripState.FollowUpState = comboState;

            var randomChoice = new RandomBranchState("RANDOM_ATTACK");
            randomChoice.AddBranch(mutilateState, MoveRepeatType.CanRepeatForever, 0.5f);
            randomChoice.AddBranch(ripState, MoveRepeatType.CanRepeatForever, 0.5f);

            var conditionalAttack = new ConditionalBranchState("CONDITIONAL_ATTACK");
            conditionalAttack.AddState(ripState, () => GetTarget()?.HasPower<VulnerablePower>() == true);
            conditionalAttack.AddState(mutilateState, () => GetTarget()?.HasPower<WeakPower>() == true);
            conditionalAttack.AddState(randomChoice, () => true);

            comboState.FollowUpState = conditionalAttack;

            var states = new List<MonsterState>
            {
                conditionalAttack,
                comboState,
                mutilateState,
                ripState,
                randomChoice
            };
            return new MonsterMoveStateMachine(states, conditionalAttack);
        }

        private Creature GetTarget()
        {
            return Creature.CombatState?.Players.FirstOrDefault(p => p.Creature.IsAlive)?.Creature;
        }

        private async Task ComboMove(IReadOnlyList<Creature> targets)
        {
            await DamageCmd.Attack(ComboDamage)
                .WithHitCount(ComboHitCount)
                .FromMonster(this)
                .WithAttackerFx(null, SlashSfx)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(null);
            var choiceContext = new ThrowingPlayerChoiceContext();
            await PowerCmd.Apply<StrengthPower>(choiceContext, Creature, 2m, Creature, null);
        }

        private async Task MutilateMove(IReadOnlyList<Creature> targets)
        {
            await DamageCmd.Attack(MutilateDamage)
                .FromMonster(this)
                .WithAttackerFx(null, BluntSfx)
                .WithHitFx("vfx/vfx_attack_blunt")
                .Execute(null);
            var choiceContext = new ThrowingPlayerChoiceContext();
            await PowerCmd.Apply<VulnerablePower>(choiceContext, targets, VulnerableAmount, Creature, null);
        }

        private async Task RipMove(IReadOnlyList<Creature> targets)
        {
            await DamageCmd.Attack(RipDamage)
                .FromMonster(this)
                .WithAttackerFx(null, SlashSfx)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(null);
            var choiceContext = new ThrowingPlayerChoiceContext();
            await PowerCmd.Apply<WeakPower>(choiceContext, targets, WeakAmount, Creature, null);
        }
    }
}

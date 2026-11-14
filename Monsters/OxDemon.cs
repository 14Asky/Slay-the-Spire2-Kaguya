using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.Monsters
{
    public sealed class OxDemon : CustomMonsterModel
    {
        private const string BiteSfx = "event:/sfx/enemy/enemy_attacks/test_subject/test_subject_bite";
        private const string SlashSfx = "event:/sfx/enemy/enemy_attacks/test_subject/test_subject_slash";

        public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 80, 75);
        public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 80, 75);

        private int EnrageAmount
    {
        get
        {
            int playerCount = Creature?.CombatState?.Players?.Count ?? 1;
            return playerCount switch
            {
                1 => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 2),
                2 => 2,
                _ => 1,
            };
        }
    }
        private int SkullBashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 8, 6);
        private const int VulnerableAmount = 2;
        private int ChargeDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 14, 11);

        protected override string VisualsPath => "res://scenes/monsters/ox_demon.tscn";

        public override NCreatureVisuals CreateCustomVisuals()
            => NodeFactory<NCreatureVisuals>.CreateFromScene(VisualsPath);

        public override async Task AfterAddedToRoom()
        {
            await Task.CompletedTask;
        }

        protected override MonsterMoveStateMachine GenerateMoveStateMachine()
        {
            var enrageState = new MoveState("ENRAGE", EnrageMove, new BuffIntent());
            var skullBashState = new MoveState("SKULL_BASH", SkullBashMove,
                new SingleAttackIntent(SkullBashDamage), new DebuffIntent());
            var chargeState = new MoveState("CHARGE", ChargeMove,
                new SingleAttackIntent(ChargeDamage));

            enrageState.FollowUpState = skullBashState;
            skullBashState.FollowUpState = chargeState;
            chargeState.FollowUpState = skullBashState;

            return new MonsterMoveStateMachine(new[] { enrageState, skullBashState, chargeState }, enrageState);
        }

        private async Task EnrageMove(IReadOnlyList<Creature> targets)
        {
            TalkCmd.Play(L10NMonsterLookup("KAGUYA-OX_DEMON.moves.ENRAGE.banter"), Creature, VfxColor.Red);
            var choiceContext = new ThrowingPlayerChoiceContext();
            await PowerCmd.Apply<EnragePower>(choiceContext, Creature, EnrageAmount, Creature, null);
        }

        private async Task SkullBashMove(IReadOnlyList<Creature> targets)
        {
            await DamageCmd.Attack(SkullBashDamage)
                .FromMonster(this)
                .WithAttackerFx(null, BiteSfx)
                .WithHitFx("vfx/vfx_attack_blunt")
                .Execute(null);
            // 瀵规墍鏈夌帺瀹舵柦鍔犳槗�?
            var choiceContext = new ThrowingPlayerChoiceContext();
            await PowerCmd.Apply<VulnerablePower>(choiceContext, targets, VulnerableAmount, Creature, null);
        }

        private async Task ChargeMove(IReadOnlyList<Creature> targets)
        {
            await DamageCmd.Attack(ChargeDamage)
                .FromMonster(this)
                .WithAttackerFx(null, SlashSfx)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(null);
        }
    }
}
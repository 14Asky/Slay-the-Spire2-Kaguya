using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using Godot;
using Kaguya.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.Monsters
{
    public sealed class TsukitoKongoPhase3Only : CustomMonsterModel
    {
        // 视觉资源路径（使用三阶段图片）
        private const string Phase3Visual = "images/packed/monster/tsukito_kongo_phase3.png";

        // 血量（基础值，单人模式，与原始三阶段相同）
        private int Phase3Hp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 820, 790);

        public override int MinInitialHp => Phase3Hp;
        public override int MaxInitialHp => Phase3Hp;

        // 第三阶段数值（与原三阶段一致）
        private int SmashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 39, 37);
        private int BlinkDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 30, 27);
        private int ZenDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 27, 24);
        private int ZenStrength => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 9, 8);
        private int GoldenBodyDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 39, 36);
        private const int GoldenBodyBlock = 40;

        private MoveState _phase3GoldenBodyState, _phase3SmashState, _phase3BlinkState, _phase3ZenState;

        public override NCreatureVisuals CreateCustomVisuals()
            => NodeFactory<NCreatureVisuals>.CreateFromScene("res://scenes/monsters/tsukito_kongo.tscn");

        protected override MonsterMoveStateMachine GenerateMoveStateMachine()
        {
            _phase3GoldenBodyState = new MoveState("GOLDEN_BODY", GoldenBodyMove,
                new SingleAttackIntent(GoldenBodyDamage), new DefendIntent(), new BuffIntent());
            _phase3SmashState = new MoveState("SMASH", SmashMove,
                new SingleAttackIntent(SmashDamage));
            _phase3BlinkState = new MoveState("BLINK", BlinkMove,
                new SingleAttackIntent(BlinkDamage), new BuffIntent());
            _phase3ZenState = new MoveState("ZEN", ZenMove,
                new SingleAttackIntent(ZenDamage), new BuffIntent());

            var phase3Random = new RandomBranchState("PHASE3_RANDOM");
            phase3Random.AddBranch(_phase3SmashState, MoveRepeatType.CanRepeatForever, 0.34f);
            phase3Random.AddBranch(_phase3BlinkState, 3, MoveRepeatType.CannotRepeat, 0.33f);
            phase3Random.AddBranch(_phase3ZenState, 1, MoveRepeatType.CannotRepeat, 0.33f);

            _phase3GoldenBodyState.FollowUpState = phase3Random;
            _phase3SmashState.FollowUpState = phase3Random;
            _phase3BlinkState.FollowUpState = phase3Random;
            _phase3ZenState.FollowUpState = phase3Random;

            var allStates = new List<MonsterState>
            {
                _phase3GoldenBodyState, _phase3SmashState, _phase3BlinkState, _phase3ZenState, phase3Random
            };
            return new MonsterMoveStateMachine(allStates, _phase3GoldenBodyState);
        }

        private async Task GoldenBodyMove(IReadOnlyList<Creature> targets)
        {
            await DamageCmd.Attack(GoldenBodyDamage)
                .FromMonster(this)
                .WithAttackerFx(null, AttackSfx)
                .WithHitFx("vfx/vfx_attack_blunt")
                .Execute(null);
            await CreatureCmd.GainBlock(Creature, GoldenBodyBlock, ValueProp.Move, null);
            var choiceContext = new ThrowingPlayerChoiceContext();
            await PowerCmd.Apply<VajraPower>(choiceContext, Creature, 1, Creature, null);
        }

        private async Task SmashMove(IReadOnlyList<Creature> targets)
        {
            await DamageCmd.Attack(SmashDamage)
                .FromMonster(this)
                .WithAttackerFx(null, AttackSfx)
                .WithHitFx("vfx/vfx_attack_blunt")
                .Execute(null);
        }

        private async Task BlinkMove(IReadOnlyList<Creature> targets)
        {
            await DamageCmd.Attack(BlinkDamage)
                .FromMonster(this)
                .WithAttackerFx(null, AttackSfx)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(null);
            var choiceContext = new ThrowingPlayerChoiceContext();
            await PowerCmd.Apply<IntangiblePower>(choiceContext, Creature, 2, Creature, null);
        }

        private async Task ZenMove(IReadOnlyList<Creature> targets)
        {
            await DamageCmd.Attack(ZenDamage)
                .FromMonster(this)
                .WithAttackerFx(null, AttackSfx)
                .WithHitFx("vfx/vfx_attack_blunt")
                .Execute(null);
            var choiceContext = new ThrowingPlayerChoiceContext();
            await PowerCmd.Apply<StrengthPower>(choiceContext, Creature, ZenStrength, Creature, null);
        }

        private void UpdateVisual(string imagePath)
        {
            var node = NCombatRoom.Instance?.GetCreatureNode(Creature);
            if (node?.Visuals.GetCurrentBody() is Sprite2D sprite)
            {
                var tex = GD.Load<Texture2D>("res://" + imagePath);
                if (tex == null) return;
                sprite.Texture = tex;
                node.CreateTween().TweenProperty(sprite, "scale", sprite.Scale, 0.8f)
                    .From(sprite.Scale * 0.5f).SetEase(Tween.EaseType.Out);
            }
        }

        public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
        {
            await base.AfterDeath(choiceContext, creature, wasRemovalPrevented, deathAnimLength);
        }

        public override async Task AfterAddedToRoom()
        {
            await base.AfterAddedToRoom();
            UpdateVisual(Phase3Visual);
            var choiceContext = new ThrowingPlayerChoiceContext();
            await PowerCmd.Apply<UnbreakablePower>(choiceContext, Creature, 410, Creature, null);
            await PowerCmd.Apply<HigherDimensionalBeingPower>(choiceContext, Creature, 6, Creature, null);
        }
    }
}
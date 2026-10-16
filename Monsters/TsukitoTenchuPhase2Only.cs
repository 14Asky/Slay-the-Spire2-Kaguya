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
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.Monsters
{
    public sealed class TsukitoTenchuPhase2Only : CustomMonsterModel
    {
        // 视觉资源路径（使用二阶段图片）
        private const string VisualPath = "images/packed/monster/tsukito_tenchu_phase2.png";

        // 血量（基础值，单人模式，根据进阶调整）
        private int Phase2Hp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 700, 650);

        public override int MinInitialHp => Phase2Hp;
        public override int MaxInitialHp => Phase2Hp;

        // 第二阶段数值
        private int LaserDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 13, 10);
        private const int WeakAmount = 2;
        private int ChargeStrength => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 2, 1);
        private const int ScatterDamage = 1;
        private int ScatterHits => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 9, 8);

        private MoveState _phase2LaserState;
        private MoveState _phase2ChargeState;
        private MoveState _phase2ScatterState;

        public override NCreatureVisuals CreateCustomVisuals()
            => NodeFactory<NCreatureVisuals>.CreateFromScene("res://scenes/monsters/tsukito_tenchu.tscn");

        protected override MonsterMoveStateMachine GenerateMoveStateMachine()
        {
            // 第二阶段技能
            _phase2LaserState = new MoveState("LASER_CANNON", LaserCannonMove,
                new SingleAttackIntent(() => LaserDamage), new DebuffIntent());
            _phase2ChargeState = new MoveState("CHARGE", ChargeMove,
                new BuffIntent());
            _phase2ScatterState = new MoveState("SCATTER", ScatterMove,
                new MultiAttackIntent(ScatterDamage, ScatterHits));

            var phase2Random = new RandomBranchState("PHASE2_RANDOM");
            phase2Random.AddBranch(_phase2LaserState, MoveRepeatType.CanRepeatForever, 0.5f);
            phase2Random.AddBranch(_phase2ChargeState, MoveRepeatType.CanRepeatForever, 0.5f);

            _phase2ChargeState.FollowUpState = _phase2ScatterState;
            _phase2ScatterState.FollowUpState = phase2Random;
            _phase2LaserState.FollowUpState = phase2Random;

            var states = new List<MonsterState>
            {
                _phase2LaserState, _phase2ChargeState, _phase2ScatterState, phase2Random
            };

            // 第一回合固定镭射炮
            return new MonsterMoveStateMachine(states, _phase2LaserState);
        }

        // ---------- 第二阶段技能 ----------
        private async Task LaserCannonMove(IReadOnlyList<Creature> targets)
        {
            await DamageCmd.Attack(LaserDamage)
                .FromMonster(this)
                .WithAttackerFx(null, AttackSfx)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(null);
            var choiceContext = new ThrowingPlayerChoiceContext();
            foreach (var t in targets)
                await PowerCmd.Apply<WeakPower>(choiceContext, t, WeakAmount, Creature, null);
        }

        private async Task ChargeMove(IReadOnlyList<Creature> targets)
        {
            var choiceContext = new ThrowingPlayerChoiceContext();
            // 原有效果：增加力量
            await PowerCmd.Apply<StrengthPower>(choiceContext, Creature, ChargeStrength, Creature, null);

            // 新增效果：给所有玩家施加乱音（1层）
            foreach (var player in Creature.CombatState.Players)
            {
                await PowerCmd.Apply<ChaoticSoundPower>(choiceContext, player.Creature, 1, Creature, null);
            }
        }

        private async Task ScatterMove(IReadOnlyList<Creature> targets)
        {
            await DamageCmd.Attack(ScatterDamage)
                .WithHitCount(ScatterHits)
                .FromMonster(this)
                .WithAttackerFx(null, AttackSfx)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(null);
        }

        // 视觉更新
        private void UpdateVisual(string imagePath)
        {
            NCreature creatureNode = NCombatRoom.Instance?.GetCreatureNode(Creature);
            if (creatureNode == null) return;

            if (creatureNode.Visuals.GetCurrentBody() is Sprite2D sprite)
            {
                Texture2D newTexture = GD.Load<Texture2D>("res://" + imagePath);
                if (newTexture == null) return;

                sprite.Texture = newTexture;
            }
        }

        // 死亡处理（无转阶段）
        public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
        {
            await base.AfterDeath(choiceContext, creature, wasRemovalPrevented, deathAnimLength);
        }

        // 战斗开始：设置视觉，并添加高维生物（8层）
        public override async Task AfterAddedToRoom()
        {
            await base.AfterAddedToRoom();
            UpdateVisual(VisualPath);
            var choiceContext = new ThrowingPlayerChoiceContext();
            await PowerCmd.Apply<HigherDimensionalBeingPower>(choiceContext, Creature, 4, Creature, null);
        }
    }
}
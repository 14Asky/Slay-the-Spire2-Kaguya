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
    public sealed class TsukitoTenchuSingle : CustomMonsterModel
    {
        // 视觉资源路径
        private const string VisualPath = "images/packed/monster/tsukito_tenchu_phase1.png";

        // 固定血量
        public override int MinInitialHp => 350;
        public override int MaxInitialHp => 400;

        private bool _lastWasHarmony = false;

        // 第一阶段数值
        private int ChaosDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 15, 12);
        private int HarmonyBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 23, 20);
        private int HarmonyRegen => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 8, 7);
        private int StrongToneDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 12, 10);
        private const int StrongToneHits = 2;
        private const int StrongToneStrength = 2;

        public override NCreatureVisuals CreateCustomVisuals()
            => NodeFactory<NCreatureVisuals>.CreateFromScene("res://scenes/monsters/tsukito_tenchu.tscn");

        protected override MonsterMoveStateMachine GenerateMoveStateMachine()
        {
            var chaosState = new MoveState("CHAOS_SOUND", ChaosSoundMove,
                new SingleAttackIntent(() => ChaosDamage), new DebuffIntent());
            var harmonyState = new MoveState("HARMONY", HarmonyMove,
                new DefendIntent(), new BuffIntent());
            var strongToneState = new MoveState("STRONG_TONE", StrongToneMove,
                new MultiAttackIntent(StrongToneDamage, StrongToneHits), new BuffIntent());

            var phase1Random = new RandomBranchState("PHASE1_RANDOM");
            phase1Random.AddBranch(chaosState, MoveRepeatType.CanRepeatForever, 0.5f);
            phase1Random.AddBranch(strongToneState, MoveRepeatType.CanRepeatForever, 0.5f);

            var phase1Conditional = new ConditionalBranchState("PHASE1_CONDITIONAL");
            phase1Conditional.AddState(harmonyState, () => Creature.CurrentHp < Creature.MaxHp * 0.5m && !_lastWasHarmony);
            phase1Conditional.AddState(phase1Random, () => true);

            chaosState.FollowUpState = phase1Conditional;
            harmonyState.FollowUpState = phase1Conditional;
            strongToneState.FollowUpState = phase1Conditional;

            var states = new List<MonsterState>
            {
                chaosState, harmonyState, strongToneState,
                phase1Random, phase1Conditional
            };

            return new MonsterMoveStateMachine(states, chaosState);
        }

        // 第一阶段技能
        private async Task ChaosSoundMove(IReadOnlyList<Creature> targets)
        {
            await DamageCmd.Attack(ChaosDamage)
                .FromMonster(this)
                .WithAttackerFx(null, AttackSfx)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(null);
            var choiceContext = new ThrowingPlayerChoiceContext();
            foreach (var t in targets)
                await PowerCmd.Apply<ChaoticSoundPowerV2>(choiceContext, t, 1, Creature, null);
            _lastWasHarmony = false;
        }

        private async Task HarmonyMove(IReadOnlyList<Creature> targets)
        {
            await CreatureCmd.GainBlock(Creature, HarmonyBlock, ValueProp.Move, null);
            var choiceContext = new ThrowingPlayerChoiceContext();
            await PowerCmd.Apply<RegenPower>(choiceContext, Creature, HarmonyRegen, Creature, null);
            _lastWasHarmony = true;
        }

        private async Task StrongToneMove(IReadOnlyList<Creature> targets)
        {
            await DamageCmd.Attack(StrongToneDamage)
                .WithHitCount(StrongToneHits)
                .FromMonster(this)
                .WithAttackerFx(null, AttackSfx)
                .WithHitFx("vfx/vfx_attack_blunt")
                .Execute(null);
            var choiceContext = new ThrowingPlayerChoiceContext();
            await PowerCmd.Apply<StrengthPower>(choiceContext, Creature, StrongToneStrength, Creature, null);
            _lastWasHarmony = false;
        }

        // 视觉更新
        private void UpdateVisual(string imagePath)
        {
            NCreature creatureNode = NCombatRoom.Instance?.GetCreatureNode(Creature);
            if (creatureNode?.Visuals.GetCurrentBody() is Sprite2D sprite)
            {
                Texture2D newTexture = GD.Load<Texture2D>("res://" + imagePath);
                if (newTexture == null) return;
                sprite.Texture = newTexture;
            }
        }

        // 战斗开始：添加高维生物
        public override async Task AfterAddedToRoom()
        {
            await base.AfterAddedToRoom();
            UpdateVisual(VisualPath);
            var choiceContext = new ThrowingPlayerChoiceContext();
            await PowerCmd.Apply<HighDimensionalBeingPower>(choiceContext, Creature, 4, Creature, null);
        }
    }
}
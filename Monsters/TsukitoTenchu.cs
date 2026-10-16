using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using Godot;
using Kaguya.Powers;
using Kaguya.Relics;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
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
    public sealed class TsukitoTenchu : CustomMonsterModel
    {
        // 视觉资源路径
        private const string Phase1VisualPath = "images/packed/monster/tsukito_tenchu_phase1.png";
        private const string Phase2VisualPath = "images/packed/monster/tsukito_tenchu_phase2.png";

        // 阶段血量（基础值，单人模式）
        private int Phase1Hp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 200, 190);
        private int Phase2Hp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 220, 205);

        public override int MinInitialHp => Phase1Hp;
        public override int MaxInitialHp => Phase1Hp;

        private int _phase = 0;
        private bool _lastWasHarmony = false;
        private bool _hasRevived = false;
        private bool _firstPhase2Turn = false;   // 第二阶段第一回合标志

        // 第一阶段数值
        private int ChaosDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 15, 12);
        private int HarmonyBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 23, 20);
        private int HarmonyRegen => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 8, 7);
        private int StrongToneDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 12, 10);
        private const int StrongToneHits = 2;
        private const int StrongToneStrength = 2;

        // 第二阶段数值
        private int LaserDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 13, 10);
        private const int WeakAmount = 2;
        private int ChargeStrength => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 2, 1);
        private const int ScatterDamage = 1;
        private int ScatterHits => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 9, 8);

        private MoveState _phase2LaserState;
        private MoveState _phase2ChargeState;
        private MoveState _phase2ScatterState;
        private MoveState _reviveState;

        // 音乐句柄
        private int? _musicHandle;

        public override bool ShouldDisappearFromDoom => false;

        public override NCreatureVisuals CreateCustomVisuals()
            => NodeFactory<NCreatureVisuals>.CreateFromScene("res://scenes/monsters/tsukito_tenchu.tscn");

        public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await base.AfterCardPlayed(choiceContext, cardPlay);
            if (cardPlay.Card.Owner != null && cardPlay.Card is Kaguya.Cards.BackToWall)
            {
                TalkCmd.Play(L10NMonsterLookup("KAGUYA-TSUKITO_TENCHU.moves.BACK_TO_WALL.banter"), Creature, VfxColor.Red);
                var ctx = new ThrowingPlayerChoiceContext();
                await PowerCmd.Apply<StrengthPower>(ctx, Creature, 2, Creature, null);
            }
        }

        protected override MonsterMoveStateMachine GenerateMoveStateMachine()
        {
            // ---------- 第一阶段 ----------
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

            // ---------- 第二阶段 ----------
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

            // ---------- 复活状态 ----------
            _reviveState = new MoveState("REVIVE", ReviveMove, new HealIntent(), new BuffIntent())
            {
                MustPerformOnceBeforeTransitioning = true
            };

            var phaseBranch = new ConditionalBranchState("PHASE_BRANCH");
            phaseBranch.AddState(phase1Conditional, () => _phase == 0);
            phaseBranch.AddState(_phase2LaserState, () => _phase == 1 && _firstPhase2Turn);
            phaseBranch.AddState(phase2Random, () => _phase == 1);

            _reviveState.FollowUpState = phaseBranch;

            var states = new List<MonsterState>
            {
                chaosState, harmonyState, strongToneState,
                phase1Random, phase1Conditional,
                _phase2LaserState, _phase2ChargeState, _phase2ScatterState, phase2Random,
                _reviveState, phaseBranch
            };

            return new MonsterMoveStateMachine(states, chaosState);
        }

        // ---------- 第一阶段技能 ----------
        private async Task ChaosSoundMove(IReadOnlyList<Creature> targets)
        {
            await DamageCmd.Attack(ChaosDamage)
                .FromMonster(this)
                .WithAttackerFx(null, AttackSfx)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(null);
            var ctx = new ThrowingPlayerChoiceContext();
            foreach (var t in targets)
                await PowerCmd.Apply<ChaoticSoundPower>(ctx, t, 1, Creature, null);
            _lastWasHarmony = false;
        }

        private async Task HarmonyMove(IReadOnlyList<Creature> targets)
        {
            await CreatureCmd.GainBlock(Creature, HarmonyBlock, ValueProp.Move, null);
            var ctx = new ThrowingPlayerChoiceContext();
            await PowerCmd.Apply<RegenPower>(ctx, Creature, HarmonyRegen, Creature, null);
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
            var ctx = new ThrowingPlayerChoiceContext();
            await PowerCmd.Apply<StrengthPower>(ctx, Creature, StrongToneStrength, Creature, null);
            _lastWasHarmony = false;
        }

        // ---------- 第二阶段技能 ----------
        private async Task LaserCannonMove(IReadOnlyList<Creature> targets)
        {
            await DamageCmd.Attack(LaserDamage)
                .FromMonster(this)
                .WithAttackerFx(null, AttackSfx)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(null);
            var ctx = new ThrowingPlayerChoiceContext();
            foreach (var t in targets)
                await PowerCmd.Apply<WeakPower>(ctx, t, WeakAmount, Creature, null);
            _firstPhase2Turn = false;
        }

        private async Task ChargeMove(IReadOnlyList<Creature> targets)
        {
            TalkCmd.Play(L10NMonsterLookup("KAGUYA-TSUKITO_TENCHU.moves.CHARGE.banter"), Creature, VfxColor.Red);
            var ctx = new ThrowingPlayerChoiceContext();
            await PowerCmd.Apply<StrengthPower>(ctx, Creature, ChargeStrength, Creature, null);
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

        // ---------- 视觉更新 ----------
        private void UpdateVisual(string imagePath)
        {
            NCreature creatureNode = NCombatRoom.Instance?.GetCreatureNode(Creature);
            if (creatureNode == null) return;

            if (creatureNode.Visuals.GetCurrentBody() is Sprite2D sprite)
            {
                Texture2D newTexture = GD.Load<Texture2D>("res://" + imagePath);
                if (newTexture == null) return;

                sprite.Texture = newTexture;

                Vector2 originalScale = sprite.Scale;
                Tween tween = creatureNode.CreateTween();
                tween.TweenProperty(sprite, "scale", originalScale, 0.8f)
                    .From(originalScale * 0.5f)
                    .SetEase(Tween.EaseType.Out)
                    .SetTrans(Tween.TransitionType.Sine);
                tween.Parallel().TweenProperty(sprite, "modulate", Colors.White, 0.4f)
                    .From(Colors.Black);
            }
        }

        // ---------- 转阶段接口 ----------
        public async Task TriggerRevive()
        {
            if (_hasRevived) return;
            _hasRevived = true;
            await CreatureCmd.TriggerAnim(Creature, "DeadTrigger", 0f);
            SetMoveImmediate(_reviveState, forceTransition: true);
        }

        private async Task ReviveMove(IReadOnlyList<Creature> targets)
        {
            _phase = 1;
            _firstPhase2Turn = true;


            UpdateVisual(Phase2VisualPath);

            SfxCmd.Play("event:/sfx/enemy/enemy_attacks/waterfall_giant/waterfall_giant_knockout");
            await CreatureCmd.TriggerAnim(Creature, "Respawn", 0.5f);
            await Cmd.Wait(0.8f);

            int playerCount = Creature.CombatState?.Players.Count(p => p.Creature.IsAlive) ?? 1;
            int scaledHp = Phase2Hp * playerCount;
            await CreatureCmd.SetMaxHp(Creature, scaledHp);
            await CreatureCmd.Heal(Creature, scaledHp);

            await PowerCmd.Remove<ChaoticSoundPower>(Creature);
            await PowerCmd.Remove<PhaseTransitionPower>(Creature);

            _lastWasHarmony = false;
        }

        public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
        {
            if (creature != Creature) return;
            if (Creature.HasPower<PhaseTransitionPower>())
                return;
            if (_musicHandle.HasValue)
            {
                NDebugAudioManager.Instance.Stop(_musicHandle.Value);
                _musicHandle = null;
            }

            // 战斗胜利后，自动为每个存活玩家升级记忆碎片（回忆一次）
            var players = Creature.CombatState?.Players.Where(p => p.Creature.IsAlive).ToList();
            if (players != null)
            {
                foreach (var player in players)
                {
                    await UpgradeMemoryFragment(player);
                }
            }

            await base.AfterDeath(choiceContext, creature, wasRemovalPrevented, deathAnimLength);
        }

        // 新增辅助方法：升级记忆碎片
        private async Task UpgradeMemoryFragment(Player player)
        {
            // 查找当前持有的可升级记忆碎片（起始、日常、演唱会）
            var currentRelic = player.Relics.FirstOrDefault(r =>
                r is MemoryFragmentStart ||
                r is MemoryFragmentDaily ||
                r is MemoryFragmentConcert);
            if (currentRelic == null) return;

            // 确定升级目标
            RelicModel newRelic = null;
            if (currentRelic is MemoryFragmentStart)
                newRelic = ModelDb.Relic<MemoryFragmentDaily>().ToMutable();
            else if (currentRelic is MemoryFragmentDaily)
                newRelic = ModelDb.Relic<MemoryFragmentConcert>().ToMutable();
            else if (currentRelic is MemoryFragmentConcert)
                newRelic = ModelDb.Relic<MemoryFragmentResolution>().ToMutable();

            if (newRelic == null) return;

            // 执行升级：移除旧遗物，添加新遗物
            await RelicCmd.Remove(currentRelic);
            await RelicCmd.Obtain(newRelic, player);
        }

        public override async Task AfterAddedToRoom()
        {
            await base.AfterAddedToRoom();
            UpdateVisual(Phase1VisualPath);
            var ctx = new ThrowingPlayerChoiceContext();
            await PowerCmd.Apply<PhaseTransitionPower>(ctx, Creature, 1, Creature, null);
            await PowerCmd.Apply<HighDimensionalBeingPower>(ctx, Creature, 3, Creature, null);

            // 播放循环音乐（Moon.mp3）
            var stream = PreloadManager.Cache.GetAsset<AudioStream>("res://audio/Moon.mp3");
            if (stream is AudioStreamMP3 mp3)
                mp3.Loop = true;
            _musicHandle = NDebugAudioManager.Instance.Play("Moon.mp3");
        }
    }
}
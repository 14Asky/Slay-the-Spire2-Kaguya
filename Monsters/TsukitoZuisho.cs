using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using Godot;
using Kaguya.Powers;
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
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Models;

namespace Kaguya.Monsters
{
    public sealed class TsukitoZuisho : CustomMonsterModel
    {
        // 视觉资源路径（请根据实际图片位置修改）
        private const string Phase1Visual = "images/packed/monster/tsukito_zuisho_phase1.png";
        private const string Phase2Visual = "images/packed/monster/tsukito_zuisho_phase2.png";

        // 阶段血量（进阶8+ / 普通）
        private int Phase1Hp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 300, 285);
        private int Phase2Hp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 730, 700);

        public override int MinInitialHp => Phase1Hp;
        public override int MaxInitialHp => Phase1Hp;

        private int _respawns = 0;
        private bool _firstPhase2Turn = true;

        // 阶段1数值
        private int CrushDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 30, 27);
        private const int CrushStrength = 3;
        private const int DevourHeal = 30;
        private const int DevourBlock = 35;
        private int HungerStrength => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 8, 7);
        private const int HungerVulnerable = 4;

        // 阶段2数值
        private int SlamDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 16, 15);
        private const int SlamHits = 2;
        private const int SlamWeak = 2;
        private int SlashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 9, 8);
        private const int SlashHits = 4;
        private const int WoundCount = 2;
        private int ClashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 8, 7);
        private const int ClashHits = 2;
        private const int ClashBlock = 20;
        private const int ClashBlockTimes = 2;
        private const int RageVulnerable = 99;

        private MoveState _phase1CrushState, _phase1DevourState, _phase1HungerState;
        private MoveState _phase2SlamState, _phase2SlashState, _phase2ClashState, _phase2RageState;
        private MoveState _reviveState;

        // 音乐句柄
        private int? _musicHandle;

        public override bool ShouldDisappearFromDoom => false;

        public override NCreatureVisuals CreateCustomVisuals()
            => NodeFactory<NCreatureVisuals>.CreateFromScene("res://scenes/monsters/tsukito_zuisho.tscn");

        public async Task TriggerDeadState()
        {
            await CreatureCmd.TriggerAnim(Creature, "DeadTrigger", 0f);
            SetMoveImmediate(_reviveState, forceTransition: true);
        }

        protected override MonsterMoveStateMachine GenerateMoveStateMachine()
        {
            // 阶段1
            _phase1CrushState = new MoveState("CRUSH", CrushMove,
                new SingleAttackIntent(CrushDamage), new BuffIntent());
            _phase1DevourState = new MoveState("DEVOUR", DevourMove,
                new HealIntent(), new DefendIntent(), new UnknownIntent());
            _phase1HungerState = new MoveState("HUNGER", HungerMove,
                new BuffIntent(), new DebuffIntent());

            var phase1Random = new RandomBranchState("PHASE1_RANDOM");
            phase1Random.AddBranch(_phase1CrushState, 1, MoveRepeatType.CannotRepeat, 0.34f);
            phase1Random.AddBranch(_phase1DevourState, 1, MoveRepeatType.CannotRepeat, 0.33f);
            phase1Random.AddBranch(_phase1HungerState, 1, MoveRepeatType.CannotRepeat, 0.33f);

            _phase1HungerState.FollowUpState = _phase1CrushState;
            _phase1CrushState.FollowUpState = phase1Random;
            _phase1DevourState.FollowUpState = phase1Random;

            // 阶段2
            _phase2SlamState = new MoveState("SLAM", SlamMove,
                new MultiAttackIntent(SlamDamage, SlamHits), new DebuffIntent(), new UnknownIntent());
            _phase2SlashState = new MoveState("SLASH", SlashMove,
                new MultiAttackIntent(SlashDamage, SlashHits), new StatusIntent(WoundCount * 2));
            _phase2ClashState = new MoveState("CLASH", ClashMove,
                new MultiAttackIntent(ClashDamage, ClashHits), new DefendIntent());
            _phase2RageState = new MoveState("RAGE", RageMove,
                new DebuffIntent(), new BuffIntent());

            var phase2Random = new RandomBranchState("PHASE2_RANDOM");
            phase2Random.AddBranch(_phase2SlamState, MoveRepeatType.CanRepeatForever, 0.34f);
            phase2Random.AddBranch(_phase2SlashState, MoveRepeatType.CanRepeatForever, 0.33f);
            phase2Random.AddBranch(_phase2ClashState, MoveRepeatType.CanRepeatForever, 0.33f);

            var phase2Entry = new ConditionalBranchState("PHASE2_ENTRY");
            phase2Entry.AddState(_phase2RageState, () => _firstPhase2Turn);
            phase2Entry.AddState(phase2Random, () => true);

            _phase2RageState.FollowUpState = phase2Random;
            _phase2SlamState.FollowUpState = phase2Random;
            _phase2SlashState.FollowUpState = phase2Random;
            _phase2ClashState.FollowUpState = phase2Random;

            // 复活状态
            _reviveState = new MoveState("RESPAWN_MOVE", RespawnMove, new HealIntent(), new BuffIntent())
            {
                MustPerformOnceBeforeTransitioning = true
            };
            var reviveBranch = new ConditionalBranchState("REVIVE_BRANCH");
            reviveBranch.AddState(phase1Random, () => _respawns == 0);
            reviveBranch.AddState(phase2Entry, () => _respawns == 1);
            _reviveState.FollowUpState = reviveBranch;

            var allStates = new List<MonsterState>
            {
                _reviveState,
                _phase1CrushState, _phase1DevourState, _phase1HungerState, phase1Random,
                _phase2SlamState, _phase2SlashState, _phase2ClashState, _phase2RageState, phase2Random, phase2Entry,
                reviveBranch
            };
            return new MonsterMoveStateMachine(allStates, _phase1CrushState);
        }

        // 阶段1技能
        private async Task CrushMove(IReadOnlyList<Creature> targets)
        {
            await DamageCmd.Attack(CrushDamage)
                .FromMonster(this)
                .WithAttackerFx(null, AttackSfx)
                .WithHitFx("vfx/vfx_attack_blunt")
                .Execute(null);
            var choiceContext = new ThrowingPlayerChoiceContext();
            await PowerCmd.Apply<StrengthPower>(choiceContext, Creature, CrushStrength, Creature, null);
        }

        private async Task DevourMove(IReadOnlyList<Creature> targets)
        {
            await CreatureCmd.Heal(Creature, DevourHeal);
            await CreatureCmd.GainBlock(Creature, DevourBlock, ValueProp.Move, null);
            var players = targets.Select(t => t.Player).Where(p => p != null).ToList();
            foreach (var player in players)
            {
                var exhaustPile = PileType.Exhaust.GetPile(player);
                var cardsToRemove = exhaustPile.Cards.ToList();
                foreach (var card in cardsToRemove)
                {
                    await CardPileCmd.RemoveFromCombat(card);
                }
            }
        }

        private async Task HungerMove(IReadOnlyList<Creature> targets)
        {
            var choiceContext = new ThrowingPlayerChoiceContext();
            await PowerCmd.Apply<StrengthPower>(choiceContext, Creature, HungerStrength, Creature, null);
            foreach (var target in targets)
            {
                await PowerCmd.Apply<VulnerablePower>(choiceContext, target, HungerVulnerable, Creature, null);
            }
        }

        // 阶段2技能
        private async Task SlamMove(IReadOnlyList<Creature> targets)
        {
            await DamageCmd.Attack(SlamDamage)
                .WithHitCount(SlamHits)
                .FromMonster(this)
                .WithAttackerFx(null, AttackSfx)
                .WithHitFx("vfx/vfx_attack_blunt")
                .Execute(null);
            var choiceContext = new ThrowingPlayerChoiceContext();
            foreach (var target in targets)
            {
                await PowerCmd.Apply<WeakPower>(choiceContext, target, SlamWeak, Creature, null);
            }
            var players = targets.Select(t => t.Player).Where(p => p != null).ToList();
            foreach (var player in players)
            {
                var exhaustPile = PileType.Exhaust.GetPile(player);
                var cardsToRemove = exhaustPile.Cards.ToList();
                foreach (var card in cardsToRemove)
                {
                    await CardPileCmd.RemoveFromCombat(card);
                }
            }
        }

        private async Task SlashMove(IReadOnlyList<Creature> targets)
        {
            await DamageCmd.Attack(SlashDamage)
                .WithHitCount(SlashHits)
                .FromMonster(this)
                .WithAttackerFx(null, AttackSfx)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(null);
            foreach (var target in targets)
            {
                for (int i = 0; i < WoundCount; i++)
                {
                    // 修正：移除 addedByPlayer 参数
                    await CardPileCmd.AddToCombatAndPreview<Wound>(target, PileType.Draw, 1, null);
                    await CardPileCmd.AddToCombatAndPreview<Wound>(target, PileType.Discard, 1, null);
                }
            }
        }

        private async Task ClashMove(IReadOnlyList<Creature> targets)
        {
            await DamageCmd.Attack(ClashDamage)
                .WithHitCount(ClashHits)
                .FromMonster(this)
                .WithAttackerFx(null, AttackSfx)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(null);
            for (int i = 0; i < ClashBlockTimes; i++)
            {
                await CreatureCmd.GainBlock(Creature, ClashBlock, ValueProp.Move, null);
            }
        }

        private async Task RageMove(IReadOnlyList<Creature> targets)
        {
            // 移除自身所有高维生物能力
            var selfHighDim = Creature.Powers.OfType<HighDimensionalBeingPower>().ToList();
            foreach (var power in selfHighDim)
            {
                await PowerCmd.Remove(power);
            }

            var choiceContext = new ThrowingPlayerChoiceContext();
            // 给所有玩家施加99层易伤
            foreach (var target in targets)
            {
                await PowerCmd.Apply<VulnerablePower>(choiceContext, target, RageVulnerable, Creature, null);
            }
            // 给自身施加99层易伤
            await PowerCmd.Apply<VulnerablePower>(choiceContext, Creature, RageVulnerable, Creature, null);
            // 添加激昂能力
            await PowerCmd.Apply<ExcitedPower>(choiceContext, Creature, 1, Creature, null);
            _firstPhase2Turn = false;
        }

        // 视觉更新
        private void UpdateVisual(string imagePath)
        {
            var node = NCombatRoom.Instance?.GetCreatureNode(Creature);
            if (node?.Visuals.GetCurrentBody() is Sprite2D sprite)
            {
                var tex = GD.Load<Texture2D>("res://" + imagePath);
                if (tex != null)
                {
                    sprite.Texture = tex;
                    Vector2 originalScale = sprite.Scale;
                    Tween tween = node.CreateTween();
                    tween.TweenProperty(sprite, "scale", originalScale, 0.8f)
                        .From(originalScale * 0.5f)
                        .SetEase(Tween.EaseType.Out)
                        .SetTrans(Tween.TransitionType.Sine);
                    tween.Parallel().TweenProperty(sprite, "modulate", Colors.White, 0.4f)
                        .From(Colors.Black);
                }
            }
        }

        // 复活逻辑
        private async Task RespawnMove(IReadOnlyList<Creature> targets)
        {
            _respawns++;


            SfxCmd.Play("event:/sfx/enemy/enemy_attacks/waterfall_giant/waterfall_giant_knockout");
            await CreatureCmd.TriggerAnim(Creature, "Respawn", 0.5f);
            await Cmd.Wait(0.8f);

            if (_respawns == 1)
                UpdateVisual(Phase2Visual);

            int playerCount = Creature.CombatState?.Players.Count(p => p.Creature.IsAlive) ?? 1;
            int newMaxHp = Phase2Hp * playerCount;
            await CreatureCmd.SetMaxHp(Creature, newMaxHp);
            await CreatureCmd.Heal(Creature, newMaxHp);

            var choiceContext = new ThrowingPlayerChoiceContext();
            await PowerCmd.Remove<GluttonyPower>(Creature);
            await PowerCmd.Remove<ZuishoPhaseTransitionPower>(Creature);
        }

        // 死亡处理
        public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
        {
            if (creature != Creature) return;
            if (Creature.HasPower<ZuishoPhaseTransitionPower>())
                return;
            if (_musicHandle.HasValue)
            {
                NDebugAudioManager.Instance.Stop(_musicHandle.Value);
                _musicHandle = null;
            }
            await base.AfterDeath(choiceContext, creature, wasRemovalPrevented, deathAnimLength);
        }

        // 战斗开始
        public override async Task AfterAddedToRoom()
        {
            await base.AfterAddedToRoom();
            UpdateVisual(Phase1Visual);
            var choiceContext = new ThrowingPlayerChoiceContext();
            foreach (var player in Creature.CombatState.Players)
                {
                    var gluttony = (GluttonyPower)ModelDb.Power<GluttonyPower>().ToMutable();
                    gluttony.Target = player.Creature;
                    await PowerCmd.Apply(choiceContext, gluttony, Creature, 1, Creature, null);
                }
            await PowerCmd.Apply<ZuishoPhaseTransitionPower>(choiceContext, Creature, 1, Creature, null);
            await PowerCmd.Apply<HighDimensionalBeingPower>(choiceContext, Creature, 5, Creature, null);

            // 播放循环音乐（Moon.mp3）
            var stream = PreloadManager.Cache.GetAsset<AudioStream>("res://audio/Moon.mp3");
            if (stream is AudioStreamMP3 mp3)
                mp3.Loop = true;
            _musicHandle = NDebugAudioManager.Instance.Play("Moon.mp3");
        }
    }
}
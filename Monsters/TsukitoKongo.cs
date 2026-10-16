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

namespace Kaguya.Monsters
{
    public sealed class TsukitoKongo : CustomMonsterModel
    {
        // 视觉资源路径
        private const string Phase1Visual = "images/packed/monster/tsukito_kongo_phase1.png";
        private const string Phase2Visual = "images/packed/monster/tsukito_kongo_phase2.png";
        private const string Phase3Visual = "images/packed/monster/tsukito_kongo_phase3.png";

        // 各阶段血量（基础值，单人模式）
        private int Phase1Hp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 149, 145);
        private int Phase2Hp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 250, 235);
        private int Phase3Hp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 350, 345);

        public override int MinInitialHp => Phase1Hp;
        public override int MaxInitialHp => Phase1Hp;

        private int _respawns = 0;
        private bool _hasSwitchedToMegaPunch = false;
        private int _strengthToCarryOver = 0;

        // 阶段1数值
        private int PunchDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 16, 15);
        private const int PunchHitCount = 2;
        private int MegaPunchDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 41, 39);
        private const int MegaPunchHeal = 25;

        // 阶段2数值
        private int GongDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 31, 28);
        private int SoundStrikeDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 16, 15);
        private const int SoundStrikeHits = 2;
        private const int SoundStrikeStrength = 2;
        private const int ShockwaveDaze = 5;
        private const int ShockwaveBlock = 40;
        private const int ShockwaveWeak = 3;
        private const int ShockwaveStatusCount = 10;

        // 阶段3数值
        private int SmashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 39, 37);
        private int BlinkDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 30, 27);
        private int ZenDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 30, 27);
        private int ZenStrength => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 9, 8);
        private int GoldenBodyDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 39, 36);
        private const int GoldenBodyBlock = 40;

        private MoveState _phase2GongState, _phase2SoundStrikeState, _phase2ShockwaveState;
        private MoveState _phase3GoldenBodyState, _phase3SmashState, _phase3BlinkState, _phase3ZenState;
        private MoveState _deadState;
        private MoveState _megaPunchState;

        // 音乐句柄
        private int? _musicHandle;

        public override bool ShouldDisappearFromDoom => false;

        public override NCreatureVisuals CreateCustomVisuals()
            => NodeFactory<NCreatureVisuals>.CreateFromScene("res://scenes/monsters/tsukito_kongo.tscn");

        public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await base.AfterCardPlayed(choiceContext, cardPlay);
            if (cardPlay.Card.Owner != null && cardPlay.Card is Kaguya.Cards.BackToWall)
            {
                TalkCmd.Play(L10NMonsterLookup("KAGUYA-TSUKITO_KONGO.moves.BACK_TO_WALL.banter"), Creature, VfxColor.Red);
                var ctx = new ThrowingPlayerChoiceContext();
                await PowerCmd.Apply<StrengthPower>(ctx, Creature, 3, Creature, null);
            }
        }

        public async Task TriggerDeadState()
        {
            await CreatureCmd.TriggerAnim(Creature, "DeadTrigger", 0f);
            SetMoveImmediate(_deadState, forceTransition: true);
        }

        protected override MonsterMoveStateMachine GenerateMoveStateMachine()
        {
            // 阶段1
            var punchState = new MoveState("PUNCH", PunchMove,
                new MultiAttackIntent(PunchDamage, PunchHitCount));
            _megaPunchState = new MoveState("MEGA_PUNCH", MegaPunchMove,
                new SingleAttackIntent(MegaPunchDamage), new HealIntent());
            var phase1Conditional = new ConditionalBranchState("PHASE1_COND");
            phase1Conditional.AddState(_megaPunchState, () => Creature.CurrentHp < Creature.MaxHp * 0.5m);
            phase1Conditional.AddState(punchState, () => true);
            punchState.FollowUpState = phase1Conditional;
            _megaPunchState.FollowUpState = phase1Conditional;

            // 阶段2
            _phase2GongState = new MoveState("GONG", GongMove,
                new SingleAttackIntent(GongDamage), new BuffIntent());
            _phase2SoundStrikeState = new MoveState("SOUND_STRIKE", SoundStrikeMove,
                new MultiAttackIntent(SoundStrikeDamage, SoundStrikeHits), new BuffIntent());
            _phase2ShockwaveState = new MoveState("SHOCKWAVE", ShockwaveMove,
                new DebuffIntent(), new StatusIntent(ShockwaveStatusCount), new DefendIntent());

            _phase2GongState.FollowUpState = _phase2SoundStrikeState;
            _phase2SoundStrikeState.FollowUpState = _phase2ShockwaveState;
            _phase2ShockwaveState.FollowUpState = _phase2SoundStrikeState;

            // 阶段3
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

            // 复活分支
            _deadState = new MoveState("RESPAWN_MOVE", RespawnMove, new HealIntent(), new BuffIntent())
            {
                MustPerformOnceBeforeTransitioning = true
            };
            var reviveBranch = new ConditionalBranchState("REVIVE_BRANCH");
            reviveBranch.AddState(_phase2GongState, () => _respawns <= 1);
            reviveBranch.AddState(_phase3GoldenBodyState, () => _respawns == 2);
            _deadState.FollowUpState = reviveBranch;

            var allStates = new List<MonsterState>
            {
                _deadState,
                punchState, _megaPunchState, phase1Conditional,
                _phase2GongState, _phase2SoundStrikeState, _phase2ShockwaveState,
                _phase3GoldenBodyState, _phase3SmashState, _phase3BlinkState, _phase3ZenState, phase3Random,
                reviveBranch
            };
            return new MonsterMoveStateMachine(allStates, phase1Conditional);
        }

        // 阶段1技能
        private async Task PunchMove(IReadOnlyList<Creature> targets)
        {
            await DamageCmd.Attack(PunchDamage)
                .WithHitCount(PunchHitCount)
                .FromMonster(this)
                .WithAttackerFx(null, AttackSfx)
                .WithHitFx("vfx/vfx_attack_blunt")
                .Execute(null);
        }

        private async Task MegaPunchMove(IReadOnlyList<Creature> targets)
        {
            await DamageCmd.Attack(MegaPunchDamage)
                .FromMonster(this)
                .WithAttackerFx(null, AttackSfx)
                .WithHitFx("vfx/vfx_attack_blunt")
                .Execute(null);
            await CreatureCmd.Heal(Creature, MegaPunchHeal);
        }

        // 阶段2技能
        private async Task GongMove(IReadOnlyList<Creature> targets)
        {
            await DamageCmd.Attack(GongDamage)
                .FromMonster(this)
                .WithAttackerFx(null, AttackSfx)
                .WithHitFx("vfx/vfx_attack_blunt")
                .Execute(null);
            var ctx = new ThrowingPlayerChoiceContext();
            await PowerCmd.Apply<GongNoisePower>(ctx, Creature, 1, Creature, null);
        }

        private async Task SoundStrikeMove(IReadOnlyList<Creature> targets)
        {
            await DamageCmd.Attack(SoundStrikeDamage)
                .WithHitCount(SoundStrikeHits)
                .FromMonster(this)
                .WithAttackerFx(null, AttackSfx)
                .WithHitFx("vfx/vfx_attack_blunt")
                .Execute(null);
            var ctx = new ThrowingPlayerChoiceContext();
            await PowerCmd.Apply<StrengthPower>(ctx, Creature, SoundStrikeStrength, Creature, null);
        }

        private async Task ShockwaveMove(IReadOnlyList<Creature> targets)
        {
            foreach (var t in targets)
            {
                // 移除 addedByPlayer 参数，使用新版签名（最后一个参数为 creator，可传 null）
                await CardPileCmd.AddToCombatAndPreview<Dazed>(t, PileType.Draw, ShockwaveDaze, null);
                await CardPileCmd.AddToCombatAndPreview<Dazed>(t, PileType.Discard, ShockwaveDaze, null);
                var ctx = new ThrowingPlayerChoiceContext();
                await PowerCmd.Apply<WeakPower>(ctx, t, ShockwaveWeak, Creature, null);
            }
            await CreatureCmd.GainBlock(Creature, ShockwaveBlock, ValueProp.Move, null);
        }

        // 阶段3技能
        private async Task GoldenBodyMove(IReadOnlyList<Creature> targets)
        {
            await DamageCmd.Attack(GoldenBodyDamage)
                .FromMonster(this)
                .WithAttackerFx(null, AttackSfx)
                .WithHitFx("vfx/vfx_attack_blunt")
                .Execute(null);
            await CreatureCmd.GainBlock(Creature, GoldenBodyBlock, ValueProp.Move, null);
            var ctx = new ThrowingPlayerChoiceContext();
            await PowerCmd.Apply<VajraPower>(ctx, Creature, 1, Creature, null);
            if (_strengthToCarryOver > 0)
            {
                await PowerCmd.Apply<StrengthPower>(ctx, Creature, _strengthToCarryOver, Creature, null);
                _strengthToCarryOver = 0;
            }
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
            var ctx = new ThrowingPlayerChoiceContext();
            await PowerCmd.Apply<IntangiblePower>(ctx, Creature, 2, Creature, null);
        }

        private async Task ZenMove(IReadOnlyList<Creature> targets)
        {
            await DamageCmd.Attack(ZenDamage)
                .FromMonster(this)
                .WithAttackerFx(null, AttackSfx)
                .WithHitFx("vfx/vfx_attack_blunt")
                .Execute(null);
            var ctx = new ThrowingPlayerChoiceContext();
            await PowerCmd.Apply<StrengthPower>(ctx, Creature, ZenStrength, Creature, null);
        }

        // 视觉更新
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

        // 血量低于50%强制切换为蓄意轰拳
        public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature dealer, CardModel cardSource)
        {
            await base.AfterDamageReceived(choiceContext, target, result, props, dealer, cardSource);
            if (target != Creature) return;
            if (_hasSwitchedToMegaPunch) return;
            if (Creature.CurrentHp < Creature.MaxHp * 0.5m && _megaPunchState != null)
            {
                _hasSwitchedToMegaPunch = true;
                if (_respawns == 0)
                    SetMoveImmediate(_megaPunchState, forceTransition: true);
            }
        }

        // 死亡时记录一阶段力量
        public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
        {
            if (creature != Creature) return;
            if (Creature.HasPower<KongoPhaseTransitionPower>())
            {
                if (_respawns == 0 && Creature.HasPower<KongoPhaseTransitionPower>())
                {
                    int currentStrength = Creature.GetPowerAmount<StrengthPower>();
                    if (currentStrength > 0)
                        _strengthToCarryOver = currentStrength;
                }
                return;
            }
            if (_musicHandle.HasValue)
            {
                NDebugAudioManager.Instance.Stop(_musicHandle.Value);
                _musicHandle = null;
            }

            // 最终死亡时，自动升级记忆碎片（回复8生命，并升级到下一阶段）
            var players = Creature.CombatState?.Players.Where(p => p.Creature.IsAlive).ToList();
            if (players != null)
            {
                var ctx = new ThrowingPlayerChoiceContext(); // 占位上下文
                foreach (var player in players)
                {
                    // 检查是否持有可升级的记忆碎片
                    var currentRelic = player.Relics.FirstOrDefault(r =>
                        r is MemoryFragmentStart ||
                        r is MemoryFragmentDaily ||
                        r is MemoryFragmentConcert);
                    if (currentRelic != null)
                    {
                        System.Type targetType = null;
                        if (currentRelic is MemoryFragmentStart)
                            targetType = typeof(MemoryFragmentDaily);
                        else if (currentRelic is MemoryFragmentDaily)
                            targetType = typeof(MemoryFragmentConcert);
                        else if (currentRelic is MemoryFragmentConcert)
                            targetType = typeof(MemoryFragmentResolution);

                        if (targetType != null)
                        {
                            await RelicCmd.Remove(currentRelic);
                            RelicModel newRelic = null;
                            if (targetType == typeof(MemoryFragmentDaily))
                                newRelic = ModelDb.Relic<MemoryFragmentDaily>().ToMutable();
                            else if (targetType == typeof(MemoryFragmentConcert))
                                newRelic = ModelDb.Relic<MemoryFragmentConcert>().ToMutable();
                            else if (targetType == typeof(MemoryFragmentResolution))
                                newRelic = ModelDb.Relic<MemoryFragmentResolution>().ToMutable();

                            if (newRelic != null)
                                await RelicCmd.Obtain(newRelic, player);
                            // 回复8点生命
                            await CreatureCmd.Heal(player.Creature, 8, true);
                        }
                    }
                }
            }

            await base.AfterDeath(choiceContext, creature, wasRemovalPrevented, deathAnimLength);
        }

        // 复活逻辑
        private async Task RespawnMove(IReadOnlyList<Creature> targets)
        {
            _respawns++;


            SfxCmd.Play("event:/sfx/enemy/enemy_attacks/waterfall_giant/waterfall_giant_knockout");
            await CreatureCmd.TriggerAnim(Creature, "Respawn", 0.5f);
            await Cmd.Wait(0.8f);

            switch (_respawns)
            {
                case 1: UpdateVisual(Phase2Visual); break;
                case 2: UpdateVisual(Phase3Visual); break;
            }

            int playerCount = Creature.CombatState?.Players.Count(p => p.Creature.IsAlive) ?? 1;
            int baseHp = _respawns == 1 ? Phase2Hp : Phase3Hp;
            int scaledHp = baseHp * playerCount;

            await CreatureCmd.SetMaxHp(Creature, scaledHp);
            await CreatureCmd.Heal(Creature, scaledHp);

            Creature.GetPower<KongoPhaseTransitionPower>()?.DoRevive();

            if (_respawns == 2)
            {
                await PowerCmd.Remove<KongoPhaseTransitionPower>(Creature);
            }
        }

        // 战斗开始
        public override async Task AfterAddedToRoom()
        {
            await base.AfterAddedToRoom();
            UpdateVisual(Phase1Visual);
            var ctx = new ThrowingPlayerChoiceContext();
            await PowerCmd.Apply<SandfieldHeroismPower>(ctx, Creature, 3, Creature, null);
            await PowerCmd.Apply<KongoPhaseTransitionPower>(ctx, Creature, 1, Creature, null);
            await PowerCmd.Apply<UnbreakablePower>(ctx, Creature, 80, Creature, null);
            await PowerCmd.Apply<HighDimensionalBeingPower>(ctx, Creature, 7, Creature, null);

            // 播放循环音乐（Moon.mp3）
            var stream = PreloadManager.Cache.GetAsset<AudioStream>("res://audio/Moon.mp3");
            if (stream is AudioStreamMP3 mp3)
                mp3.Loop = true;
            _musicHandle = NDebugAudioManager.Instance.Play("Moon.mp3");
        }
    }
}
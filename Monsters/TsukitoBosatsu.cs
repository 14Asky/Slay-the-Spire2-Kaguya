using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using Godot;
using Kaguya;
using Kaguya.Monsters;
using Kaguya.Powers;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.Combat;
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
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.ValueProps;
using System;
using Timer = Godot.Timer;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.Monsters
{
    public sealed class TsukitoBosatsu : CustomMonsterModel
    {
        private const string Phase1Visual = "images/packed/monster/tsukito_bosatsu_phase1.png";
        private const string Phase2Visual = "images/packed/monster/tsukito_bosatsu_phase2.png";

        private const int PHASE1_MAX_HP = 200000;
        private const int PHASE2_MAX_HP = 1000000;
        
        private int _turnCount = 0;
        private int _respawns = 0;
        private bool _hasSummoned;
        private HashSet<Type> _summonedTypes;
        private EndTimerPower _turnCounterPower;
        private Timer _moonTextTimer;
        private Timer _kaguyaTextTimer;

        private int _moonTextLineIndex;
        private int _lastRespawns = 0;
        private int _moonTextTurn = 0;
        private int _lastTrackedTurnCount = -1;
        private HashSet<string> _playedAudios = new HashSet<string>();

        private static readonly Dictionary<string, string> _audioMap = new Dictionary<string, string>
        {
            { "我是从月亮上来的！", "FromMoon.mp3" },
            { "我才不要这么烂的结局，我就想要快乐的结局！", "DontWantBad.mp3" },
            { "我要自己创造一个快乐的结局，然后带你一起到达！", "LetYouGood.mp3" },
            { "这样就能随时和DOGE在一起了", "DOGE.mp3" },
            { "超好玩的！", "ReallyJoy.mp3" },
            { "教教我嘛，彩叶~", "TeachMe.mp3" },
            { "呐呐，我开始当主播了，怎么样？", "Liver.mp3" },
            { "彩叶，当我的制作人吧！", "zhizuoren.mp3" },
            { "这样下去赢不了的...", "Nowin.mp3" },
            { "这是独属于我们的手势，只属于我们", "jinxianbici.mp3" },
            { "我也跟你提过很多过分的要求...", "guofenyaoqiu.mp3" },
            { "现在还很烫，吃之前记得先吹一吹哦", "Blow.mp3" },
            { "我要不跟你结婚好了~", "Marry.mp3" },
            { "但我还有好多好多事情想做呢...", "xiangzuoshi.mp3" },
            { "你在学我~", "xuewo.mp3" },
            { "每天都一模一样，无聊的要死", "EveryDayBoring.mp3" },
            { "可是...大家还是有在压抑自己最真实的感觉吧", "yayi.mp3" },
            { "为了更重要的某样东西", "gengzhongyao.mp3" },
            { "毕竟我是抛下工作来这里的嘛...", "paoxia.mp3" },
            { "我好像真的是辉夜姬呢...", "PrincessKaguya.mp3" },
            { "这就是我的结局，我要开心地奔向我的命运！", "jieshou.mp3" },
            { "当然说实话，我其实也还想跟你唱更多的歌", "xainggenni.mp3" },
            { "彩排太辛苦了，我肚子都饿了", "lianxixinku.mp3" },
            { "等结束了我们去吃松饼好不好？", "chison.mp3" },
        };
        private int _lastMoonTextTurn = -1;

        private static readonly string[][] _moonTextScripts = new string[][] {
            new[] { "我是从月亮上来的！" },
            new[] { "我才不要这么烂的结局，我就想要快乐的结局！", "我要自己创造一个快乐的结局，然后带你一起到达！" },
            new[] { "这样就能随时和DOGE在一起了", "超好玩的！" },
            new[] { "教教我嘛，彩叶~", "呐呐，我开始当主播了，怎么样？" },
            new[] { "彩叶，当我的制作人吧！", "这样下去赢不了的..." },
            new[] { "这是独属于我们的手势，只属于我们", "我也跟你提过很多过分的要求...", "现在还很烫，吃之前记得先吹一吹哦" },
            new[] { "彩排太辛苦了，我肚子都饿了", "等结束了我们去吃松饼好不好？" },
            new[] { "我要不跟你结婚好了~", "但我还有好多好多事情想做呢..." },
            new[] { "你在学我~", "每天都一模一样，无聊的要死" },
            new[] { "可是...大家还是有在压抑自己最真实的感觉吧", "为了更重要的某样东西" },
            new[] { "毕竟我是抛下工作来这里的嘛...", "我好像真的是辉夜姬呢..." },
            new[] { "这就是我的结局，我要开心地奔向我的命运！", "当然说实话，我其实也还想跟你唱更多的歌" },
        };
        private static readonly Type[] _phase1SummonTypes = new Type[]
        {
            typeof(TsukitoBagSingle),
            typeof(TsukitoKongoTwoPhase),
            typeof(TsukitoTenchuSingle),
            typeof(TsukitoZuishoSingle)
        };

        private static readonly Type[] _phase2SummonTypes = new Type[]
        {
            typeof(TsukitoTenchuPhase2Only),
            typeof(TsukitoKongoPhase3Only),
            typeof(TsukitoZuishoPhase2Only)
        };

        private Type[] _currentSummonTypes = _phase1SummonTypes;

        private MonsterModel _bagSingle, _kongoTwoPhase, _tenchuSingle, _zuishoSingle;
        private MonsterModel _tenchuPhase2, _kongoPhase3, _zuishoPhase2;

        private MoveState _startState, _majestyState, _bindState, _gazeState, _encourageState, _reviveState, _annihilationState;
        private MoveState _respawnStateMachine;

        private int? _musicHandle;

        public override int MinInitialHp => (int)(_respawns == 0 ? PHASE1_MAX_HP : PHASE2_MAX_HP);
        public override int MaxInitialHp => MinInitialHp;

        public override bool ShouldDisappearFromDoom => false;

        public override NCreatureVisuals CreateCustomVisuals()
            => NodeFactory<NCreatureVisuals>.CreateFromScene("res://scenes/monsters/tsukito_bosatsu.tscn");

        public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await base.AfterCardPlayed(choiceContext, cardPlay);
            if (cardPlay.Card.Owner != null && cardPlay.Card is Kaguya.Cards.BackToWall)
            {
                TalkCmd.Play(L10NMonsterLookup("KAGUYA-TSUKITO_TENCHU.moves.BACK_TO_WALL.banter"), Creature, VfxColor.Red);
                var ctx = new ThrowingPlayerChoiceContext();
                await PowerCmd.Apply<StrengthPower>(ctx, Creature, 5, Creature, null);
            }
        }

        public override async Task AfterAddedToRoom()
        {
            await base.AfterAddedToRoom();

            var stream = PreloadManager.Cache.GetAsset<AudioStream>("res://audio/Shun.mp3");
            if (stream is AudioStreamMP3 mp3)
                mp3.Loop = true;
            _musicHandle = NDebugAudioManager.Instance.Play("Shun.mp3");

            if (KaguyaModConfig.PlayProcessAnimations)
                await VideoPlayerHelper.PlayFullscreenVideo("res://videoes/BosatsuStart.ogv");

            int playerCount = Creature.CombatState?.Players.Count(p => p.Creature.IsAlive) ?? 1;
            int scaledHp = PHASE1_MAX_HP * playerCount;
            await CreatureCmd.SetMaxAndCurrentHp(Creature, scaledHp);

            _bagSingle = ModelDb.Monster<TsukitoBagSingle>();
            _kongoTwoPhase = ModelDb.Monster<TsukitoKongoTwoPhase>();
            _tenchuSingle = ModelDb.Monster<TsukitoTenchuSingle>();
            _zuishoSingle = ModelDb.Monster<TsukitoZuishoSingle>();
            _tenchuPhase2 = ModelDb.Monster<TsukitoTenchuPhase2Only>();
            _kongoPhase3 = ModelDb.Monster<TsukitoKongoPhase3Only>();
            _zuishoPhase2 = ModelDb.Monster<TsukitoZuishoPhase2Only>();

            _summonedTypes = new HashSet<Type>();
            _hasSummoned = false;
            var ctx = new ThrowingPlayerChoiceContext();
            await PowerCmd.Apply<BosatsuPhaseTransitionPower>(ctx, Creature, 1, Creature, null);
            foreach (var player in Creature.CombatState.Players)
            {
                await PowerCmd.Apply<OppressivePressurePower>(ctx, player.Creature, 1, Creature, null);
            }

            // MoonText
            SpawnAtRandom("我是从月亮上来的！");

            _kaguyaTextTimer = new Timer();
            _kaguyaTextTimer.WaitTime = 6f;
            _kaguyaTextTimer.OneShot = false;
            _kaguyaTextTimer.Autostart = true;
            NCombatRoom.Instance.AddChild(_kaguyaTextTimer);
            _kaguyaTextTimer.Timeout += SpawnMoonTextLine;
            await PowerCmd.Apply<HigherDimensionalBeingPower>(ctx, Creature, 12, Creature, null);

            // 重置回合计数为 1 层，重新开始计数
            _turnCounterPower = await PowerCmd.Apply<EndTimerPower>(ctx, Creature, 1, Creature, null) as EndTimerPower;

        }

        public async Task TriggerRevive()
        {
            SetMoveImmediate(_respawnStateMachine, forceTransition: true);
        }

        private bool IsMoonPerson(Creature creature)
        {
            if (creature == Creature) return false;
            if (creature.Monster == null) return false;
            var type = creature.Monster.GetType();
            return _currentSummonTypes.Contains(type);
        }

        private int CountMoonPersons()
        {
            return Creature.CombatState.Enemies.Count(c => IsMoonPerson(c));
        }

        private bool CanSummonNewType()
        {
            return _currentSummonTypes.Any(t => !_summonedTypes.Contains(t));
        }

        public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
        {
            if (creature == Creature && !Creature.HasPower<BosatsuPhaseTransitionPower>())
            {
                if (_musicHandle.HasValue)
                {
                    NDebugAudioManager.Instance.Stop(_musicHandle.Value);
                    _musicHandle = null;
                }
            }

            if (creature == Creature && Creature.HasPower<BosatsuPhaseTransitionPower>())
                return;

            if (creature != Creature && IsMoonPerson(creature))
            {
                decimal fraction = (_respawns == 0) ? 0.25m : (1m / 3m);
                int totalDamage = (int)Math.Round(Creature.MaxHp * fraction, MidpointRounding.AwayFromZero);
                await CreatureCmd.Damage(choiceContext, Creature, totalDamage, ValueProp.Unblockable | ValueProp.Unpowered, null, null);

                // 击杀月人减少计数（每次-2，最低保持为 1）
                if (Creature.IsAlive && _turnCounterPower != null)
                {
                    int currentAmount = _turnCounterPower.Amount;
                    int newAmount = Math.Max(1, currentAmount - 2);
                    int offset = newAmount - currentAmount;
                    if (offset != 0)
                    {
                        _turnCount = newAmount;
                        await PowerCmd.ModifyAmount(choiceContext, _turnCounterPower, offset, null, null);
                        await CheckDespair();
                    }
                }

                _hasSummoned = false;
                if (Creature.IsAlive)
                    SetMoveImmediate(_reviveState, forceTransition: true);
            }

            await base.AfterDeath(choiceContext, creature, wasRemovalPrevented, deathAnimLength);
        }

        public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
        {
            if (side != Creature.Side) return;
            if (CountMoonPersons() == 0)
                _hasSummoned = false;
            await Task.CompletedTask;
        }

        protected override MonsterMoveStateMachine GenerateMoveStateMachine()
        {
            _startState = new MoveState("START", StartMove, new SummonIntent(), new DebuffIntent(), new BuffIntent());
            _majestyState = new MoveState("MAJESTY", MajestyMove, new DefendIntent(), new UnknownIntent());
            _bindState = new MoveState("BIND", BindMove, new DebuffIntent());
            _gazeState = new MoveState("GAZE", GazeMove, new SingleAttackIntent(20));
            _encourageState = new MoveState("ENCOURAGE", EncourageMove, new BuffIntent(), new HealIntent());
            _reviveState = new MoveState("REVIVE", ReviveMove, new SummonIntent());
            _annihilationState = new MoveState("ANNIHILATION", AnnihilationMove, new MultiAttackIntent(5, 10));

            var normalRandom = new RandomBranchState("NORMAL_RANDOM");
            normalRandom.AddBranch(_majestyState, MoveRepeatType.CannotRepeat, 0.33f);
            normalRandom.AddBranch(_gazeState, MoveRepeatType.CannotRepeat, 0.33f);
            normalRandom.AddBranch(_encourageState, MoveRepeatType.CanRepeatForever, 0.34f);

            var mainBranch = new ConditionalBranchState("MAIN_BRANCH");
            mainBranch.AddState(_startState, () => _turnCount == 0);
            mainBranch.AddState(_bindState, () => _turnCount == 1 && _respawns == 0);
            mainBranch.AddState(_annihilationState, () => CountMoonPersons() == 0 && !CanSummonNewType());
            mainBranch.AddState(_reviveState, () => CountMoonPersons() == 0);
            mainBranch.AddState(normalRandom, () => true);

            _startState.FollowUpState = mainBranch;
            _majestyState.FollowUpState = mainBranch;
            _bindState.FollowUpState = mainBranch;
            _gazeState.FollowUpState = mainBranch;
            _encourageState.FollowUpState = mainBranch;
            _reviveState.FollowUpState = mainBranch;
            _annihilationState.FollowUpState = mainBranch;

            _respawnStateMachine = new MoveState("RESPAWN", RespawnMove, new HealIntent(), new BuffIntent())
            {
                MustPerformOnceBeforeTransitioning = true
            };
            _respawnStateMachine.FollowUpState = mainBranch;

            var allStates = new List<MonsterState>
            {
                _startState, _majestyState, _bindState, _gazeState, _encourageState, _reviveState, _annihilationState,
                normalRandom, mainBranch, _respawnStateMachine
            };
            return new MonsterMoveStateMachine(allStates, mainBranch);
        }

        private async Task IncrementTurn()
        {
            _turnCount++;
            if (_turnCounterPower != null)
            {
                await PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), _turnCounterPower, 1, null, null);
                await CheckDespair();
            }
        }

        private async Task CheckDespair()
        {
            if (_turnCounterPower != null && _turnCounterPower.Amount >= 7)
            {
                // 触发绝望，移除计数能力防止重复
                var alivePlayers = Creature.CombatState.Players
                    .Select(p => p.Creature)
                    .Where(c => c.IsAlive)
                    .ToList();
                await TriggerDespair(alivePlayers);

                await PowerCmd.Remove<EndTimerPower>(Creature);
                _turnCounterPower = null;
            }
        }

        private async Task TriggerDespair(List<Creature> targets)
        {
            var alivePlayers = targets.Where(t => t.IsAlive && t.Player != null).ToList();
            var ctx = new ThrowingPlayerChoiceContext();

            foreach (var player in alivePlayers)
            {
                var buffs = player.Powers.Where(p => p.Type == PowerType.Buff).ToList();
                foreach (var power in buffs)
                {
                    if (power is BattleStartPower ||
                        power is FairDuelPower ||
                        power is GoodEndingPower ||
                        power is OppressivePressurePower)
                        continue;

                    if (power is ArtifactPower || power is HigherDimensionalBeingPower)
                    {
                        await PowerCmd.Remove(power);
                    }
                    else
                    {
                        decimal reduceAmount = power.Amount - 1;
                        if (reduceAmount > 0)
                            await PowerCmd.ModifyAmount(ctx, power, -reduceAmount, null, null);
                    }
                }

                await PowerCmd.Apply<DoomPower>(ctx, player, 99999, Creature, null);
            }

            var doomed = DoomPower.GetDoomedCreatures(alivePlayers);
            await DoomPower.DoomKill(doomed);

            foreach (var player in alivePlayers)
            {
                if (player.HasPower<DoomPower>())
                    await PowerCmd.Remove<DoomPower>(player);
            }
        }

        private async Task AnnihilationMove(IReadOnlyList<Creature> targets)
        {
            await DamageCmd.Attack(5)
                .WithHitCount(10)
                .FromMonster(this)
                .WithAttackerFx(null, AttackSfx)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(null);
            await IncrementTurn();
        }

        private async Task StartMove(IReadOnlyList<Creature> targets)
        {
            if (_respawns == 1)
            {
                var room = NCombatRoom.Instance;
                if (room != null)
                    if (KaguyaModConfig.PlayProcessAnimations)
                        await KaguyaFlashbackOverlay.PlayNumberedAsync(room, "res://images/events/flashback_", ".png", 6, tailText: "彩叶，最喜欢你了...");
                var ngCtx = new ThrowingPlayerChoiceContext();
                foreach (var player in Creature.CombatState.Players)
                {
                    if (player.Creature.IsAlive)
                        await PowerCmd.Apply<LongingPower>(ngCtx, player.Creature, 1, Creature, null);
                }

            }

            if (_turnCount > 0 && _moonTextTimer != null) { _moonTextTimer.QueueFree(); _moonTextTimer = null; }
            await TrySummonMoonPerson();
            var alivePlayers = Creature.CombatState.Players.Select(p => p.Creature).Where(c => c.IsAlive).ToList();
            var ctx = new ThrowingPlayerChoiceContext();
            foreach (var player in alivePlayers)
            {
                await PowerCmd.Apply<FairDuelPower>(ctx, player, 1, Creature, null);
                await PowerCmd.Apply<VulnerablePower>(ctx, player, 99, Creature, null);
                await PowerCmd.Apply<WeakPower>(ctx, player, 99, Creature, null);
                await PowerCmd.Apply<FrailPower>(ctx, player, 99, Creature, null);
            }
            var firstPlayer = Creature.CombatState.Players.FirstOrDefault();
            if (firstPlayer != null)
            {
                await PowerCmd.Apply<BattleStartPower>(ctx, firstPlayer.Creature, 2, Creature, null);
            }
            await IncrementTurn();
        }

        private async Task MajestyMove(IReadOnlyList<Creature> targets)
        {
            var allies = Creature.CombatState.GetTeammatesOf(Creature);
            foreach (var ally in allies)
            {
                await CreatureCmd.GainBlock(ally, 40, ValueProp.Move, null);
            }
            foreach (var player in Creature.CombatState.Players)
            {
                var exhaustPile = PileType.Exhaust.GetPile(player);
                var cards = exhaustPile.Cards.ToList();
                foreach (var card in cards)
                {
                    await CardPileCmd.RemoveFromCombat(card);
                }
            }
            await IncrementTurn();
        }

        private async Task BindMove(IReadOnlyList<Creature> targets)
        {
            var ctx = new ThrowingPlayerChoiceContext();
            foreach (var target in targets)
            {
                await PowerCmd.Apply<ChainsOfBindingPower>(ctx, target, 2, Creature, null);
            }
            await IncrementTurn();
        }

        private async Task GazeMove(IReadOnlyList<Creature> targets)
        {
            await DamageCmd.Attack(20)
                .FromMonster(this)
                .WithAttackerFx(null, AttackSfx)
                .WithHitFx("vfx/vfx_attack_blunt")
                .Execute(null);
            foreach (var player in Creature.CombatState.Players)
            {
                var hand = PileType.Hand.GetPile(player).Cards.ToList();
                if (hand.Count == 0) continue;
                var card = base.Rng.NextItem(hand);
                if (card != null)
                {
                    await CardCmd.Exhaust(new ThrowingPlayerChoiceContext(), card);
                }
            }
            await IncrementTurn();
        }

        private async Task EncourageMove(IReadOnlyList<Creature> targets)
        {
            var allies = Creature.CombatState.GetTeammatesOf(Creature);
            var ctx = new ThrowingPlayerChoiceContext();
            foreach (var ally in allies)
            {
                await PowerCmd.Apply<RegenPower>(ctx, ally, 20, Creature, null);
                await PowerCmd.Apply<StrengthPower>(ctx, ally, 3, Creature, null);
            }
            await IncrementTurn();
        }

        private async Task ReviveMove(IReadOnlyList<Creature> targets)
        {
            if (_turnCount > 0 && _moonTextTimer != null) { _moonTextTimer.QueueFree(); _moonTextTimer = null; }
            await TrySummonMoonPerson();
            await IncrementTurn();
        }

        private async Task RespawnMove(IReadOnlyList<Creature> targets)
        {
            _respawns = 1;
            _turnCount = 0;
            _summonedTypes.Clear();
            _hasSummoned = false;
            _currentSummonTypes = _phase2SummonTypes;


            UpdateVisual(Phase2Visual);

            var minions = Creature.CombatState.Enemies.Where(e => e != Creature).ToList();
            foreach (var minion in minions)
            {
                await CreatureCmd.Kill(minion);
            }

            int playerCount = Creature.CombatState?.Players.Count(p => p.Creature.IsAlive) ?? 1;
            int newMaxHp = PHASE2_MAX_HP * playerCount;
            await CreatureCmd.SetMaxAndCurrentHp(Creature, newMaxHp);

            await PowerCmd.Remove<BosatsuPhaseTransitionPower>(Creature);

            // 重置回合计数为 1 层，重新开始计数
            await PowerCmd.Remove<EndTimerPower>(Creature);
            _turnCounterPower = await PowerCmd.Apply<EndTimerPower>(new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null) as EndTimerPower;

            if (_musicHandle.HasValue)
            {
                NDebugAudioManager.Instance.Stop(_musicHandle.Value);
                _musicHandle = null;
            }
            var stream = PreloadManager.Cache.GetAsset<AudioStream>("res://audio/Reply.mp3");
            if (stream is AudioStreamMP3 mp3)
                mp3.Loop = true;
            _musicHandle = NDebugAudioManager.Instance.Play("Reply.mp3");
        }

        private async Task TrySummonMoonPerson()
        {
            if (_hasSummoned) return;

            List<Type> availableTypes;
            if (_summonedTypes.Count == 0)
            {
                availableTypes = _currentSummonTypes
                    .Where(t => t != typeof(TsukitoZuishoSingle) && t != typeof(TsukitoKongoTwoPhase))
                    .ToList();
            }
            else
            {
                availableTypes = _currentSummonTypes.Where(t => !_summonedTypes.Contains(t)).ToList();
            }

            if (availableTypes.Count == 0) return;
            var chosenType = base.Rng.NextItem(availableTypes);
            _summonedTypes.Add(chosenType);
            _hasSummoned = true;
            await SummonMoonPersonOfType(chosenType);
        }

        private MonsterModel GetMoonPersonTemplate(Type type)
        {
            if (type == typeof(TsukitoBagSingle)) return _bagSingle;
            if (type == typeof(TsukitoKongoTwoPhase)) return _kongoTwoPhase;
            if (type == typeof(TsukitoTenchuSingle)) return _tenchuSingle;
            if (type == typeof(TsukitoZuishoSingle)) return _zuishoSingle;
            if (type == typeof(TsukitoTenchuPhase2Only)) return _tenchuPhase2;
            if (type == typeof(TsukitoKongoPhase3Only)) return _kongoPhase3;
            if (type == typeof(TsukitoZuishoPhase2Only)) return _zuishoPhase2;
            return null;
        }

        private async Task SummonMoonPersonOfType(Type type)
        {
            var template = GetMoonPersonTemplate(type);
            if (template == null) return;
            var instance = template.ToMutable();
            await CreatureCmd.Add(instance, Creature.CombatState);
        }

        private void UpdateVisual(string imagePath)
        {
            var node = NCombatRoom.Instance?.GetCreatureNode(Creature);
            if (node?.Visuals.GetCurrentBody() is Sprite2D sprite)
            {
                var tex = GD.Load<Texture2D>("res://" + imagePath);
                if (tex != null)
                    sprite.Texture = tex;
            }
        }

        private void SpawnMoonTextLine()
        {
            if (_lastRespawns != _respawns)
            {
                _lastRespawns = _respawns;
                _moonTextTurn = 0;
                _moonTextLineIndex = 0;
                _lastTrackedTurnCount = _turnCount;
            }
            else if (_lastTrackedTurnCount == -1)
            {
                _lastTrackedTurnCount = _turnCount;
            }
            else if (_turnCount != _lastTrackedTurnCount)
            {
                if (_turnCount > _lastTrackedTurnCount)
                    _moonTextTurn++;
                _lastTrackedTurnCount = _turnCount;
                _moonTextLineIndex = 0;
            }
            int turnIndex = _moonTextTurn % 12;
            string[] lines = _moonTextScripts[turnIndex];
            if (lines.Length == 0) return;

            if (_moonTextLineIndex >= lines.Length)
                _moonTextLineIndex = 0;
            string line = lines[_moonTextLineIndex];
            _moonTextLineIndex++;
            SpawnAtRandom(line);
        }

        private void SpawnAtRandom(string text)
        {
            if (_audioMap.TryGetValue(text, out string audioPath) && _playedAudios.Add(text))
            {
                NDebugAudioManager.Instance.Play(audioPath, 5.6f);
            }
            float x = (float)GD.RandRange(150.0, 800.0);
            float y = (float)GD.RandRange(200.0, 650.0);
            KaguyaMoonText.SpawnInstant(
                "[color=gold]" + text + "[/color]",
                new Vector2(x, y));
        }
    }
}

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
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.Monsters
{
    public sealed class TsukitoZuishoPhase2Only : CustomMonsterModel
    {
        private const string VisualPath = "images/packed/monster/tsukito_zuisho_phase2.png";

        private int Phase2Hp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 950, 900);
        public override int MinInitialHp => Phase2Hp;
        public override int MaxInitialHp => Phase2Hp;

        private bool _firstTurn = true;   // 第一回合固定愤懑

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

        private MoveState _slamState, _slashState, _clashState, _rageState;

        public override NCreatureVisuals CreateCustomVisuals()
            => NodeFactory<NCreatureVisuals>.CreateFromScene("res://scenes/monsters/tsukito_zuisho.tscn");

        protected override MonsterMoveStateMachine GenerateMoveStateMachine()
        {
            _slamState = new MoveState("SLAM", SlamMove,
                new MultiAttackIntent(SlamDamage, SlamHits), new DebuffIntent(), new UnknownIntent());
            _slashState = new MoveState("SLASH", SlashMove,
                new MultiAttackIntent(SlashDamage, SlashHits), new StatusIntent(WoundCount * 2));
            _clashState = new MoveState("CLASH", ClashMove,
                new MultiAttackIntent(ClashDamage, ClashHits), new DefendIntent());
            _rageState = new MoveState("RAGE", RageMove,
                new DebuffIntent(), new BuffIntent());

            var randomPool = new RandomBranchState("RANDOM_POOL");
            randomPool.AddBranch(_slamState, MoveRepeatType.CanRepeatForever, 0.34f);
            randomPool.AddBranch(_slashState, MoveRepeatType.CanRepeatForever, 0.33f);
            randomPool.AddBranch(_clashState, MoveRepeatType.CanRepeatForever, 0.33f);

            var mainBranch = new ConditionalBranchState("MAIN_BRANCH");
            mainBranch.AddState(_rageState, () => _firstTurn);
            mainBranch.AddState(randomPool, () => true);

            _rageState.FollowUpState = randomPool;
            _slamState.FollowUpState = randomPool;
            _slashState.FollowUpState = randomPool;
            _clashState.FollowUpState = randomPool;

            var states = new List<MonsterState> { _slamState, _slashState, _clashState, _rageState, randomPool, mainBranch };
            return new MonsterMoveStateMachine(states, mainBranch);
        }

        private async Task SlamMove(IReadOnlyList<Creature> targets)
        {
            await DamageCmd.Attack(SlamDamage)
                .WithHitCount(SlamHits)
                .FromMonster(this)
                .WithAttackerFx(null, AttackSfx)
                .WithHitFx("vfx/vfx_attack_blunt")
                .Execute(null);
            var choiceContext = new ThrowingPlayerChoiceContext();
            foreach (var t in targets)
                await PowerCmd.Apply<WeakPower>(choiceContext, t, SlamWeak, Creature, null);
            var players = targets.Select(t => t.Player).Where(p => p != null).ToList();
            foreach (var player in players)
            {
                var exhaustPile = PileType.Exhaust.GetPile(player);
                var cards = exhaustPile.Cards.ToList();
                foreach (var card in cards)
                    await CardPileCmd.RemoveFromCombat(card);
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
                    // 修正：移除 addedByPlayer 参数，使用 creator = null
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
                await CreatureCmd.GainBlock(Creature, ClashBlock, ValueProp.Move, null);
        }

        private async Task RageMove(IReadOnlyList<Creature> targets)
        {
            var choiceContext = new ThrowingPlayerChoiceContext();
            foreach (var target in targets)
                await PowerCmd.Apply<VulnerablePower>(choiceContext, target, RageVulnerable, Creature, null);
            await PowerCmd.Apply<VulnerablePower>(choiceContext, Creature, RageVulnerable, Creature, null);
            await PowerCmd.Apply<ExcitedPower>(choiceContext, Creature, 1, Creature, null);
            _firstTurn = false;
        }

        private void UpdateVisual(string imagePath)
        {
            var node = NCombatRoom.Instance?.GetCreatureNode(Creature);
            if (node?.Visuals.GetCurrentBody() is Sprite2D sprite)
            {
                var tex = GD.Load<Texture2D>("res://" + imagePath);
                if (tex != null) sprite.Texture = tex;
            }
        }

        public override async Task AfterAddedToRoom()
        {
            await base.AfterAddedToRoom();
            UpdateVisual(VisualPath);
            var choiceContext = new ThrowingPlayerChoiceContext();
            await PowerCmd.Apply<ExcitedPower>(choiceContext, Creature, 1, Creature, null);
            await PowerCmd.Apply<HighDimensionalBeingPower>(choiceContext, Creature, 10, Creature, null);
        }
    }
}
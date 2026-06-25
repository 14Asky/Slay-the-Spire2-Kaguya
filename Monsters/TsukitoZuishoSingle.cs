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
using MegaCrit.Sts2.Core.Models;

namespace Kaguya.Monsters
{
    public sealed class TsukitoZuishoSingle : CustomMonsterModel
    {
        // 视觉资源路径
        private const string Phase1Visual = "images/packed/monster/tsukito_zuisho_phase1.png";

        // 固定血量
        public override int MinInitialHp => 380;
        public override int MaxInitialHp => 400;

        private bool _lastWasDevour = false;   // 用于吞噬冷却? 原版使用随机分支冷却，不需要额外标志

        // 阶段1数值（与原版一致）
        private int CrushDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 30, 27);
        private const int CrushStrength = 3;
        private const int DevourHeal = 30;
        private const int DevourBlock = 35;
        private int HungerStrength => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 8, 7);
        private const int HungerVulnerable = 4;

        private MoveState _phase1CrushState, _phase1DevourState, _phase1HungerState;

        public override NCreatureVisuals CreateCustomVisuals()
            => NodeFactory<NCreatureVisuals>.CreateFromScene("res://scenes/monsters/tsukito_zuisho.tscn");

        protected override MonsterMoveStateMachine GenerateMoveStateMachine()
        {
            // 阶段1（完整保留原版第一阶段逻辑）
            _phase1CrushState = new MoveState("CRUSH", CrushMove,
                new SingleAttackIntent(CrushDamage), new BuffIntent());
            _phase1DevourState = new MoveState("DEVOUR", DevourMove,
                new HealIntent(), new DefendIntent(), new UnknownIntent());
            _phase1HungerState = new MoveState("HUNGER", HungerMove,
                new BuffIntent(), new DebuffIntent());

            // 三个技能均不能连续使用（冷却1回合，不能重复）
            var phase1Random = new RandomBranchState("PHASE1_RANDOM");
            phase1Random.AddBranch(_phase1CrushState, 1, MoveRepeatType.CannotRepeat, 0.34f);
            phase1Random.AddBranch(_phase1DevourState, 1, MoveRepeatType.CannotRepeat, 0.33f);
            phase1Random.AddBranch(_phase1HungerState, 1, MoveRepeatType.CannotRepeat, 0.33f);

            // 饥饿后必须接重压（强制跳转，无视冷却）
            _phase1HungerState.FollowUpState = _phase1CrushState;
            // 重压和吞噬结束后回到随机分支
            _phase1CrushState.FollowUpState = phase1Random;
            _phase1DevourState.FollowUpState = phase1Random;

            var allStates = new List<MonsterState>
            {
                _phase1CrushState, _phase1DevourState, _phase1HungerState, phase1Random
            };
            // 初始状态：重压（第一回合固定）
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
            // 移除所有玩家消耗牌堆中的卡牌
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

        // 视觉更新（仅一阶段）
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

        // 死亡处理（无转阶段，直接基类）
        public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
        {
            await base.AfterDeath(choiceContext, creature, wasRemovalPrevented, deathAnimLength);
        }

        // 战斗开始：添加饕餮和高维生物
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
            await PowerCmd.Apply<HighDimensionalBeingPower>(choiceContext, Creature, 5, Creature, null);
        }
    }
}
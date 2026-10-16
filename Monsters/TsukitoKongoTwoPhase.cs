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
    public sealed class TsukitoKongoTwoPhase : CustomMonsterModel
    {
        // 视觉资源路径（如果图片不存在可注释掉 UpdateVisual 调用）
        private const string VisualPath = "images/packed/monster/tsukito_kongo_phase2.png";

        // 血量基础值（单人模式）
        private const int BaseHp = 500;

        public override int MinInitialHp => BaseHp;
        public override int MaxInitialHp => BaseHp;

        // 阶段2数值
        private int GongDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 32, 28);
        private int SoundStrikeDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 18, 17);
        private const int SoundStrikeHits = 2;
        private const int SoundStrikeStrength = 3;
        private const int ShockwaveDaze = 4;         // 晕眩数量（每堆）
        private const int ShockwaveBlock = 45;       // 自身格挡
        private const int ShockwaveWeak = 3;         // 虚弱层数
        private const int ShockwaveStatusCount = 8;  // 总共添加的状态牌数量（4+4）

        private MoveState _phase2GongState, _phase2SoundStrikeState, _phase2ShockwaveState;

        public override NCreatureVisuals CreateCustomVisuals()
            => NodeFactory<NCreatureVisuals>.CreateFromScene("res://scenes/monsters/tsukito_kongo.tscn");

        protected override MonsterMoveStateMachine GenerateMoveStateMachine()
        {
            // 敲锣：攻击 + 强化（获得锣鼓噪声）
            _phase2GongState = new MoveState("GONG", GongMove,
                new SingleAttackIntent(GongDamage), new BuffIntent());
            // 音击：攻击 + 强化（获得力量）
            _phase2SoundStrikeState = new MoveState("SOUND_STRIKE", SoundStrikeMove,
                new MultiAttackIntent(SoundStrikeDamage, SoundStrikeHits), new BuffIntent());
            // 音波：削弱 + 状态牌（8张） + 防御
            _phase2ShockwaveState = new MoveState("SHOCKWAVE", ShockwaveMove,
                new DebuffIntent(), new StatusIntent(ShockwaveStatusCount), new DefendIntent());

            // 固定顺序：敲锣 → 音击 → 音波 → 音击 → 音波 → ...
            _phase2GongState.FollowUpState = _phase2SoundStrikeState;
            _phase2SoundStrikeState.FollowUpState = _phase2ShockwaveState;
            _phase2ShockwaveState.FollowUpState = _phase2SoundStrikeState;

            var allStates = new List<MonsterState> { _phase2GongState, _phase2SoundStrikeState, _phase2ShockwaveState };
            // 初始状态：敲锣
            return new MonsterMoveStateMachine(allStates, _phase2GongState);
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
                // 修正：移除 addedByPlayer 参数，使用新版签名
                await CardPileCmd.AddToCombatAndPreview<Dazed>(t, PileType.Draw, ShockwaveDaze, null);
                await CardPileCmd.AddToCombatAndPreview<Dazed>(t, PileType.Discard, ShockwaveDaze, null);
                var ctx = new ThrowingPlayerChoiceContext();
                await PowerCmd.Apply<WeakPower>(ctx, t, ShockwaveWeak, Creature, null);
            }
            await CreatureCmd.GainBlock(Creature, ShockwaveBlock, ValueProp.Move, null);
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

        // 血量缩放（多人模式）
        public override async Task AfterAddedToRoom()
        {
            await base.AfterAddedToRoom();
            // 根据存活玩家数量缩放血量
            int playerCount = Creature.CombatState?.Players.Count(p => p.Creature.IsAlive) ?? 1;
            int scaledHp = BaseHp * playerCount;
            await CreatureCmd.SetMaxHp(Creature, scaledHp);
            await CreatureCmd.Heal(Creature, scaledHp);

            UpdateVisual(VisualPath);
            var ctx = new ThrowingPlayerChoiceContext();
            // 添加锣鼓噪声能力（一开场就有）
            await PowerCmd.Apply<GongNoisePower>(ctx, Creature, 1, Creature, null);
            // 添加其他预设能力
            await PowerCmd.Apply<UnbreakablePower>(ctx, Creature, 250, Creature, null);
            await PowerCmd.Apply<HigherDimensionalBeingPower>(ctx, Creature, 5, Creature, null);
        }
    }
}
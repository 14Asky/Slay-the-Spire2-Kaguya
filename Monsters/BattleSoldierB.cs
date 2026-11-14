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
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.Monsters
{
    public sealed class BattleSoldierB : CustomMonsterModel
    {
        public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 50, 42);
        public override int MaxInitialHp => MinInitialHp;

        // 鼓舞数值
        private const int EncourageStrength = 2;
        private const int EncourageHeal = 3;

        // 敲锣数值
        private int GongDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 5, 4);
        private const int GongDazeCount = 3;

        protected override string VisualsPath => "res://scenes/monsters/battle_soldier_b.tscn";

        public override NCreatureVisuals CreateCustomVisuals()
            => NodeFactory<NCreatureVisuals>.CreateFromScene(VisualsPath);

        // 战斗开始时添加 InspirationPower
        public override async Task AfterAddedToRoom()
        {
            await base.AfterAddedToRoom();
            var choiceContext = new ThrowingPlayerChoiceContext();
            await PowerCmd.Apply<InspirationPower>(choiceContext, Creature, 1, Creature, null);
        }

        protected override MonsterMoveStateMachine GenerateMoveStateMachine()
        {
            // 鼓舞状态
            var encourageState = new MoveState("ENCOURAGE", EncourageMove,
                new BuffIntent(), new HealIntent());

            // 敲锣状态
            var gongState = new MoveState("GONG", GongMove,
                new SingleAttackIntent(GongDamage), new StatusIntent(GongDazeCount));

            // 顺序循环：鼓舞 → 敲锣 → 鼓舞 → ...
            encourageState.FollowUpState = gongState;
            gongState.FollowUpState = encourageState;

            var states = new List<MonsterState> { encourageState, gongState };
            // 第一回合固定鼓舞
            return new MonsterMoveStateMachine(states, encourageState);
        }

        private async Task EncourageMove(IReadOnlyList<Creature> targets)
        {
            var choiceContext = new ThrowingPlayerChoiceContext();
            var allies = Creature.CombatState.GetTeammatesOf(Creature);
            foreach (var ally in allies)
            {
                await PowerCmd.Apply<StrengthPower>(choiceContext, ally, EncourageStrength, Creature, null);
                await CreatureCmd.Heal(ally, EncourageHeal);
            }
        }

        private async Task GongMove(IReadOnlyList<Creature> targets)
        {
            await DamageCmd.Attack(GongDamage)
                .FromMonster(this)
                .WithAttackerFx(null, AttackSfx)
                .WithHitFx("vfx/vfx_attack_blunt")
                .Execute(null);

            foreach (var player in Creature.CombatState.Players)
            {
                // 使用新版签名：AddToCombatAndPreview<T>(Creature target, PileType pileType, int count, Player? creator)
                await CardPileCmd.AddToCombatAndPreview<Dazed>(player.Creature, PileType.Discard, GongDazeCount, null);
            }
        }
    }
}

using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.Monsters
{
    public sealed class BattleSoldierA : CustomMonsterModel
    {
        public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 55, 48);
        public override int MaxInitialHp => MinInitialHp;

        // 猛攻数值
        private int AssaultDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 2);
        private const int AssaultHitCount = 2;
        private const int AssaultWeak = 1;

        // 护卫数值
        private int GuardDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 6, 5);
        private const int GuardBlock = 4;

        protected override string VisualsPath => "res://scenes/monsters/battle_soldier_a.tscn";

        public override NCreatureVisuals CreateCustomVisuals()
            => NodeFactory<NCreatureVisuals>.CreateFromScene(VisualsPath);

        protected override MonsterMoveStateMachine GenerateMoveStateMachine()
        {
            // 猛攻状态
            var assaultState = new MoveState("ASSAULT", AssaultMove,
                new MultiAttackIntent(AssaultDamage, AssaultHitCount), new DebuffIntent());

            // 护卫状态
            var guardState = new MoveState("GUARD", GuardMove,
                new SingleAttackIntent(GuardDamage), new DefendIntent());

            // 顺序循环：猛攻 → 护卫 → 猛攻 → ...
            assaultState.FollowUpState = guardState;
            guardState.FollowUpState = assaultState;

            var states = new List<MonsterState> { assaultState, guardState };
            // 第一回合固定猛攻
            return new MonsterMoveStateMachine(states, assaultState);
        }

        private async Task AssaultMove(IReadOnlyList<Creature> targets)
        {
            await DamageCmd.Attack(AssaultDamage)
                .WithHitCount(AssaultHitCount)
                .FromMonster(this)
                .WithAttackerFx(null, AttackSfx)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(null);
            var ctx = new ThrowingPlayerChoiceContext();
            foreach (var target in targets)
            {
                await PowerCmd.Apply<WeakPower>(ctx, target, AssaultWeak, Creature, null);
            }
        }

        private async Task GuardMove(IReadOnlyList<Creature> targets)
        {
            // 对所有玩家造成伤害
            await DamageCmd.Attack(GuardDamage)
                .FromMonster(this)
                .WithAttackerFx(null, AttackSfx)
                .WithHitFx("vfx/vfx_attack_blunt")
                .Execute(null);
            // 所有友方怪物获得15点格挡
            var allies = Creature.CombatState.GetTeammatesOf(Creature);
            foreach (var ally in allies)
            {
                await CreatureCmd.GainBlock(ally, GuardBlock, ValueProp.Move, null);
            }
        }
    }
}

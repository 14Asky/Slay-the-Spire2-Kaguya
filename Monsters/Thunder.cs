using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using Kaguya.Powers;
using Kaguya.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.Monsters
{
    public sealed class Thunder : CustomMonsterModel
    {
        public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 105, 90);
        public override int MaxInitialHp => MinInitialHp;

        private int BindingTrapDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 10, 8);
        private int MineTrapDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 8, 6);
        private int HeavyDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 22, 20);
        private const int StrengthGain = 4;

        public override NCreatureVisuals CreateCustomVisuals()
            => NodeFactory<NCreatureVisuals>.CreateFromScene("res://scenes/monsters/thunder.tscn");

        protected override MonsterMoveStateMachine GenerateMoveStateMachine()
        {
            var bindingTrapState = new MoveState("BINDING_TRAP", BindingTrapMove,
                new SingleAttackIntent(() => BindingTrapDamage), new DebuffIntent());

            var mineTrapState = new MoveState("MINE_TRAP", MineTrapMove,
                new SingleAttackIntent(() => MineTrapDamage), new DebuffIntent());

            var heavyState = new MoveState("HEAVY_ATTACK", HeavyAttackMove,
                new SingleAttackIntent(() => HeavyDamage), new BuffIntent());

            var randomTrap = new RandomBranchState("RANDOM_TRAP");
            randomTrap.AddBranch(bindingTrapState, MoveRepeatType.CanRepeatForever, 0.5f);
            randomTrap.AddBranch(mineTrapState, MoveRepeatType.CanRepeatForever, 0.5f);

            bindingTrapState.FollowUpState = heavyState;
            mineTrapState.FollowUpState = heavyState;
            heavyState.FollowUpState = randomTrap;

            var states = new List<MonsterState> { bindingTrapState, mineTrapState, heavyState, randomTrap };
            return new MonsterMoveStateMachine(states, randomTrap);
        }

        // 束缚陷阱：造成伤害，并对所有玩家施加 ThornTrapPower
        private async Task BindingTrapMove(IReadOnlyList<Creature> targets)
        {
            // 造成伤害（对所有玩家）
            await DamageCmd.Attack(BindingTrapDamage)
                .FromMonster(this)
                .WithAttackerFx(null, AttackSfx)
                .Execute(null);

            var choiceContext = new ThrowingPlayerChoiceContext();
            // 对每个玩家施加荆棘环能力
            foreach (var target in targets)
            {
                await PowerCmd.Apply<ThornTrapPower>(choiceContext, target, 1, Creature, null);
            }
        }

        // 地雷陷阱：造成伤害，并对所有玩家施加 BacklashTrapPower
        private async Task MineTrapMove(IReadOnlyList<Creature> targets)
        {
            await DamageCmd.Attack(MineTrapDamage)
                .FromMonster(this)
                .Execute(null);

            var choiceContext = new ThrowingPlayerChoiceContext();
            foreach (var target in targets)
            {
                await PowerCmd.Apply<BacklashTrapPower>(choiceContext, target, 1, Creature, null);
            }
        }

        private async Task HeavyAttackMove(IReadOnlyList<Creature> targets)
        {
            var target = targets.FirstOrDefault();
            if (target == null) return;

            await DamageCmd.Attack(HeavyDamage)
                .FromMonster(this)
                .Execute(null);

            var choiceContext = new ThrowingPlayerChoiceContext();
            await PowerCmd.Apply<StrengthPower>(choiceContext, Creature, StrengthGain, Creature, null);
        }

        // 死亡掉落：每个存活玩家都获得一个随机遗物（从 LandmineTrap 或 LeiBigSword 中选）
        public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
        {
            await base.AfterDeath(choiceContext, creature, wasRemovalPrevented, deathAnimLength);
            if (creature != Creature) return;

            // 获取所有存活的玩家
            var players = Creature.CombatState?.Players.Where(p => p.Creature.IsAlive).ToList();
            if (players == null || players.Count == 0) return;

            var relicTypes = new[] { typeof(LandmineTrap), typeof(LeiBigSword) };

            foreach (var player in players)
            {
                var randomType = player.RunState.Rng.Niche.NextItem(relicTypes);
                RelicModel relic;
                if (randomType == typeof(LandmineTrap))
                    relic = ModelDb.Relic<LandmineTrap>().ToMutable();
                else
                    relic = ModelDb.Relic<LeiBigSword>().ToMutable();

                await RelicCmd.Obtain(relic, player);
            }
        }
    }
}
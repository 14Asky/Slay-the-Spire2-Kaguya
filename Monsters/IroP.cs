using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using Godot;
using Kaguya.Powers;
using Kaguya.RelicPools;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.Monsters
{
    public sealed class IroP : CustomMonsterModel
    {
        public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 105, 90);
        public override int MaxInitialHp => MinInitialHp;

        private const int RisingDragonDamage = 1;
        private int RisingDragonHitCount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 9, 8);

        private int ParryDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 12, 10);
        private int ParryBlockCount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 7, 6);
        private const int ParryBlockPerHit = 1;

        protected override string VisualsPath => "res://scenes/monsters/iro_p.tscn";

        public override NCreatureVisuals CreateCustomVisuals()
            => NodeFactory<NCreatureVisuals>.CreateFromScene(VisualsPath);

        protected override MonsterMoveStateMachine GenerateMoveStateMachine()
        {
            var buffState = new MoveState("BUFF", BuffMove, new BuffIntent());
            var risingDragonState = new MoveState("RISING_DRAGON", RisingDragonMove, new MultiAttackIntent(RisingDragonDamage, () => RisingDragonHitCount));
            var parryState = new MoveState("PARRY", ParryMove, new SingleAttackIntent(ParryDamage), new DefendIntent());

            var decisionState = new ConditionalBranchState("DECISION");
            decisionState.AddState(parryState, () => Creature.CurrentHp < Creature.MaxHp * 0.5m);
            decisionState.AddState(risingDragonState, () => true);

            buffState.FollowUpState = decisionState;
            risingDragonState.FollowUpState = decisionState;
            parryState.FollowUpState = risingDragonState;

            var states = new List<MonsterState> { buffState, risingDragonState, parryState, decisionState };
            return new MonsterMoveStateMachine(states, buffState);
        }

        private async Task BuffMove(IReadOnlyList<Creature> targets)
        {
            var choiceContext = new ThrowingPlayerChoiceContext();
            await PowerCmd.Apply<StrengthPower>(choiceContext, Creature, 1, Creature, null);
        }

        private async Task RisingDragonMove(IReadOnlyList<Creature> targets)
        {
            var target = targets.FirstOrDefault();
            if (target == null) return;
            await DamageCmd.Attack(RisingDragonDamage)
                .WithHitCount(RisingDragonHitCount)
                .FromMonster(this)
                .WithAttackerFx(null, AttackSfx)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(null);
            var choiceContext = new ThrowingPlayerChoiceContext();
            await PowerCmd.Apply<DexterityPower>(choiceContext, Creature, 1, Creature, null);
        }

        private async Task ParryMove(IReadOnlyList<Creature> targets)
        {
            var target = targets.FirstOrDefault();
            if (target == null) return;
            await DamageCmd.Attack(ParryDamage)
                .FromMonster(this)
                .WithAttackerFx(null, AttackSfx)
                .WithHitFx("vfx/vfx_attack_blunt")
                .Execute(null);
            for (int i = 0; i < ParryBlockCount; i++)
            {
                await CreatureCmd.GainBlock(Creature, ParryBlockPerHit, ValueProp.Move, null);
            }
            var choiceContext = new ThrowingPlayerChoiceContext();
            await PowerCmd.Apply<StrengthPower>(choiceContext, Creature, 1, Creature, null);
        }

        // 死亡时从KaguyaRelicPool中给予稀有遗物
        public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, CardModel? source, bool wasRemovalPrevented, float deathAnimLength)
        {
            await base.AfterDeath(choiceContext, creature, source, wasRemovalPrevented, deathAnimLength);
            if (creature != Creature) return;

            var players = Creature.CombatState?.Players.Where(p => p.Creature.IsAlive).ToList();
            if (players == null || players.Count == 0) return;

            var kaguyaPool = ModelDb.RelicPool<KaguyaRelicPool>();
            var allRelics = ModelDb.AllRelics
                .Where(r => r.Rarity == RelicRarity.Rare && kaguyaPool.AllRelicIds.Contains(r.Id) && r.IsAllowed(players.First().RunState))
                .ToList();
            if (allRelics.Count == 0) return;

            foreach (var player in players)
            {
                var randomRelic = player.RunState.Rng.Niche.NextItem(allRelics);
                var relic = randomRelic.ToMutable();
                await RelicCmd.Obtain(relic, player);
            }
        }
    }
}

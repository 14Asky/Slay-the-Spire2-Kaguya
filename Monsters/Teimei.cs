using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
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
    public sealed class Teimei : CustomMonsterModel
    {
        public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 105, 90);
        public override int MaxInitialHp => MinInitialHp;

        private int HeavyDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 18, 16);
        private int HeavyBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 18, 16);
        private const int ComboDamage = 4;
        private const int ComboHitCount = 3;
        private const int StrengthGain = 5;

        protected override string VisualsPath => "res://scenes/monsters/teimei.tscn";

        public override NCreatureVisuals CreateCustomVisuals()
            => NodeFactory<NCreatureVisuals>.CreateFromScene(VisualsPath);

        protected override MonsterMoveStateMachine GenerateMoveStateMachine()
        {
            var buffState = new MoveState("BUFF", BuffMove, new BuffIntent());
            var heavyState = new MoveState("HEAVY", HeavyMove, new SingleAttackIntent(HeavyDamage), new DefendIntent());
            var comboState = new MoveState("COMBO", ComboMove, new MultiAttackIntent(ComboDamage, ComboHitCount));

            buffState.FollowUpState = heavyState;
            heavyState.FollowUpState = comboState;
            comboState.FollowUpState = buffState;

            var conditionalStart = new ConditionalBranchState("START");
            conditionalStart.AddState(heavyState, () => Creature.CurrentHp < Creature.MaxHp * 0.5m);
            conditionalStart.AddState(buffState, () => true);

            var states = new List<MonsterState> { buffState, heavyState, comboState, conditionalStart };
            return new MonsterMoveStateMachine(states, conditionalStart);
        }

        private async Task BuffMove(IReadOnlyList<Creature> targets)
        {
            var choiceContext = new ThrowingPlayerChoiceContext();
            await PowerCmd.Apply<StrengthPower>(choiceContext, Creature, StrengthGain, Creature, null);
        }

        private async Task HeavyMove(IReadOnlyList<Creature> targets)
        {
            var target = targets.FirstOrDefault();
            if (target == null) return;
            await DamageCmd.Attack(HeavyDamage)
                .FromMonster(this)
                .WithAttackerFx(null, AttackSfx)
                .WithHitFx("vfx/vfx_attack_blunt")
                .Execute(null);
            await CreatureCmd.GainBlock(Creature, HeavyBlock, ValueProp.Move, null);
        }

        private async Task ComboMove(IReadOnlyList<Creature> targets)
        {
            var target = targets.FirstOrDefault();
            if (target == null) return;
            await DamageCmd.Attack(ComboDamage)
                .WithHitCount(ComboHitCount)
                .FromMonster(this)
                .WithAttackerFx(null, AttackSfx)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(null);
        }

        // 死亡掉落：每个存活玩家随机获得 BrothersDeposit 或 DataUsb
        public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, CardModel? source, bool wasRemovalPrevented, float deathAnimLength)
        {
            await base.AfterDeath(choiceContext, creature, source, wasRemovalPrevented, deathAnimLength);
            if (creature != Creature) return;

            var players = Creature.CombatState?.Players.Where(p => p.Creature.IsAlive).ToList();
            if (players == null || players.Count == 0) return;

            var relicTypes = new[] { typeof(BrothersDeposit), typeof(DataUsb) };

            foreach (var player in players)
            {
                var randomType = player.RunState.Rng.Niche.NextItem(relicTypes);
                RelicModel relic;
                if (randomType == typeof(BrothersDeposit))
                    relic = ModelDb.Relic<BrothersDeposit>().ToMutable();
                else
                    relic = ModelDb.Relic<DataUsb>().ToMutable();

                await RelicCmd.Obtain(relic, player);
            }
        }
    }
}

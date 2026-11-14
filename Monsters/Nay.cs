using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using Kaguya.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
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
    public sealed class Nay : CustomMonsterModel
    {
        public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 100, 90);
        public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 105, 95);

        private int RapidShotDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 2, 1);
        private const int RapidShotHitCount = 7;
        private const int DazedCount = 4;

        protected override string VisualsPath => "res://scenes/monsters/nay.tscn";

        public override NCreatureVisuals CreateCustomVisuals()
            => NodeFactory<NCreatureVisuals>.CreateFromScene(VisualsPath);

        protected override MonsterMoveStateMachine GenerateMoveStateMachine()
        {
            var lightningArrowState = new MoveState("LIGHTNING_ARROW", LightningArrowMove,
                new DebuffIntent(), new BuffIntent());

            var rapidShotState1 = new MoveState("RAPID_SHOT", RapidShotMove,
                new MultiAttackIntent(RapidShotDamage, RapidShotHitCount));

            var rapidShotState2 = new MoveState("RAPID_SHOT_2", RapidShotMove,
                new MultiAttackIntent(RapidShotDamage, RapidShotHitCount));

            lightningArrowState.FollowUpState = rapidShotState1;
            rapidShotState1.FollowUpState = rapidShotState2;
            rapidShotState2.FollowUpState = lightningArrowState;

            var states = new List<MonsterState> { lightningArrowState, rapidShotState1, rapidShotState2 };
            return new MonsterMoveStateMachine(states, lightningArrowState);
        }

        private async Task LightningArrowMove(IReadOnlyList<Creature> targets)
        {
            await CardPileCmd.AddToCombatAndPreview<Dazed>(targets, PileType.Draw, DazedCount, null);
            await CardPileCmd.AddToCombatAndPreview<Dazed>(targets, PileType.Discard, DazedCount, null);
            var choiceContext = new ThrowingPlayerChoiceContext();
            await PowerCmd.Apply<StrengthPower>(choiceContext, Creature, 1, Creature, null);
        }

        private async Task RapidShotMove(IReadOnlyList<Creature> targets)
        {
            await DamageCmd.Attack(RapidShotDamage)
                .WithHitCount(RapidShotHitCount)
                .FromMonster(this)
                .WithAttackerFx(null, AttackSfx)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(null);
        }

        // 死亡掉落：每个存活玩家获得 NaiBow
        public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, CardModel? source, bool wasRemovalPrevented, float deathAnimLength)
        {
            await base.AfterDeath(choiceContext, creature, source, wasRemovalPrevented, deathAnimLength);
            if (creature != Creature) return;

            var players = Creature.CombatState?.Players.Where(p => p.Creature.IsAlive).ToList();
            if (players == null || players.Count == 0) return;

            foreach (var player in players)
            {
                var relic = ModelDb.Relic<NaiBow>().ToMutable();
                await RelicCmd.Obtain(relic, player);
            }
        }
    }
}

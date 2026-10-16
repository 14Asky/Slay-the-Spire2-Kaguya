using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using Kaguya.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.Monsters
{
    public sealed class Tsukibito : CustomMonsterModel
    {
        public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 55, 50);
        public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 65, 60);

        private const int ComboDamage = 6;
        private const int ComboHitCount = 2;
        private const int HealAmount = 12;
        private const int StrengthGain = 2;

        private int _starterMoveIdx;

        public int StarterMoveIdx
        {
            get => _starterMoveIdx;
            set
            {
                AssertMutable();
                _starterMoveIdx = value;
            }
        }

        public override NCreatureVisuals CreateCustomVisuals()
            => NodeFactory<NCreatureVisuals>.CreateFromScene("res://scenes/monsters/tsukibito.tscn");

        protected override MonsterMoveStateMachine GenerateMoveStateMachine()
        {
            var comboState = new MoveState("COMBO", ComboMove, new MultiAttackIntent(ComboDamage, ComboHitCount));
            var regenState = new MoveState("REGEN", RegenMove, new HealIntent(), new BuffIntent());

            comboState.FollowUpState = regenState;
            regenState.FollowUpState = comboState;

            // 根据 StarterMoveIdx 决定初始状态：0=连击，1=再生，2=连击，3=再生...
            MonsterState initialState = (StarterMoveIdx % 2 == 0) ? comboState : regenState;

            return new MonsterMoveStateMachine(new[] { comboState, regenState }, initialState);
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

        private async Task RegenMove(IReadOnlyList<Creature> targets)
        {
            await CreatureCmd.Heal(Creature, HealAmount);
            var choiceContext = new ThrowingPlayerChoiceContext();
            await PowerCmd.Apply<StrengthPower>(choiceContext, Creature, StrengthGain, Creature, null);
        }

        // ---------- 战斗开始 ----------
        public override async Task AfterAddedToRoom()
        {
            await base.AfterAddedToRoom();
            var choiceContext = new ThrowingPlayerChoiceContext();
            await PowerCmd.Apply<HighDimensionalBeingPower>(choiceContext, Creature, 1, Creature, null);
        }
    }
}
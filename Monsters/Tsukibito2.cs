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
    public sealed class Tsukibito2 : CustomMonsterModel
    {
        // 血量与1号相同
        public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 50, 45);
        public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 60, 55);

        private const int ComboDamage = 6;
        private const int ComboHitCount = 2;
        private const int HealAmount = 12;
        private const int StrengthGain = 2;

        public override NCreatureVisuals CreateCustomVisuals()
            => NodeFactory<NCreatureVisuals>.CreateFromScene("res://scenes/monsters/tsukibito.tscn");

        protected override MonsterMoveStateMachine GenerateMoveStateMachine()
        {
            var comboState = new MoveState("COMBO", ComboMove, new MultiAttackIntent(ComboDamage, ComboHitCount));
            var regenState = new MoveState("REGEN", RegenMove, new HealIntent(), new BuffIntent());

            // 顺序与1号相反：再生 → 连击 → 再生 ...
            regenState.FollowUpState = comboState;
            comboState.FollowUpState = regenState;

            return new MonsterMoveStateMachine(new[] { regenState, comboState }, regenState); // 初始状态为再生
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
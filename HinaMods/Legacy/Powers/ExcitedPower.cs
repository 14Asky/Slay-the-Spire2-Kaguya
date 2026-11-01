using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Kaguya.Powers
{
    public sealed class LegacyExcitedPower : CustomPowerModel
    {
        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Single;

        public override string CustomPackedIconPath => "res://images/powers/ExcitedPower.png";
        public override string CustomBigIconPath => "res://images/powers/ExcitedPower.png";

        private const int StrengthGain = 2;
        private const int DexterityGain = 2;

        // 修正：参数类型改为 ICombatState
        public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
        {
            if (side != Owner.Side) return;
            // 使用占位上下文
            var choiceContext = new ThrowingPlayerChoiceContext();
            await PowerCmd.Apply<StrengthPower>(choiceContext, Owner, StrengthGain, Owner, null);
            await PowerCmd.Apply<DexterityPower>(choiceContext, Owner, DexterityGain, Owner, null);
            Flash();
        }
    }
}

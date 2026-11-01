using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Kaguya.Powers
{
    public sealed class LegacyVajraAllPower : CustomPowerModel
    {
        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Single;

        public override string CustomPackedIconPath => "res://images/powers/VajraPower.png";
        public override string CustomBigIconPath => "res://images/powers/VajraPower.png";

        private const int PlatingAmount = 25;

        public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
        {
            if (side != Owner.Side) return;

            // 获取所有友方怪物（包括自身）
            var allies = Owner.CombatState.GetTeammatesOf(Owner);
            foreach (var ally in allies)
            {
                await PowerCmd.Apply<PlatingPower>(choiceContext, ally, PlatingAmount, Owner, null);
            }
        }
    }
}

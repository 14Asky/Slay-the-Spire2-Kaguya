using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.Powers
{
    public sealed class AccountRemedyPower : CustomPowerModel
    {
        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Counter;
        public override PowerInstanceType InstanceType => PowerInstanceType.None;

        public override string CustomPackedIconPath => "res://images/powers/kaguya_no_energy.png";
        public override string CustomBigIconPath => "res://images/powers/kaguya_no_energy.png";

        // 层数 > 0 时阻止死亡
        public override bool ShouldDieLate(Creature creature)
        {
            if (creature != Owner) return true;
            return Amount <= 0;
        }

        // 阻止死亡后：消耗所有手牌，回复至 30% 最大生命值
        public override async Task AfterPreventingDeath(Creature creature)
        {
            if (creature != Owner) return;
            if (Amount <= 0) return;

            // 消耗一层
            await PowerCmd.Decrement(this);

            var choiceContext = new ThrowingPlayerChoiceContext();

            // 回复至 30% 最大生命值
            decimal healAmount = creature.MaxHp * 0.3m;
            await CreatureCmd.Heal(creature, healAmount, true);

            Flash();
        }
    }
}

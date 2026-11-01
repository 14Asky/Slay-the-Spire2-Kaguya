using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using System.Threading.Tasks;

namespace Kaguya.Powers
{
    public sealed class LegacyFairDuelPower : CustomPowerModel
    {
        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Counter;  // 层数 = 剩余复活次数

        // 图标路径（请根据实际资源放置）
        public override string CustomPackedIconPath => "res://images/packed/card_portraits/kaguya/BattleStartPower.png";
        public override string CustomBigIconPath => "res://images/packed/card_portraits/kaguya/BattleStartPower.png";

        // 显示剩余次数
        public override int DisplayAmount => (int)Amount;

        // 控制是否允许死亡：层数 > 0 时阻止死亡，否则正常死亡
        public override bool ShouldDieLate(Creature creature)
        {
            if (creature != Owner) return true;
            return Amount <= 0;
        }

        // 阻止死亡后，消耗一层，并回复至最大生命值
        public override async Task AfterPreventingDeath(Creature creature)
        {
            if (creature != Owner) return;
            if (Amount <= 0) return;

            // 减少一层
            await PowerCmd.Decrement(this);

            // 回复至满血
            await CreatureCmd.Heal(creature, creature.MaxHp - creature.CurrentHp, true);

            // 播放闪烁特效
            Flash();
        }
    }
}
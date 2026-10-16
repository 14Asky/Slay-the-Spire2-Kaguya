using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using System.Threading.Tasks;

namespace Kaguya.Powers
{
    public sealed class HighDimensionalBeingPower : CustomPowerModel
    {
        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Counter;

        // 多人模式缩放
        public override bool ShouldScaleInMultiplayer => true;

        // 图标路径（请根据实际资源位置调整）
        public override string CustomPackedIconPath => "res://images/powers/gaoweishengwu.png";
        public override string CustomBigIconPath => "res://images/powers/gaoweishengwu.png";

        // 关键：确保能力不会因怪物死亡/复活而被移除
        public override bool ShouldPowerBeRemovedAfterOwnerDeath() => false;

        // 抵消 Debuff 层数（与 ArtifactPower 逻辑一致）
        public override bool TryModifyPowerAmountReceived(PowerModel canonicalPower, Creature target, decimal amount, Creature _, out decimal modifiedAmount)
        {
            if (target != Owner)
            {
                modifiedAmount = amount;
                return false;
            }
            if (canonicalPower.GetTypeForAmount(amount) != PowerType.Debuff)
            {
                modifiedAmount = amount;
                return false;
            }
            if (!canonicalPower.IsVisible)
            {
                modifiedAmount = amount;
                return false;
            }
            modifiedAmount = default;
            return true;
        }

        public override async Task AfterModifyingPowerAmountReceived(PowerModel power)
        {
            await PowerCmd.Decrement(this);
        }
    }
}
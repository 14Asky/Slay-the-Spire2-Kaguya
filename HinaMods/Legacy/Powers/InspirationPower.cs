using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using System.Threading.Tasks;

namespace Kaguya.Powers
{
    /// <summary>
    /// 鼓舞：目前没有任何效果，仅作为增益状态占位。
    /// </summary>
    public sealed class LegacyInspirationPower : CustomPowerModel
    {
        public override PowerType Type => PowerType.Buff;
        // 可叠加，层数会显示在图标上（暂时无实际用途）
        public override PowerStackType StackType => PowerStackType.Counter;

        // 图标路径，请替换为你的实际资源路径
        public override string CustomPackedIconPath => "res://images/powers/InspirationPower.png";
        public override string CustomBigIconPath => "res://images/powers/InspirationPower.png";

        // 没有任何重写方法，因此不会触发任何效果
    }
}
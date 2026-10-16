using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using System.Threading.Tasks;

namespace Kaguya.Powers
{
    public sealed class TurnCounterPower : CustomPowerModel
    {
        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Counter; // 显示数字

        public override string CustomPackedIconPath => "res://images/ui/run_history/tsukito_tenchu_boss_encounter.png";
        public override string CustomBigIconPath => "res://images/ui/run_history/tsukito_tenchu_boss_encounter.png";

        // 不需要任何实际效果，仅用于展示层数
    }
}
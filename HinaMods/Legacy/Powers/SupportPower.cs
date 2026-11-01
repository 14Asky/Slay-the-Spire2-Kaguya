using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
// 🔥 引用 BaseLib 的 CustomPowerModel
using BaseLib.Abstracts;
using System.Threading.Tasks;

// 匹配你的文件路径
namespace Kaguya.HinaMods.Legacy.Powers;

// 🔥 继承 BaseLib 的 CustomPowerModel（自动注册+自定义图标）
public sealed class LegacySupportPower : CustomPowerModel
{
    // 1. 基础配置
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override bool AllowNegative => false;
}

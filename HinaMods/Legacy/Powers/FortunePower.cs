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
public sealed class LegacyFortunePower : CustomPowerModel
{
	// 1. 基础配置
	public override PowerType Type => PowerType.Buff;
	public override PowerStackType StackType => PowerStackType.Counter;
	public override bool AllowNegative => false;

	// 2. 🔥 直接写你的图标路径！想放哪就放哪（彻底解决图标问题）
	public override string CustomPackedIconPath => "res://images/hinamods/Powers/fortune_power.png";
	public override string CustomBigIconPath => "res://images/hinamods/Powers/fortune_power.png";

	// 3. 回合逻辑
	public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, MegaCrit.Sts2.Core.Entities.Players.Player player)
	{
		return Task.CompletedTask;
	}
}

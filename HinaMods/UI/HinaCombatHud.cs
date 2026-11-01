#nullable enable
using BaseLib.Patches.UI;
using Godot;
using Kaguya.HinaMods.Character;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Kaguya.HinaMods.UI;

/// <summary>
/// 把月夜 HUD 挂到战斗界面上。
///
/// 多人隔离的依据：BaseLib 的 <see cref="ExtraCombatUi"/> 回调是通过
/// <c>LocalContext.GetMe(combatState)</c> 拿到【本地玩家】后才触发的，
/// 也就是说每台客户端只会为自己这一名玩家调用一次回调 —— 每个玩家只会看到自己的 HUD。
/// 这里再叠一层保险：只有在本地玩家、且角色是本 mod 角色时才创建。
/// 基础设施类，无本地化键。
/// </summary>
public static class HinaCombatHud
{
	private static bool _registered;

	/// <summary>在模组初始化时调用一次即可。</summary>
	public static void Register()
	{
		if (_registered)
		{
			return;
		}

		_registered = true;

		ExtraCombatUi.RegisterCombatUiElement(CreateMoonlightHud, ExtraCombatUi.CombatUiPositioning.Left);
	}

	private static Control? CreateMoonlightHud(NCombatUi ui, Player player, CombatState combatState)
	{
		if (!IsLocalKaguya(player))
		{
			return null;
		}

		HinaMoonlightHud hud = HinaMoonlightHud.Create(player);
		ui.AddChild(hud);
		return hud;
	}

	private static bool IsLocalKaguya(Player player)
	{
		if (player.Character is not HinaCharacter)
		{
			return false;
		}

		// NetId 未建立时退化为放行（单机流程），避免误隐藏
		return !LocalContext.NetId.HasValue || LocalContext.IsMe(player);
	}
}

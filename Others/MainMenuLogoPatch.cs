using Godot;
using HarmonyLib;
using Kaguya.Utils;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using System.Threading.Tasks;

namespace Kaguya;

[HarmonyPatch(typeof(NMainMenuBg))]
public static class MainMenuLogoPatch
{
	private const string CustomBgScenePath = "res://scenes/ui/menu_bg.tscn";

	[HarmonyPatch(nameof(NMainMenuBg._Ready))]
	[HarmonyPostfix]
	static void ReadyPostfix(NMainMenuBg __instance)
	{
		if (!KaguyaModConfig.AllowMainMenuSceneReplacement)
			return;

		// 隐藏原版背景
		var bgContainer = __instance.GetNodeOrNull<Control>("BgContainer");
		if (bgContainer != null)
			bgContainer.Visible = false;

		// 替换为自定义场景
		var bgScene = GD.Load<PackedScene>(CustomBgScenePath);
		if (bgScene != null)
		{
			var customBg = bgScene.Instantiate<Control>();
			customBg.Name = "KaguyaBg";
			customBg.MouseFilter = Control.MouseFilterEnum.Ignore;
			customBg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
			__instance.AddChild(customBg);
			__instance.MoveChild(customBg, 0);
		}

		// 隐藏原版 Logo（不使用自定义 Logo）
		var logo = __instance.GetNode("%Logo");
		if (logo is CanvasItem canvas)
			canvas.Visible = false;

		// Fire-and-forget update check
		_ = CheckForUpdateAndShowPopup();
	}

	[HarmonyPatch(nameof(NMainMenuBg.HideLogo))]
	[HarmonyPostfix]
	static void HideLogoPostfix() { }

	[HarmonyPatch(nameof(NMainMenuBg.ShowLogo))]
	[HarmonyPostfix]
	static void ShowLogoPostfix() { }

	private static async Task CheckForUpdateAndShowPopup()
	{
		try
		{
			var result = await UpdateChecker.CheckForUpdateAsync();
			if (result.HasUpdate)
			{
				var popup = UpdatePopup.Create(
					result.CurrentVersion,
					result.LatestVersion,
					result.ReleaseUrl);
				if (popup != null)
				{
					NModalContainer.Instance?.ShowBackstop();
					NModalContainer.Instance?.AddChild(popup);
				}
			}
		}
		catch { }
	}
}

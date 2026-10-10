using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;

namespace Kaguya;

/// <summary>
/// 检测主菜单相关设置是否被修改，若修改则提示需要重启游戏生效
/// </summary>
[HarmonyPatch(typeof(NSettingsScreen))]
public static class MainMenuRestartPopup
{
    private static bool _snapshotScene;
    private static bool _snapshotBgm;
    private static bool _hasSnapshot;

    [HarmonyPatch("OnSubmenuOpened")]
    [HarmonyPostfix]
    static void OnSubmenuOpenedPostfix()
    {
        _snapshotScene = KaguyaModConfig.AllowMainMenuSceneReplacement;
        _snapshotBgm = KaguyaModConfig.AllowMainMenuBgmReplacement;
        _hasSnapshot = true;
    }

    [HarmonyPatch("OnSubmenuClosed")]
    [HarmonyPostfix]
    static void OnSubmenuClosedPostfix()
    {
        if (!_hasSnapshot)
            return;

        bool sceneChanged = _snapshotScene != KaguyaModConfig.AllowMainMenuSceneReplacement;
        bool bgmChanged = _snapshotBgm != KaguyaModConfig.AllowMainMenuBgmReplacement;

        if (!sceneChanged && !bgmChanged)
            return;

        ShowRestartPopup();
    }

    private static void ShowRestartPopup()
    {
        var modal = NModalContainer.Instance;
        if (modal == null)
            return;

        modal.ShowBackstop();

        var popup = new ConfirmationDialog
        {
            Title = "提示",
            DialogText = "修改该设置必须重启游戏才能生效",
            OkButtonText = "确定",
            CancelButtonText = "",
            Exclusive = true,
            Unresizable = true
        };

        popup.Confirmed += () => modal.HideBackstop();
        popup.CloseRequested += () => modal.HideBackstop();

        modal.AddChild(popup);
        popup.PopupCentered();
    }
}

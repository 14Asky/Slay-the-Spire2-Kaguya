namespace Kaguya;

public sealed class KaguyaModConfig : BaseLib.Config.SimpleModConfig
{
    public static bool NonKaguyaCharactersSeeKaguyaContent { get; set; } = false;
    public static bool PlayProcessAnimations { get; set; } = true;
    public static bool AllowAncientCoexistence { get; set; } = false;
    public static bool AllowMemoryFragmentForAllCharacters { get; set; } = false;
    public static bool AllowMainMenuSceneReplacement { get; set; } = true;
    public static bool AllowMainMenuBgmReplacement { get; set; } = true;
}

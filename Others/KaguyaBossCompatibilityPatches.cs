using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Kaguya.Encounters;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Audio;

namespace Kaguya;

internal static class KaguyaBossCompatibilityPaths
{
    internal const string GloryMapTop = "res://images/packed/map/map_bgs/glory/map_top_glory.png";
    internal const string GloryMapMiddle = "res://images/packed/map/map_bgs/glory/map_middle_glory.png";
    internal const string GloryMapBottom = "res://images/packed/map/map_bgs/glory/map_bottom_glory.png";

    private static readonly IReadOnlyDictionary<Type, (string Icon, string Outline)> BossIcons =
        new Dictionary<Type, (string, string)>
        {
            [typeof(TsukitoBosatsuBossEncounter)] =
                ("res://images/packed/map/tsukito_bosatsu_icon.png", "res://images/packed/map/tsukito_bosatsu_icon_outline.png"),
            [typeof(TsukitoKongoBossEncounter)] =
                ("res://images/packed/map/tsukito_kongo_icon.png", "res://images/packed/map/tsukito_kongo_icon_outline.png"),
            [typeof(TsukitoTenchuBossEncounter)] =
                ("res://images/packed/map/tsukito_tenchu_icon.png", "res://images/packed/map/tsukito_tenchu_icon_outline.png"),
        };

    internal static bool IsKaguyaBoss(EncounterModel encounter) => BossIcons.ContainsKey(encounter.GetType());

    internal static bool IsMissingOutline(EncounterModel encounter, string path) =>
        BossIcons.TryGetValue(encounter.GetType(), out var icons)
        && string.Equals(path, icons.Outline, StringComparison.OrdinalIgnoreCase);

    internal static bool TryGetIconFallback(string path, out string replacement)
    {
        foreach (var icons in BossIcons.Values)
        {
            if (string.Equals(path, icons.Outline, StringComparison.OrdinalIgnoreCase))
            {
                replacement = icons.Icon;
                return true;
            }
        }

        replacement = path;
        return false;
    }
}

[HarmonyPatch(typeof(ActModel), nameof(ActModel.MapTopBgPath), MethodType.Getter)]
internal static class KaguyaFinalActMapTopPathPatch
{
    [HarmonyPostfix]
    private static void Postfix(ActModel __instance, ref string __result)
    {
        if (__instance is FinalAct)
            __result = KaguyaBossCompatibilityPaths.GloryMapTop;
    }
}

[HarmonyPatch(typeof(ActModel), nameof(ActModel.MapMidBgPath), MethodType.Getter)]
internal static class KaguyaFinalActMapMiddlePathPatch
{
    [HarmonyPostfix]
    private static void Postfix(ActModel __instance, ref string __result)
    {
        if (__instance is FinalAct)
            __result = KaguyaBossCompatibilityPaths.GloryMapMiddle;
    }
}

[HarmonyPatch(typeof(ActModel), nameof(ActModel.MapBotBgPath), MethodType.Getter)]
internal static class KaguyaFinalActMapBottomPathPatch
{
    [HarmonyPostfix]
    private static void Postfix(ActModel __instance, ref string __result)
    {
        if (__instance is FinalAct)
            __result = KaguyaBossCompatibilityPaths.GloryMapBottom;
    }
}

[HarmonyPatch(typeof(EncounterModel), nameof(EncounterModel.MapNodeAssetPaths), MethodType.Getter)]
internal static class KaguyaBossAssetListPatch
{
    [HarmonyPostfix]
    private static void Postfix(EncounterModel __instance, ref IEnumerable<string> __result)
    {
        if (KaguyaBossCompatibilityPaths.IsKaguyaBoss(__instance))
            __result = __result.Where(path => !KaguyaBossCompatibilityPaths.IsMissingOutline(__instance, path)).ToArray();
    }
}

[HarmonyPatch]
internal static class KaguyaBossOutlineLookupPatch
{
    private static MethodBase TargetMethod() => typeof(AssetCache)
        .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
        .Single(method => method.Name == "GetAsset" && !method.IsGenericMethod);

    [HarmonyPrefix]
    private static void Prefix(ref string path)
    {
        if (KaguyaBossCompatibilityPaths.TryGetIconFallback(path, out string replacement))
            path = replacement;
    }
}

[HarmonyPatch(typeof(NRunMusicController), nameof(NRunMusicController.PlayCustomMusic), new[] { typeof(string) })]
internal static class KaguyaEmptyCustomMusicPatch
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    private static bool Prefix(string customMusic) => !string.IsNullOrEmpty(customMusic);
}

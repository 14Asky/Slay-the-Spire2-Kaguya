using System;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace Kaguya;

/// <summary>
/// 卡图保护（最简版）。
///
/// 问题：第三方 mod 会全局替换卡牌立绘，把本 mod 的卡也换成别人的图。
///
/// 关键点：真正决定卡面显示的是 <c>CardModel.Portrait</c>（Texture2D），
/// 它**不是 virtual**，所有卡共用；而本 mod 的卡 override 的 <c>PortraitPath</c> 是 virtual，
/// patch 基类对 override 的子类不生效，所以必须挂在 <c>Portrait</c> 上。
///
/// 本 mod 的卡图本来就放在带前缀的独立目录里，路径天然唯一：
///   彩叶 → res://images/packed/card_portraits/kaguya/{类名}.png
///   辉夜 → res://images/hinamods/Cards/Big/{entry}.png（由 CustomPortraitPath 给出）
/// 所以这里只需要把被改过的值取回来，不做任何路径改写。
/// </summary>
[HarmonyPatch(typeof(CardModel), nameof(CardModel.Portrait), MethodType.Getter)]
public static class CardPortraitProtection
{
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    private static void Postfix(CardModel __instance, ref Texture2D __result)
    {
        if (__instance == null)
            return;

        Type type = __instance.GetType();
        string? ns = type.Namespace;
        if (ns == null || !ns.StartsWith("Kaguya.", StringComparison.Ordinal))
            return;   // 不是本 mod 的卡，不碰

        string? path = ResolvePath(__instance, type);
        if (path == null || !ResourceLoader.Exists(path))
            return;

        Texture2D? texture = GD.Load<Texture2D>(path);
        if (texture != null && __result != texture)
            __result = texture;
    }

    /// <summary>辉夜卡读 CustomPortraitPath；彩叶卡按类名拼到自己的目录。</summary>
    private static string? ResolvePath(CardModel card, Type type)
    {
        PropertyInfo? prop = type.GetProperty("CustomPortraitPath", BindingFlags.Public | BindingFlags.Instance);
        if (prop != null && prop.PropertyType == typeof(string))
        {
            string? custom = prop.GetValue(card) as string;
            if (!string.IsNullOrEmpty(custom))
                return custom;
        }

        return $"res://images/packed/card_portraits/kaguya/{type.Name}.png";
    }
}

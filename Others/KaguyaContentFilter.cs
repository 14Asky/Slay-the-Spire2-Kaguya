using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Relics;
using Kaguya.HinaMods.Character;
using Kaguya.Characters;
using MegaCrit.Sts2.Core.Runs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Kaguya;

public static class KaguyaContentFilter
{
    private static readonly Assembly KaguyaAssembly = typeof(KaguyaModConfig).Assembly;

    private static bool IsKaguyaCharacter(Player player)
        => player.Character is HinaCharacter
        || player.Character is SakayoriIroha;

    private static bool ShouldAllow(IRunState state)
    {
        if (KaguyaModConfig.NonKaguyaCharactersSeeKaguyaContent)
            return true;
        if (state == null)
            return true;
        return state.Players.Any(p => IsKaguyaCharacter(p));
    }

    public static bool ShouldShowKaguyaContent()
    {
        if (KaguyaModConfig.NonKaguyaCharactersSeeKaguyaContent)
            return true;
        if (!MegaCrit.Sts2.Core.Runs.RunManager.Instance.IsInProgress)
            return false;
        var state = MegaCrit.Sts2.Core.Runs.RunManager.Instance.DebugOnlyGetState();
        if (state == null)
            return false;
        return state.Players.Any(p => IsKaguyaCharacter(p));
    }

    static void Postfix(AbstractModel __instance, IRunState state, ref bool __result)
    {
        if (!__result) return;
        if (__instance.GetType().Assembly != KaguyaAssembly) return;

        // 版本过滤：不属于当前版本的那一套直接判为不可用。
        // 只作用于辉夜侧（Kaguya.HinaMods.*）；彩叶侧命名空间是 Kaguya.Cards / Kaguya.Relics 等，不受影响。
        if (IsHinaContent(__instance) && IsLegacyContent(__instance) != KaguyaModConfig.UseLegacyHina)
        {
            __result = false;
            return;
        }

        __result = ShouldAllow(state);
    }

    /// <summary>是否属于辉夜侧内容（新旧两套都算）。</summary>
    private static bool IsHinaContent(AbstractModel m)
        => m.GetType().Namespace?.StartsWith("Kaguya.HinaMods", StringComparison.Ordinal) == true;

    /// <summary>是否属于经典版（HinaMods/Legacy 下移植过来的那一套）。</summary>
    private static bool IsLegacyContent(AbstractModel m)
        => m.GetType().Namespace?.StartsWith("Kaguya.HinaMods.Legacy", StringComparison.Ordinal) == true;

    [HarmonyPatch(typeof(RelicModel), nameof(RelicModel.IsAllowed), typeof(IRunState))]
    [HarmonyPostfix]
    static void RelicIsAllowed(RelicModel __instance, IRunState state, ref bool __result)
        => Postfix(__instance, state, ref __result);

    [HarmonyPatch(typeof(EventModel), nameof(EventModel.IsAllowed), typeof(IRunState))]
    [HarmonyPostfix]
    static void EventIsAllowed(EventModel __instance, IRunState state, ref bool __result)
        => Postfix(__instance, state, ref __result);

    /// <summary>
    /// 遗物池后置过滤（注册层之外的兜底）。
    /// 正常情况注册层已经把另一套拦掉了，这里只是防止已注册的残留被枚举到。
    /// </summary>
    [HarmonyPatch(typeof(RelicPoolModel), "get_AllRelics")]
    [HarmonyPostfix]
    static void FilterRelicPool(ref IEnumerable<RelicModel> __result)
    {
        if (__result == null) return;
        bool legacy = KaguyaModConfig.UseLegacyHina;
        __result = __result.Where(r =>
        {
            string? ns = r.GetType().Namespace;
            bool isHina = ns != null && ns.StartsWith("Kaguya.HinaMods", StringComparison.Ordinal);
            if (!isHina) return true;
            bool isLegacy = ns!.StartsWith("Kaguya.HinaMods.Legacy", StringComparison.Ordinal);
            return isLegacy == legacy;
        }).ToList();
    }

    /// <summary>
    /// 药水池后置过滤（兜底）。
    /// <c>PotionModel</c> 没有 <c>IsAllowed</c>（只有 CanBeGeneratedInCombat /
    /// PassesCustomUsabilityCheck），遗物那套 patch 拦不住药水，所以这里单独一层。
    /// </summary>
    [HarmonyPatch(typeof(PotionPoolModel), "get_AllPotions")]
    [HarmonyPostfix]
    static void FilterPotionPool(ref IEnumerable<PotionModel> __result)
    {
        if (__result == null) return;
        bool legacy = KaguyaModConfig.UseLegacyHina;
        __result = __result.Where(p =>
        {
            string? ns = p.GetType().Namespace;
            bool isHina = ns != null && ns.StartsWith("Kaguya.HinaMods", StringComparison.Ordinal);
            if (!isHina) return true;
            bool isLegacy = ns!.StartsWith("Kaguya.HinaMods.Legacy", StringComparison.Ordinal);
            return isLegacy == legacy;
        }).ToList();
    }
}

/// <summary>
/// 版本注册过滤 —— 这是唯一从根源上生效的一层。
///
/// 两套辉夜遗物都在基类上标了 <c>[Pool(typeof(HinaModsRelicPool))]</c>，
/// 两套药水都标了 <c>[Pool(typeof(HinaModsPotionPool))]</c>。
/// BaseLib 的 <c>CustomContentDictionary.AddModel</c> 反射读出这个特性后，
/// 对**每一个**模型无条件调用 <c>ModHelper.AddModelToPool</c>，
/// 于是新旧两套被注册进了**同一个池**，池里自然两套都有。
///
/// 而这个注册是**一次性**的：写进 <c>_moddedContentForPools</c> 后由
/// <c>isFrozen</c> 锁死，引擎没有提供任何注销接口。所以在
/// <c>AllRelics</c> / <c>AllPotions</c> 上做后置过滤只能遮住"枚举到的东西"，
/// 一旦别处读的是缓存字段（<c>_relics</c> / <c>_allPotions</c>）或
/// <c>AllRelicIds</c> 就漏 —— 这正是之前遗物和药水两套都能出现的原因。
///
/// 拦截点因此必须放在注册这一步：只放行当前版本的那一套。
/// </summary>
[HarmonyPatch]
public static class KaguyaModelRegistrationFilter
{
    /// <summary>
    /// 目标方法不存在时直接跳过整个 patch 类，避免 <c>PatchAll</c> 抛异常
    /// 把 mod 初始化整个打断（那样会导致辉夜内容全部不加载）。
    /// </summary>
    [HarmonyPrepare]
    static bool Prepare()
    {
        var target = AccessTools.Method(typeof(ModHelper), "AddModelToPool", new[] { typeof(Type), typeof(Type) });
        if (target == null)
        {
            Log.Info("[Kaguya] 未找到 ModHelper.AddModelToPool(Type,Type)，版本池过滤退回后置过滤");
            return false;
        }
        return true;
    }

    static MethodBase TargetMethod()
        => AccessTools.Method(typeof(ModHelper), "AddModelToPool", new[] { typeof(Type), typeof(Type) });

    [HarmonyPrefix]
    static bool Prefix(Type poolType, Type modelType)
    {
        string? ns = modelType.Namespace;

        // 不是辉夜侧（彩叶 Kaguya.Relics / Kaguya.Cards，以及官方内容）一律放行
        if (ns == null || !ns.StartsWith("Kaguya.HinaMods", StringComparison.Ordinal))
            return true;

        bool isLegacy = ns.StartsWith("Kaguya.HinaMods.Legacy", StringComparison.Ordinal);
        if (isLegacy == KaguyaModConfig.UseLegacyHina)
            return true;

        // 卡牌数量太多，只记录遗物/药水这条线，便于验证池是否干净
        if (poolType.Name.Contains("Relic") || poolType.Name.Contains("Potion"))
        {
            Log.Info($"[Kaguya] 版本过滤：跳过 {modelType.Name} → {poolType.Name}（当前 {KaguyaModConfig.ResolveVersion()}）");
        }
        return false;
    }
}

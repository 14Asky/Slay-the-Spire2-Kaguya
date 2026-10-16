using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace Kaguya.HinaMods.Patches
{
    /// <summary>
    /// 让自定义关键词「歌声」在卡面描述末尾自动追加的那行标题显示为紫色。
    /// <para>
    /// 引擎的 CardKeywordExtensions.GetCardText 把关键词标题硬编码成 [gold]…[/gold]，
    /// 本补丁只在关键词是「歌声」时把金色标签替换成紫色标签，其余关键词（消耗 / 虚无 / 保留…）
    /// 与所有卡片描述正文一概不受影响。
    /// </para>
    /// <para>
    /// 目标类型在游戏程序集里是 internal，因此用字符串解析目标方法，避免编译期可见性问题；
    /// 补丁由 Others/KaguyaModInitializer.cs 里的 Harmony("Kaguya.Mod").PatchAll() 统一应用。
    /// </para>
    /// 基础设施类，无本地化键。
/// </summary>
    [HarmonyPatch]
    internal static class EchoKeywordPurplePatch
    {
        private const string TargetTypeName = "MegaCrit.Sts2.Core.Entities.Cards.CardKeywordExtensions";

        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(AccessTools.TypeByName(TargetTypeName), "GetCardText");
        }

        [HarmonyPostfix]
        private static void Postfix(CardKeyword keyword, ref string __result)
        {
            if (__result == null || keyword != KaguyaKeywords.Echo)
            {
                return;
            }

            __result = __result.Replace("[gold]", "[purple]").Replace("[/gold]", "[/purple]");
        }
    }
}

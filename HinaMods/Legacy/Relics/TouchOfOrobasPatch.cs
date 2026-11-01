using Godot;
using HarmonyLib;
using Kaguya.HinaMods.Legacy.Relics;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods
{
    [HarmonyPatch(typeof(TouchOfOrobas))]
    public static class LegacyTouchOfOrobasPatch
    {
        private static int _savedStacks;

        [HarmonyPatch("GetUpgradedStarterRelic")]
        [HarmonyPrefix]
        public static bool GetUpgradedPrefix(RelicModel starterRelic, ref RelicModel __result)
        {
            if (starterRelic is LegacyHinaModsFortuneRelic fortune)
            {
                _savedStacks = fortune.SavedTsukimiStacks;
                GD.Print($"[LegacyTouchOfOrobasPatch] GetUpgraded: captured stacks = {_savedStacks}");
                __result = ModelDb.Relic<LegacyHinaModsAncientFortuneRelic>();
                return false;
            }
            return true;
        }

        [HarmonyPatch("AfterObtained")]
        [HarmonyPrefix]
        public static void AfterObtainedPrefix(TouchOfOrobas __instance)
        {
            if (_savedStacks > 0) return;

            if (__instance.Owner == null) return;

            var id = __instance.StarterRelic
                ?? __instance.Owner.Relics.FirstOrDefault(r => r.Rarity == RelicRarity.Starter)?.Id;
            if (id == null) return;

            var relic = __instance.Owner.GetRelicById(id);
            if (relic is LegacyHinaModsFortuneRelic fortune)
                _savedStacks = fortune.SavedTsukimiStacks;
        }

        [HarmonyPatch("AfterObtained")]
        [HarmonyPostfix]
        public static void AfterObtainedPostfix(TouchOfOrobas __instance, ref Task __result)
        {
            if (_savedStacks <= 0) return;
            int stacks = _savedStacks;
            _savedStacks = 0;

            var original = __result;
            __result = TransferStacks(original, __instance, stacks);
        }

        private static async Task TransferStacks(Task original, TouchOfOrobas instance, int stacks)
        {
            await original;
            if (instance.Owner == null) return;

            var newRelic = instance.Owner.Relics
                .OfType<LegacyHinaModsAncientFortuneRelic>()
                .FirstOrDefault();
            if (newRelic != null)
            {
                AccessTools.Field(typeof(LegacyHinaModsAncientFortuneRelic), "_savedTsukimiStacks")
                    .SetValue(newRelic, stacks);
                AccessTools.Method(typeof(LegacyHinaModsAncientFortuneRelic), "UpdateDisplay")
                    .Invoke(newRelic, null);
                GD.Print($"[LegacyTouchOfOrobasPatch] TransferStacks: set {stacks} on AncientFortuneRelic");
            }
            else
            {
                GD.Print("[LegacyTouchOfOrobasPatch] TransferStacks: AncientFortuneRelic not found in Owner.Relics");
            }
        }
    }
}

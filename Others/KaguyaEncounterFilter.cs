using HarmonyLib;
using Kaguya.Encounters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using System.Collections.Generic;
using System.Linq;

namespace Kaguya;

[HarmonyPatch(typeof(ActModel), nameof(ActModel.CreateMap))]
public static class KaguyaEncounterFilter
{
    private static bool IsModEncounter(EncounterModel encounter)
        => encounter is TsukitoTenchuBossEncounter
        || encounter is TsukitoKongoBossEncounter;

    internal static bool IsModEliteOrMonster(EncounterModel encounter)
        => encounter is OxDemonArthropodEncounter
        || encounter is OxDemonEncounter
        || encounter is TsukitoBagEncounter
        || encounter is IroPEncounter
        || encounter is DoubleTsukibitoEncounter
        || encounter is MixedSoldiersEliteEncounter
        || encounter is QuadTsukibitoEncounter
        || encounter is ThunderEncounter
        || encounter is NayEncounter
        || encounter is TeimeiEncounter;

    private static EncounterModel? FindNonModBoss(ActModel act, EncounterModel? exclude)
    {
        return act.AllBossEncounters
            .Where(e => !IsModEncounter(e))
            .FirstOrDefault(e => exclude == null || e != exclude);
    }

    public static void Prefix(ActModel __instance, RunState runState)
    {
        if (KaguyaContentFilter.ShouldShowKaguyaContent())
            return;

        if (__instance is not Hive and not Glory)
            return;

        if (__instance.BossEncounter != null && IsModEncounter(__instance.BossEncounter))
        {
            var replacement = FindNonModBoss(__instance, null);
            if (replacement != null)
                __instance.SetBossEncounter(replacement);
        }

        if (__instance.HasSecondBoss
            && __instance.SecondBossEncounter != null
            && IsModEncounter(__instance.SecondBossEncounter))
        {
            var replacement = FindNonModBoss(__instance, __instance.BossEncounter);
            if (replacement != null)
                __instance.SetSecondBossEncounter(replacement);
        }
    }
}

[HarmonyPatch(typeof(ActModel), nameof(ActModel.GenerateRooms))]
public static class KaguyaEliteMonsterFilter
{
    public static void Postfix(ActModel __instance)
    {
        if (KaguyaContentFilter.ShouldShowKaguyaContent())
            return;

        var rooms = Traverse.Create(__instance).Field("_rooms").GetValue<RoomSet>();
        if (rooms == null) return;

        rooms.normalEncounters.RemoveAll(e => KaguyaEncounterFilter.IsModEliteOrMonster(e));
        rooms.eliteEncounters.RemoveAll(e => KaguyaEncounterFilter.IsModEliteOrMonster(e));
    }
}
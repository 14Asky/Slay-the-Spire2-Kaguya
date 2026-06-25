using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Rooms;
using Kaguya.HinaMods.Character;
using Kaguya.Characters;
using MegaCrit.Sts2.Core.Runs;
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
        __result = ShouldAllow(state);
    }

    [HarmonyPatch(typeof(RelicModel), nameof(RelicModel.IsAllowed), typeof(IRunState))]
    [HarmonyPostfix]
    static void RelicIsAllowed(RelicModel __instance, IRunState state, ref bool __result)
        => Postfix(__instance, state, ref __result);

    [HarmonyPatch(typeof(EventModel), nameof(EventModel.IsAllowed), typeof(IRunState))]
    [HarmonyPostfix]
    static void EventIsAllowed(EventModel __instance, IRunState state, ref bool __result)
        => Postfix(__instance, state, ref __result);

    [HarmonyPatch(typeof(ActModel), nameof(ActModel.GenerateRooms))]
    [HarmonyPostfix]
    static void GenerateRoomsPostfix(ActModel __instance)
    {
        if (ShouldShowKaguyaContent())
            return;
        var rooms = Traverse.Create(__instance).Field("_rooms").GetValue<RoomSet>();
        if (rooms == null) return;
        rooms.normalEncounters.RemoveAll(e => e.GetType().Assembly == KaguyaAssembly);
        rooms.eliteEncounters.RemoveAll(e => e.GetType().Assembly == KaguyaAssembly);
    }
}

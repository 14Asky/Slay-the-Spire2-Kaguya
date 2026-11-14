#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Saves.Migrations;
using MegaCrit.Sts2.Core.Saves.Migrations.ProgressSaves;
using MegaCrit.Sts2.Core.Saves.Migrations.RunHistories;
using MegaCrit.Sts2.Core.Saves.Migrations.SerializableRuns;

namespace Kaguya;

// 0.113 introduces a vanilla CARD.JOY. Only saves crossing the pre-0.113
// schema boundary can use the old unprefixed ID for Kaguya's status card.
// Never alias CARD.JOY globally: new saves and network packets need vanilla Joy.
[HarmonyPatch]
internal static class KaguyaJoySaveMigration
{
    internal const string OldId = "CARD.JOY";
    internal const string NewId = "CARD.KAGUYA-JOY";

    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.DeclaredMethod(typeof(SerializableRunV20ToV21), "ApplyMigration");
        yield return AccessTools.DeclaredMethod(typeof(RunHistoryV10ToV11), "ApplyMigration");
        yield return AccessTools.DeclaredMethod(typeof(ProgressSaveV23ToV24), "ApplyMigration");
    }

    [HarmonyPrefix]
    private static void MigrateLegacyJoy(MigratingData saveData)
    {
        int changed = RewriteReferences(saveData.GetRawNode());
        if (changed > 0)
            Log.Info($"[Kaguya] Migrated {changed} legacy Joy reference(s) to {NewId}.");
    }

    internal static int RewriteReferences(JsonNode? node)
    {
        int changed = 0;
        if (node is JsonObject obj)
        {
            foreach (var property in obj.ToList())
            {
                if (IsLegacyId(property.Value))
                {
                    obj[property.Key] = NewId;
                    changed++;
                }
                else
                    changed += RewriteReferences(property.Value);
            }
        }
        else if (node is JsonArray array)
        {
            for (int i = 0; i < array.Count; i++)
            {
                if (IsLegacyId(array[i]))
                {
                    array[i] = NewId;
                    changed++;
                }
                else
                    changed += RewriteReferences(array[i]);
            }
        }
        return changed;
    }

    private static bool IsLegacyId(JsonNode? node) =>
        node is JsonValue value && value.TryGetValue<string>(out string? id) && id == OldId;
}

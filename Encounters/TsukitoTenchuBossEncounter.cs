using Godot;
using Kaguya;
using HarmonyLib;
using Kaguya.Encounters;
using Kaguya.Monsters;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Rooms;
using System.Collections.Generic;
using System.Linq;

namespace Kaguya.Encounters
{
    public sealed class TsukitoTenchuBossEncounter : EncounterModel
    {
        public override RoomType RoomType => RoomType.Boss;

        public override string CustomBgm => string.Empty;

        public override string BossNodePath => "res://images/packed/map/tsukito_tenchu_icon";

        public override MegaSkeletonDataResource BossNodeSpineResource => null;

        public override IEnumerable<MonsterModel> AllPossibleMonsters => new[] { ModelDb.Monster<TsukitoTenchu>() };

        protected override bool HasCustomBackground => false;

        public override float GetCameraScaling() => 0.85f;

        public override Vector2 GetCameraOffset() => Vector2.Down * 70f;

        protected override IReadOnlyList<(MonsterModel, string)> GenerateMonsters()
        {
            return new[] { (ModelDb.Monster<TsukitoTenchu>().ToMutable(), (string)null) };
        }
    }
}
[HarmonyPatch(typeof(Hive), nameof(Hive.GenerateAllEncounters))]
public static class HiveGenerateAllEncountersPatch
{
    static void Postfix(ref IEnumerable<EncounterModel> __result)
    {
        __result = __result
            .Concat(new[] { ModelDb.Encounter<TsukitoTenchuBossEncounter>() })
            .Distinct();
    }
}

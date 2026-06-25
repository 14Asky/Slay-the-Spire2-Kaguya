using Godot;
using HarmonyLib;
using Kaguya.Encounters;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Singleton;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Unlocks;
using System.Collections.Generic;

namespace Kaguya;

public sealed class FinalAct : ActModel
{
    protected override int BaseNumberOfRooms => 7;

    public new LocString Title => new LocString("acts", "FINAL_ACT.title");

    // --- 怪物与事件配置 ---
    public override IEnumerable<EncounterModel> GenerateAllEncounters()
    {
        return new List<EncounterModel> {
            ModelDb.Encounter<TsukitoBosatsuBossEncounter>(),
            ModelDb.Encounter<TsukitoZuishoEliteEncounter>()
        };
    }

    public override IEnumerable<AncientEventModel> AllAncients
    {
        get
        {
            var fushi = ModelDb.AncientEvent<FushiAncient>();
            if (fushi != null) return new List<AncientEventModel> { fushi };
            return new List<AncientEventModel>();
        }
    }

    public override IEnumerable<AncientEventModel> GetUnlockedAncients(UnlockState state) => AllAncients;

    // --- 视听配置 (借用 Act 3) ---
    public override string[] BgMusicOptions => new string[] { "event:/music/act3_boss_queen", "event:/music/act3_boss_queen" };
    public override string[] MusicBankPaths => new string[] { "res://banks/desktop/act3_a1.bank", "res://banks/desktop/act3_a2.bank" };
    public override string AmbientSfx => "event:/sfx/ambience/act3_ambience";


    // 地图背景底色
    public override Color MapBgColor => new Color(0f, 0f, 0f, 0f);
    // 已走过的路径连线颜色
    public override Color MapTraveledColor => new Color("ffffff");
    // 未走过的路径连线颜色
    public override Color MapUntraveledColor => new Color("b0b0b0");

    public override string ChestSpineSkinNameNormal => "act3";
    public override string ChestSpineSkinNameStroke => "act3_stroke";
    public override string ChestOpenSfx => "event:/sfx/ui/treasure/treasure_act3";

    public override IEnumerable<EncounterModel> BossDiscoveryOrder => new List<EncounterModel> { ModelDb.Encounter<QueenBoss>() };
    public override IEnumerable<EventModel> AllEvents => new List<EventModel>();
    protected override void ApplyActDiscoveryOrderModifications(UnlockState unlockState) { }


    // 修正：参数为 Rng，返回 MapPointTypeCounts
    public override MapPointTypeCounts GetMapPointTypes(Rng mapRng)
    {
        // 休息点数量：平均值7，标准差1，最小6，最大7（参考 Overgrowth）
        int restCount = mapRng.NextGaussianInt(7, 1, 6, 7);
        // 问号数量：使用标准随机方法（10~14之间，倾向于12）
        int unknownCount = MapPointTypeCounts.StandardRandomUnknownCount(mapRng);
        // 剩余房间会自动补充为普通怪物房
        return new MapPointTypeCounts(unknownCount, restCount);
    }
    [HarmonyPatch(typeof(MultiplayerScalingModel), nameof(MultiplayerScalingModel.GetMultiplayerScaling))]
    public static class MultiplayerScalingPatch
    {
        static bool Prefix(EncounterModel encounter, int actIndex, ref decimal __result)
        {
            if (actIndex == 3)
            {
                // 直接返回第三层的缩放系数（actIndex = 2）
                __result = MultiplayerScalingModel.GetMultiplayerScaling(encounter, 2);
                return false; // 跳过原方法，避免索引越界
            }
            return true;
        }
    }
    public override int Index => 2;

    public override bool IsDefault => true;

    public override bool IsUnlocked(UnlockState unlockState) => true;
}

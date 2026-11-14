using Godot;
using HarmonyLib;
using Kaguya.Encounters;
using Kaguya.Events;
using Kaguya.Relics;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace Kaguya;

[HarmonyPatch]
public static class Act4Logic
{
    // 第四层注入与结局跳转
    [HarmonyPatch(typeof(RunManager), nameof(RunManager.EnterNextAct))]
    [HarmonyPrefix]
    public static bool Prefix_Sequence(RunManager __instance, ref Task __result)
    {
        var state = AccessTools.Property(typeof(RunManager), "State").GetValue(__instance) as RunState;
        if (state == null) return true;

        // 第三层结束时注入第四层
        if (state.CurrentActIndex == 2)
        {
            // 先同步执行遗物替换（阻塞等待完成）
            DoAct3RelicSwap(state).GetAwaiter().GetResult();

            // 检查进入第四层的条件：记忆碎片·决心 或 八千年的思念
            bool canEnterAct4 = state.Players.Any(p =>
                p.Relics.Any(r => r is MemoryFragmentResolution) ||
                p.Relics.Any(r => r is EightThousandYearsLonging));

            if (!canEnterAct4)
            {
                GD.Print("[Act 4 Mod] 无进入第四层条件，游戏正常结束。");
                return true; // 让原版流程正常结束
            }

            var acts = state.Acts.ToList();
            if (acts.Count == 3)
            {
                var finalAct = ModelDb.Act<FinalAct>().ToMutable();
                finalAct.GenerateRooms(state.Rng.UpFront, state.UnlockState, state.Players.Count > 1);
                acts.Add(finalAct);
                AccessTools.Property(typeof(RunState), "Acts").SetValue(state, acts);
                GD.Print("[Act 4 Mod] 已注入第四层。");
            }
            return true; // 让原版 EnterNextAct 处理跳转
        }

        // 第四层完成后：劫持结局流程，跳转到自定义结局事件
        if (state.CurrentActIndex >= 3 && state.Act is FinalAct)
        {
            __result = HandleKaguyaEnding(__instance, state);
            return false;
        }

        return true;
    }

    // 第三层遗物替换逻辑
    private static async Task DoAct3RelicSwap(RunState state)
    {
        bool hasTriggerRelic = state.Players.Any(p =>
            p.Relics.Any(r => r is SuperdimensionalKaguya) ||
            p.Relics.Any(r => r is SunHeaddress));

        if (!hasTriggerRelic) return;

        NDebugAudioManager.Instance.Play("dasuki.mp3");

        foreach (var player in state.Players)
        {
            bool playerHadTrigger = player.Relics.Any(r =>
                r is SuperdimensionalKaguya || r is SunHeaddress);
            if (!playerHadTrigger) continue;

            // 移除 SuperdimensionalKaguya 和 SunHeaddress
            var relicsToRemove = player.Relics
                .Where(r => r is SuperdimensionalKaguya || r is SunHeaddress)
                .ToList();
            foreach (var relic in relicsToRemove)
                await RelicCmd.Remove(relic);

            // 移除所有 MemoryFragment 变体
            var memoryFragments = player.Relics
                .Where(r => r is MemoryFragmentStart || r is MemoryFragmentDaily ||
                            r is MemoryFragmentConcert || r is MemoryFragmentResolution)
                .ToList();
            foreach (var frag in memoryFragments)
                await RelicCmd.Remove(frag);

            // 给予 EightThousandYearsLonging
            var eightThousand = ModelDb.Relic<EightThousandYearsLonging>().ToMutable();
            await RelicCmd.Obtain(eightThousand, player);
        }
    }

    // 直接进入结局事件，事件内部会调用 OnEnded
    private static async Task HandleKaguyaEnding(RunManager instance, RunState state)
    {
        var currentRoom = state.CurrentRoom;
        if (currentRoom is EventRoom er && er.CanonicalEvent is KaguyaEnding)
        {
            await instance.WinRun();
            return;
        }
        else
        {
            var endingEvent = ModelDb.Event<KaguyaEnding>();
            if (endingEvent == null)
            {
                await instance.WinRun();
                return;
            }
            ClearScreens();
            await instance.EnterRoom(new EventRoom(endingEvent));
            await FadeIn();
        }
    }

    private static async Task FadeIn(bool showTransition = true)
    {
        if (!TestMode.IsOn)
        {
            await NGame.Instance.Transition.RoomFadeIn(showTransition);
        }
    }

    private static void ClearScreens()
    {
        if (!TestMode.IsOn)
        {
            NOverlayStack.Instance.Clear();
        }
    }

    // 线性地图构建
    [HarmonyPatch(typeof(StandardActMap), "AssignPointTypes")]
    [HarmonyPostfix]
    public static void Postfix_Map(StandardActMap __instance)
    {
        var rm = RunManager.Instance;
        var state = AccessTools.Property(typeof(RunManager), "State").GetValue(rm) as RunState;
        if (state != null && state.Act is FinalAct)
        {
            var grid = AccessTools.Property(typeof(StandardActMap), "Grid").GetValue(__instance) as MapPoint[,];
            if (grid == null) return;

            for (int r = 1; r < grid.GetLength(1); r++)
                for (int c = 0; c < 7; c++) grid[c, r] = null;

            SetPoint(grid, 3, 1, MapPointType.RestSite);
            SetPoint(grid, 3, 2, MapPointType.Shop);
            SetPoint(grid, 3, 3, MapPointType.Elite);
            SetPoint(grid, 3, 4, MapPointType.RestSite);

            __instance.StartingMapPoint.PointType = MapPointType.Ancient;
            __instance.BossMapPoint.PointType = MapPointType.Boss;

            __instance.StartingMapPoint.Children.Clear();
            __instance.StartingMapPoint.AddChildPoint(grid[3, 1]);
            grid[3, 1].Children.Clear(); grid[3, 1].AddChildPoint(grid[3, 2]);
            grid[3, 2].Children.Clear(); grid[3, 2].AddChildPoint(grid[3, 3]);
            grid[3, 3].Children.Clear(); grid[3, 3].AddChildPoint(grid[3, 4]);
            grid[3, 4].Children.Clear(); grid[3, 4].AddChildPoint(__instance.BossMapPoint);
        }
    }

    // 强制指定怪物
    [HarmonyPatch(typeof(ActModel), nameof(ActModel.GenerateRooms), new[] { typeof(MegaCrit.Sts2.Core.Random.Rng), typeof(MegaCrit.Sts2.Core.Unlocks.UnlockState), typeof(bool), typeof(bool) })]
    [HarmonyPostfix]
    public static void Postfix_Rooms(ActModel __instance)
    {
        if (__instance is FinalAct)
        {
            var rooms = AccessTools.Field(typeof(ActModel), "_rooms").GetValue(__instance) as RoomSet;
            if (rooms != null)
            {
                rooms.Boss = ModelDb.Encounter<TsukitoBosatsuBossEncounter>();
                rooms.eliteEncounters.Clear();
                rooms.eliteEncounters.Add(ModelDb.Encounter<TsukitoZuishoEliteEncounter>());
            }
        }
    }

    // 资源重定向
    [HarmonyPatch(typeof(ActModel), "get_MapTopBg")]
    [HarmonyPrefix]
    public static bool Prefix_MapTopBg(ActModel __instance, ref Texture2D __result)
    {
        if (__instance is FinalAct)
        {
            __result = MegaCrit.Sts2.Core.Assets.PreloadManager.Cache.GetCompressedTexture2D("res://images/packed/map/map_bgs/glory/map_top_glory.png");
            return false;
        }
        return true;
    }

    [HarmonyPatch(typeof(ActModel), "get_MapMidBg")]
    [HarmonyPrefix]
    public static bool Prefix_MapMidBg(ActModel __instance, ref Texture2D __result)
    {
        if (__instance is FinalAct)
        {
            __result = MegaCrit.Sts2.Core.Assets.PreloadManager.Cache.GetCompressedTexture2D("res://images/packed/map/map_bgs/glory/map_middle_glory.png");
            return false;
        }
        return true;
    }

    [HarmonyPatch(typeof(ActModel), "get_MapBotBg")]
    [HarmonyPrefix]
    public static bool Prefix_MapBotBg(ActModel __instance, ref Texture2D __result)
    {
        if (__instance is FinalAct)
        {
            __result = MegaCrit.Sts2.Core.Assets.PreloadManager.Cache.GetCompressedTexture2D("res://images/packed/map/map_bgs/glory/map_bottom_glory.png");
            return false;
        }
        return true;
    }

    [HarmonyPatch(typeof(ActModel), "get_RestSiteBackgroundPath")]
    [HarmonyPrefix]
    public static bool Prefix_Rest(ActModel __instance, ref string __result)
    {
        if (__instance is FinalAct) { __result = "res://scenes/rest_site/glory_rest_site.tscn"; return false; }
        return true;
    }

    [HarmonyPatch(typeof(ActModel), "get_BackgroundScenePath")]
    [HarmonyPrefix]
    public static bool Prefix_Bg(ActModel __instance, ref string __result)
    {
        if (__instance is FinalAct) { __result = "res://scenes/backgrounds/glory/glory_background.tscn"; return false; }
        return true;
    }

    [HarmonyPatch(typeof(ActModel), nameof(ActModel.GenerateBackgroundAssets))]
    [HarmonyPrefix]
    public static bool Prefix_Assets(ActModel __instance, MegaCrit.Sts2.Core.Random.Rng rng, ref BackgroundAssets __result)
    {
        if (__instance is FinalAct) { __result = new BackgroundAssets("glory", rng); return false; }
        return true;
    }

    // 音频稳定性保护
    [HarmonyPatch(typeof(NRunMusicController), "UpdateMusic")]
    [HarmonyPrefix]
    public static bool Prefix_Music()
    {
        var state = AccessTools.Property(typeof(RunManager), "State").GetValue(RunManager.Instance) as RunState;
        if (state != null && state.Act is FinalAct) return false;
        return true;
    }

    private static void SetPoint(MapPoint[,] grid, int col, int row, MapPointType type)
    {
        MapPoint p = new MapPoint(col, row);
        p.PointType = type;
        p.CanBeModified = false;
        grid[col, row] = p;
    }
}

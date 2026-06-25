using BaseLib.Config;
using BaseLib.Utils;
using Godot;
using Godot.Bridge;
using HarmonyLib;
using Kaguya;
using Kaguya.CardPools;
using Kaguya.Cards;
using Kaguya.RelicPools;
using Kaguya.Relics;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Multiplayer.Replay;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace KaguyaMod
{
    [ModInitializer(nameof(Initialize))]
    public static class KaguyaModInitializer
    {
        public static void Initialize()
        {
            try
            {
                ScriptManagerBridge.LookupScriptsInAssembly(typeof(KaguyaModInitializer).Assembly);
                ModHelper.AddModelToPool(typeof(EventCardPool), typeof(Remember));
                ModHelper.AddModelToPool(typeof(EventCardPool), typeof(StarlitSea));
                ModHelper.AddModelToPool(typeof(EventCardPool), typeof(ILoveMyself));
                ModHelper.AddModelToPool(typeof(CurseCardPool), typeof(Boring));
                ModHelper.AddModelToPool(typeof(KaguyaCardPool), typeof(Coax));
                ModHelper.AddModelToPool(typeof(StatusCardPool), typeof(Happy));
                ModHelper.AddModelToPool(typeof(StatusCardPool), typeof(Joy));
                ModHelper.AddModelToPool(typeof(StatusCardPool), typeof(LostSoul));
                ModHelper.AddModelToPool(typeof(StatusCardPool), typeof(Pride));
                ModHelper.AddModelToPool(typeof(StatusCardPool), typeof(Relocate));
                ModHelper.AddModelToPool(typeof(EventCardPool), typeof(HappySynthesizer));
                ModHelper.AddModelToPool(typeof(EventCardPool), typeof(WorldPrincess));
                ModHelper.AddModelToPool(typeof(EventCardPool), typeof(ExOtogibanashi));
                ModHelper.AddModelToPool(typeof(TokenCardPool), typeof(YachiyoOtogibanashi));
                ModHelper.AddModelToPool(typeof(TokenCardPool), typeof(Otogibanashi));
                ModHelper.AddModelToPool(typeof(StatusCardPool), typeof(Satisfied));
                ModHelper.AddModelToPool(typeof(EventCardPool), typeof(TransientSymphony));
                ModHelper.AddModelToPool(typeof(EventCardPool), typeof(Reply));
                ModHelper.AddModelToPool(typeof(EventCardPool), typeof(Ray));
                ModHelper.AddModelToPool(typeof(EventCardPool), typeof(Melt));
                ModHelper.AddModelToPool(typeof(EventRelicPool), typeof(YachiyoShrine));
                ModHelper.AddModelToPool(typeof(EventRelicPool), typeof(TsukuyomiShrine));
                ModHelper.AddModelToPool(typeof(EventRelicPool), typeof(KaguyaAccount));
                ModHelper.AddModelToPool(typeof(EventRelicPool), typeof(HappySynthesizerRelic));
                ModHelper.AddModelToPool(typeof(EventRelicPool), typeof(PerformanceCostume));
                ModHelper.AddModelToPool(typeof(EventRelicPool), typeof(TearsOfMemory));
                ModHelper.AddModelToPool(typeof(EventRelicPool), typeof(PartingGift));
                ModHelper.AddModelToPool(typeof(EventRelicPool), typeof(Ideal));
                ModHelper.AddModelToPool(typeof(EventRelicPool), typeof(OrdinaryHappiness));
                ModHelper.AddModelToPool(typeof(KaguyaRelicPool), typeof(RundownApartment));
                ModHelper.AddModelToPool(typeof(KaguyaCardPool), typeof(StrikeIroha));
                ModHelper.AddModelToPool(typeof(KaguyaCardPool), typeof(DefendIroha));
                ModHelper.AddModelToPool(typeof(KaguyaCardPool), typeof(TsukuyomiNight));
                ModHelper.AddModelToPool(typeof(KaguyaCardPool), typeof(PartTimeJob));
                ModHelper.AddModelToPool(typeof(EventRelicPool), typeof(DataUsb));
                ModHelper.AddModelToPool(typeof(EventRelicPool), typeof(BrothersDeposit));
                ModHelper.AddModelToPool(typeof(EventRelicPool), typeof(LeiBigSword));
                ModHelper.AddModelToPool(typeof(KaguyaRelicPool), typeof(ChallengeLetter));
                ModHelper.AddModelToPool(typeof(KaguyaRelicPool), typeof(ThoughtStrand));
                ModHelper.AddModelToPool(typeof(KaguyaRelicPool), typeof(EightThousandYearsLonging));
                ModHelper.AddModelToPool(typeof(KaguyaRelicPool), typeof(SchoolUniform));
                ModHelper.AddModelToPool(typeof(KaguyaRelicPool), typeof(ElectronicKeyboard));
                ModHelper.AddModelToPool(typeof(KaguyaRelicPool), typeof(WorkClothes));
                ModHelper.AddModelToPool(typeof(KaguyaRelicPool), typeof(FlatSheathCharm));
                ModHelper.AddModelToPool(typeof(KaguyaRelicPool), typeof(RoyalTwinBlades));
                ModHelper.AddModelToPool(typeof(KaguyaRelicPool), typeof(TechContactLens));
                var harmony = new Harmony("Kaguya.Mod");
                harmony.PatchAll();
            }
            catch (System.Exception e)
            {
                Log.Error("KaguyaMod - 加载失败: " + e.Message);
                return;
            }
            ModConfigRegistry.Register("Kaguya", new KaguyaModConfig());
                        Log.Info("KaguyaMod - 加载成功!");
        }
    }
}
public static class KaguyaTags
{
    public const CardTag StatusTag = (CardTag)1000;
    public const CardTag SongTag = (CardTag)1001;  
    public const CardTag PartnerTag = (CardTag)1004;
}
[HarmonyPatch(typeof(CardModel), nameof(CardModel.PortraitPath), MethodType.Getter)]
public static class CardModel_GetPortrait_Patch
{
    private static readonly Dictionary<string, string> CustomPortraits = new(StringComparer.OrdinalIgnoreCase)
    {
        [nameof(BelieveInYou)] = "res://images/packed/card_portraits/colorless/believe_in_you.png",
        [nameof(Coordinate)] = "res://images/packed/card_portraits/colorless/coordinate.png",
        [nameof(GangUp)] = "res://images/packed/card_portraits/colorless/gang_up.png",
        [nameof(Apotheosis)] = "res://images/packed/card_portraits/event/Kami.png"
    };

    static void Postfix(CardModel __instance, ref string __result)
    {
        var className = __instance?.GetType().Name;
        if (string.IsNullOrEmpty(className)) return;
        if (!CustomPortraits.TryGetValue(className, out var path)) return;
        if (!ResourceLoader.Exists(path)) return;
        __result = path;
    }
}

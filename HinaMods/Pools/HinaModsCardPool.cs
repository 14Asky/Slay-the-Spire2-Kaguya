using System;
using System.Collections.Generic;
using System.Linq;
using BaseLib.Abstracts;
using Godot;
using Kaguya.HinaMods.Cards.Rare;
using Kaguya.HinaMods.Cards.Uncommon;
using Kaguya.HinaMods.Powers;
using Kaguya.HinaMods.SupportCards.Common;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using Kaguya.HinaMods.Cards;
using Kaguya.HinaMods.Extensions;
using Kaguya.HinaModsCode.cards.Rare;

namespace Kaguya.HinaMods.Character;

/// <summary>
/// 辉夜侧卡池：卡框材质 <c>hina</c>，卡池标题绑定 HinaCharacter 的角色 ID。
/// 继承 HinaModsCard 的卡在 PoolAttribute(Inherited=true) 下会自动进本池。
/// 基础设施类，无本地化键。
/// </summary>
public class HinaModsCardPool : CustomCardPoolModel
{
	public override string CardFrameMaterialPath => "hina";
	// 卡池ID（绑定角色）
	public override string Title => HinaCharacter.CharacterId;

	public override Texture2D CustomFrame(CustomCardModel card)
	{
		return card.Type switch
		{
			CardType.Attack => PreloadManager.Cache.GetAsset<Texture2D>("res://scenes/hinamods/cards/card_frame_attack.png"),
			CardType.Power => PreloadManager.Cache.GetAsset<Texture2D>("res://scenes/hinamods/cards/card_frame_power.png"),
			_ => PreloadManager.Cache.GetAsset<Texture2D>("res://scenes/hinamods/cards/card_frame_skill.png"),
		};
	}
	// 能量图标路径
	public override string BigEnergyIconPath => "Charui/big_energy.png".ImagePath();
	public override string TextEnergyIconPath => "Charui/text_energy.png".ImagePath();

	public override Color DeckEntryCardColor => new("#f79431");
	// 非无色卡牌（角色专属）
	public override bool IsColorless => false;

	/// <summary>
	/// 卡池最终过滤。
	/// 引擎的 <c>CardPoolModel.AllCards</c> 在 <c>GenerateAllCards()</c> 之后还会执行
	/// <c>ModHelper.ConcatModelsFromMods</c>，把带 <c>PoolAttribute</c> 的模型**自动追加**进来——
	/// 也就是说只改 GenerateAllCards 的清单，另一套版本的卡仍然会混进池里。
	/// 这里在最终出口按命名空间剔一遍，才是真正生效的开关。
	/// </summary>
	public override IEnumerable<CardModel> AllCards
	{
		get
		{
			bool legacy = KaguyaModConfig.UseLegacyHina;
			foreach (CardModel card in base.AllCards)
			{
				bool isLegacy = card.GetType().Namespace?.StartsWith("Kaguya.HinaMods.Legacy", StringComparison.Ordinal) == true;
				if (isLegacy == legacy)
				{
					yield return card;
				}
			}
		}
	}

	protected override CardModel[] GenerateAllCards()
	{
		// 经典版：走 master 分支的实现（HinaMods/Legacy 下的 Legacy* 卡）
		if (KaguyaModConfig.UseLegacyHina)
			return GenerateLegacyCards();
		return GenerateNewCards();
	}

	private static CardModel[] GenerateNewCards()
	{
		return new CardModel[]
		{
			// 基础卡牌
			ModelDb.Card<Escort>(),
			ModelDb.Card<Raid>(),
			ModelDb.Card<MoonTorii>(),
			ModelDb.Card<Disgust>(),
			ModelDb.Card<HeavyHammer>(),
			ModelDb.Card<ReturnFlow>(),
			ModelDb.Card<SpinningStrike>(),
			ModelDb.Card<StartBroadcast>(),
			ModelDb.Card<Cheers>(),
			ModelDb.Card<FirstBeat>(),
			//ModelDb.Card<HinaModsLiveAccident>(),
			ModelDb.Card<Unbelievable>(),
			//ModelDb.Card<HinaModsMakeup>(),
			ModelDb.Card<Omurice>(),
			ModelDb.Card<Birth>(),
			ModelDb.Card<PeacefulDaily>(),
			ModelDb.Card<Listen>(),
			ModelDb.Card<LiveBroadcast>(),
			ModelDb.Card<Appear>(),
			ModelDb.Card<DownRush>(),
			ModelDb.Card<Finale>(),
			ModelDb.Card<KaguyaHime>(),

		// 罕见卡牌
			ModelDb.Card<ReturnPerformance>(),
			ModelDb.Card<MoonlightFlow>(),
			ModelDb.Card<PieceOfCake>(),
			ModelDb.Card<StringChorus>(),
			ModelDb.Card<Moonfall>(),
			ModelDb.Card<FullMoon>(),
			ModelDb.Card<ActSpoiled>(),
			ModelDb.Card<NoteCombo>(),
			ModelDb.Card<FirstEncounter>(),
			ModelDb.Card<CoordinatedOps>(),
			ModelDb.Card<VastSky>(),
			ModelDb.Card<Chorus>(),
			ModelDb.Card<VocalSupport>(),
			ModelDb.Card<Celebration>(),
			ModelDb.Card<WatchAMovie>(),
			ModelDb.Card<Worry>(),
			ModelDb.Card<StarlitBrilliance>(),
			ModelDb.Card<Starfall>(),
			ModelDb.Card<PlayfulLife>(),
			ModelDb.Card<TurnBack>(),
			ModelDb.Card<DanceSteps>(),
			ModelDb.Card<Wait>(),
			ModelDb.Card<SeekMemories>(),
			ModelDb.Card<RoyalFan>(),
			ModelDb.Card<CurtainRaiser>(),
			ModelDb.Card<GoldenScale>(),
			ModelDb.Card<VictoryFlag>(),
			ModelDb.Card<Gaze>(),

			// 稀有卡牌
			ModelDb.Card<MoonSet>(),
			ModelDb.Card<SupportWeaponSwap>(),
			ModelDb.Card<WorldsNumberOnePrincess>(),
			ModelDb.Card<CelestialForm>(),
			ModelDb.Card<Encore>(),
			ModelDb.Card<AllRoundSupport>(),
			ModelDb.Card<MoonGate>(),
			ModelDb.Card<Peach>(),
			ModelDb.Card<HeavenlyRobe>(),
			ModelDb.Card<ShiningStage>(),
			ModelDb.Card<Reverberation>(),
			ModelDb.Card<CurtainFalls>(),
			ModelDb.Card<Annihilation>(),
			ModelDb.Card<Climax>(),

			ModelDb.Card<YachiyoClone>(),
			ModelDb.Card<StarRiverDream>(),
			ModelDb.Card<TricolorStreamer>(),

			ModelDb.Card<CounterattackStart>(),
			ModelDb.Card<DecisiveBlow>(),
			ModelDb.Card<PerfectCombo>(),
			ModelDb.Card<LeapingStrike>(),
			ModelDb.Card<GraduationPerformance>(),
			ModelDb.Card<UnderFireworks>(),
			ModelDb.Card<HeavensWill>(),
			ModelDb.Card<SameMoonDifferentTime>(),
			ModelDb.Card<Preparation>(),
		   
			//多人联机牌
			ModelDb.Card<YachiyosAid>(),
			ModelDb.Card<Gather>(),
		};
	}

	/// <summary>
	/// 经典版卡池（master 分支的内容）。
	/// 每移植一张旧卡就在这里登记一条；未登记的不会出现在经典版卡池。
	/// </summary>
	private static CardModel[] GenerateLegacyCards()
	{
		return new CardModel[]
		{
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.LegacyGenerateSupportCard>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.SupportCards.Common.LegacyGenerateSupportStrike>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.Uncommon.LegacyHinaModsMoonBarrier>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.LegacyHinaModsMoonEclipse>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.LegacyHinaModsMoonEclipseStrike>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.LegacyHinaModsMoonScar>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.LegacyHinaModsMuffin>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.LegacyMoonTideReturn>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.LegacySingerAllStrike>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.LegacySingerGuard>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.LegacySingerSelfDebuff>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.LegacySingerStrike>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.LegacyHinaModsClearGame>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.LegacyHinaModsOmurice>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.LegacyHinaModsBirth>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.LegacyDailySong>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.LegacySingerHoldLine>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.LegacySingerRecycle>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.LegacyMoonlightTripleStrike>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.LegacyFullMoonStrike>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.LegacySupportAllocation>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.LegacyHinaModsMoonFinalStrike>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.Uncommon.LegacySingerDraw>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.LegacyHinaModsMoonDraw>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.Uncommon.LegacyHinaModsFortuneExpend>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.Uncommon.LegacyHinaModsBreakEvil>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.LegacyExhaustSingerGuard>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.LegacyNoteCombo>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.LegacyHinaModsMoonConcerto>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.LegacySupportStrikeAuraCard>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.Uncommon.LegacyHomeRun>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.LegacyGrowth>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.LegacySingerSupportGuard>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.LegacySupportBurst>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.LegacySingerBulwark>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.LegacyParryParry>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.LegacyTsukimiDraw>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.LegacyTsukimiFrenzy>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.LegacyGameLife>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.LegacyStarNightFree>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.Rare.LegacySingerFugue>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.LegacySingerWaitBlock>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.LegacySingerReshape>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.LegacyMoonLightAria>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.LegacySupportRhythmCard>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.LegacySupportEnergyCharge>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.LegacySupportMoonlightCall>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.LegacySingerMoonStrike>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.LegacySupportAuraCard>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.LegacySingerEchoCopy>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.LegacyTsukimiForm>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.LegacyEncore>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.Uncommon.LegacyRallySupportCard>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.Uncommon.LegacyHeavenlyRaimentCard>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.LegacyHinaModsLeap>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.LegacyTsukimiRobe>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.LegacySingerConvert>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.Uncommon.LegacySingerCascade>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.Rare.LegacySingerComboStrike>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.LegacySupportObliteration>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.Rare.LegacyHinaModsMoonlight>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.LegacyConcertedPreparation>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.LegacySupportAssemble>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.Rare.LegacyFinalPerformance>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.SupportCards.Moons.LegacyHinaModsTsukimiBlessing>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.Rare.LegacyHinaModsMoonlightRecall>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.SupportCards.Common.LegacyHinaModsWinStrike>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.Uncommon.LegacySingerComb>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.LegacyLeapStrike>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.LegacyGraduationPerformance>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.LegacySupportForgeCard>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.LegacyMoonlightSlash>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.LegacyMoonlightOverflow>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.LegacyConfluence>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.LegacyMultiSupportGather>(),
			ModelDb.Card<Kaguya.HinaMods.Legacy.Cards.LegacyMoonlightTeamBless>(),
		};
	}
}

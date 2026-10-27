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

	protected override CardModel[] GenerateAllCards()
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
}

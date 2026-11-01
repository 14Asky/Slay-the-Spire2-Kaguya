using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using Kaguya.HinaMods;
using Kaguya.HinaMods.Character;
using Kaguya.HinaMods.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Cards;

[Pool(typeof(HinaModsCardPool))]
public abstract class HinaModsCard(int cost, CardType type, CardRarity rarity, TargetType target) :
	CustomCardModel(cost, type, rarity, target)
{
	public virtual HashSet<string> CustomTags { get; } = new HashSet<string>();

	// 🔥 仅新增：标记本回合是否【手动打出】（兼容余音逻辑，无报错）
	private bool _isPlayedManuallyThisTurn;

	public HinaModsCard() : this(0, CardType.Skill, CardRarity.Basic, TargetType.Self)
	{
	}

	// 修复类型转换错误 + 仅支援卡添加消耗+虚无
	public override List<CardKeyword> CanonicalKeywords =>
		CustomTags.Contains(LegacyCustomCardTags.SUPPORT)
		? [
			CardKeyword.Exhaust,
			CardKeyword.Ethereal
		]
		: base.CanonicalKeywords.ToList();

	/// <summary>
	/// 密封重写，禁止子类覆盖，保证基类逻辑永不失效
	/// 合并顺序：官方原生提示 → 子类自定义提示 → 标签提示（歌者/支援）
	/// </summary>
	protected sealed override IEnumerable<IHoverTip> ExtraHoverTips
	{
		get
		{
			// 1. 保留【官方原生悬浮提示】（第一优先级）
			foreach (var tip in base.ExtraHoverTips)
				yield return tip;

			// 2. 加载【子类自定义悬浮提示】（第二优先级：易伤/力量/敏捷等）
			foreach (var tip in GetCustomHoverTips())
				yield return tip;

			// 3. 自动添加【标签悬浮提示】（第三优先级：歌者/支援，纯官方写法）
			if (CustomTags.Contains(LegacyCustomCardTags.SINGER))
			{
				yield return new HoverTip(
					new LocString("card_keywords", "SINGER.title"),
					new LocString("card_keywords", "SINGER.description")
				);
			}
			if (CustomTags.Contains(LegacyCustomCardTags.SINGERCARD))
			{
				yield return new HoverTip(
					new LocString("card_keywords", "SINGERCARD.title"),
					new LocString("card_keywords", "SINGERCARD.description")
				);
			}
			if (CustomTags.Contains(LegacyCustomCardTags.SUPPORT))
			{
				yield return new HoverTip(
					new LocString("card_keywords", "SUPPORT.title"),
					new LocString("card_keywords", "SUPPORT.description")
				);
			}
			if (CustomTags.Contains(LegacyCustomCardTags.SUPPORTCARD))
			{
				yield return new HoverTip(
					new LocString("card_keywords", "SUPPORTCARD.title"),
					new LocString("card_keywords", "SUPPORTCARD.description")
				);
			}
			if (CustomTags.Contains(LegacyCustomCardTags.MOONVIEW))
			{
				yield return new HoverTip(
					new LocString("card_keywords", "MOONVIEW.title"),
					new LocString("card_keywords", "MOONVIEW.description")
				);
			}
		}
	}

	/// <summary>
	/// 🔥 子类专用：重写此方法添加自定义悬浮（官方规范，无冲突）
	/// 所有卡牌的易伤、力量、格挡等提示，都重写这个方法
	/// </summary>
	protected virtual IEnumerable<IHoverTip> GetCustomHoverTips()
	{
		// 默认空，子类按需重写
		return Enumerable.Empty<IHoverTip>();
	}

	public string GetSingerDescription()
	{
		string baseDesc = GetDescriptionForPile(PileType.None);

		if (CustomTags.Contains(LegacyCustomCardTags.SINGER))
		{
			const string singerTitle = "歌者";
			const string singerDesc = "打出后进入消耗堆，你的回合开始时返回抽牌堆。";
			return $"[gold]{singerTitle}[/gold]\n{singerDesc}\n{baseDesc}";
		}

		return baseDesc;
	}

	// ================================================================================
	// ✅ 歌者核心机制1：打出后进入消耗堆（无修改）
	// ================================================================================
	protected override CardLocation GetResultLocationForCardPlay()
	{
		if (CustomTags.Contains(LegacyCustomCardTags.SINGER))
		{
			return new CardLocation(Owner, PileType.Exhaust, CardPilePosition.Bottom);
		}

		return base.GetResultLocationForCardPlay();
	}

	// 🔥 仅新增：参考余音代码，标记【手动打出】的歌者牌
	public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await base.AfterCardPlayed(choiceContext, cardPlay);

		// 严格匹配余音逻辑：仅手动打出的歌者牌标记
		if (cardPlay.Card != this || cardPlay.IsAutoPlay || !CustomTags.Contains(LegacyCustomCardTags.SINGER))
			return;

		_isPlayedManuallyThisTurn = true;
	}

	// ================================================================================
	// 🔥 仅修改这里：只有【本回合手动打出】的歌者牌才返回抽牌堆
	// ================================================================================
	public override async Task AfterSideTurnEnd(PlayerChoiceContext ctx, CombatSide side, IEnumerable<Creature> participants)
	{
		// ✅ 正确调用基类方法
		await base.AfterSideTurnEnd(ctx, side, participants);

		// 原有逻辑完全不变
		// 只在玩家回合结束生效
		if (side != CombatSide.Player || Owner == null)
			return;

		// 获取消耗堆
		var exhaustPile = PileType.Exhaust.GetPile(Owner);
		if (exhaustPile.IsEmpty)
			return;

		// 筛选歌者卡牌 + 🔥 新增限制：本回合手动打出
		var singerCards = exhaustPile.Cards
			.OfType<HinaModsCard>()
			.Where(c => c.CustomTags.Contains(LegacyCustomCardTags.SINGER) && c._isPlayedManuallyThisTurn)
			.ToList();

		if (singerCards.Count == 0)
			return;

		// 直接遍历，随机插入抽牌堆
		foreach (var card in singerCards)
		{
			await CardPileCmd.Add(card, PileType.Draw, CardPilePosition.Random);
			// 回合结束重置标记
			card._isPlayedManuallyThisTurn = false;
		}
	}

	// ================================================================================
	// 普通预览复用现有专属大图，避免落到缺失的小图路径。
	// ================================================================================
	public override string CustomPortraitPath
	{
		get
		{
			var path = $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigCardImagePath();
			return ResourceLoader.Exists(path) ? path : "card.png".BigCardImagePath();
		}
	}

	public override string PortraitPath
	{
		get
		{
			return CustomPortraitPath;
		}
	}

	public override string BetaPortraitPath
	{
		get
		{
			var path = $"Beta/{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();
			return ResourceLoader.Exists(path) ? path : PortraitPath;
		}
	}
}

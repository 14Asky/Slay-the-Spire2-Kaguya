//using BaseLib.Utils;
//using Godot;
//using Kaguya.HinaMods.Legacy.Powers;
//using MegaCrit.Sts2.Core.Commands;
//using MegaCrit.Sts2.Core.Entities.Cards;
//using MegaCrit.Sts2.Core.Entities.Creatures;
//using MegaCrit.Sts2.Core.GameActions.Multiplayer;
//using MegaCrit.Sts2.Core.HoverTips;
//using MegaCrit.Sts2.Core.Localization.DynamicVars;
//using MegaCrit.Sts2.Core.Models;
//using MegaCrit.Sts2.Core.Models.Powers;
//using MegaCrit.Sts2.Core.ValueProps;
//using Kaguya.HinaMods;
//using Kaguya.HinaMods.Legacy.Cards;
//using System.Collections.Generic;
//using System.Threading.Tasks;

//namespace Kaguya.HinaMods.Legacy.Cards.Rare;

//// 月之终焉 · 终极月见卡牌
//public sealed class LegacyHinaModsTsukimiUltimate : HinaModsCard
//{
//	// 无动态数值变量
//	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
//	{
//	};
//	public override List<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

//	// ====================== 核心修复：对齐DailySong悬浮写法 ======================
//	// 保留基类悬浮（MOONVIEW标签提示）+ 追加月见BUFF悬浮提示
//	protected override IEnumerable<IHoverTip> ExtraHoverTips
//	{
//		get
//		{
//			// 1. 先继承基类的所有悬浮提示（自定义标签、基础提示）
//			foreach (var tip in base.ExtraHoverTips)
//				yield return tip;

//			// 2. 追加月见之力的悬浮提示
//			yield return HoverTipFactory.FromPower<LegacyTsukimiTimePower>();
//		}
//	}

//	// 模组自定义标签
//	public override HashSet<string> CustomTags => new HashSet<string>() { LegacyCustomCardTags.MOONVIEW };

//	// 构造：0费 技能牌 上古稀有 目标自身
//	public LegacyHinaModsTsukimiUltimate()
//		: base(1, CardType.Skill, CardRarity.Ancient, TargetType.Self) { }

//	// 核心打出条件：拥有 ≥1000 层【月见 LegacyTsukimiTimePower】才可打出
//	protected override bool IsPlayable
//	{
//		get
//		{
//			LegacyTsukimiTimePower power = base.Owner.Creature.GetPower<LegacyTsukimiTimePower>();
//			return power != null && power.Amount >= 1000;
//		}
//	}

//	// 满足条件金光闪烁
//	protected override bool ShouldGlowGoldInternal => IsPlayable;

//	// =========================================================================
//	// 打出逻辑：消耗1000层月见 → 获得月夜层数的力量+敏捷
//	// =========================================================================
//	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
//	{
//		// 1. 消耗 1000层 月见(LegacyTsukimiTimePower)
//		LegacyTsukimiTimePower tsukimiPower = base.Owner.Creature.GetPower<LegacyTsukimiTimePower>();
//		if (tsukimiPower != null)
//		{
//			await PowerCmd.ModifyAmount(
//				tsukimiPower,
//				-1000,
//				base.Owner.Creature,
//				this
//			);
//		}

//		// 2. 获取当前 月夜(LegacyFortunePower) 层数
//		LegacyFortunePower moonPower = base.Owner.Creature.GetPower<LegacyFortunePower>();
//		decimal currentMoon = moonPower?.Amount ?? 0;

//		// 3. 获得等同于月夜层数的 力量 + 敏捷
//		if (currentMoon > 0)
//		{
//			await PowerCmd.Apply<StrengthPower>(Owner.Creature, currentMoon, Owner.Creature, this);
//			await PowerCmd.Apply<DexterityPower>(Owner.Creature, currentMoon, Owner.Creature, this);
//		}
//	}

//	// 升级逻辑
//	protected override void OnUpgrade()
//	{
//		AddKeyword(CardKeyword.Retain);
//	}
//}

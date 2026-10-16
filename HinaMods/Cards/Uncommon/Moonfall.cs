using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
// 引入日志命名空间（STS2官方战斗日志）
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;
using Kaguya.HinaMods.Cards;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards.Uncommon;

/// <summary>
/// 月陨（Moonfall）
/// 本地化键：KAGUYA-HINA_MODS_BREAK_EVIL.title / .description
/// 1费 · 攻击牌 · 罕见 · 目标：单个敌人
/// 卡面：造成{CalculatedDamage:diff()}点伤害。
///       每拥有1层[gold]月夜[/gold]，额外造成{ExtraDamage:diff()}点伤害。
/// 代码实测：CalculationBase=8
/// 升级：CalculationBase +2；ExtraDamage +1
/// </summary>
public class Moonfall : HinaModsCard
{
	// 🔥 修复2：标准构造函数（和完美打击完全一致）
	public Moonfall()
		: base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
	{
	}

	// 🔥 核心：参考完美打击，配置官方标准伤害动态变量
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		// 基础伤害：8
		new CalculationBaseVar(8m),
		// 每层月夜的加成：未升级+1，升级后+2
		new ExtraDamageVar(1m),
		// 最终伤害 = 基础伤害 + 月夜层数 × 额外加成（官方自动计算）
		new CalculatedDamageVar(ValueProp.Move)
			.WithMultiplier((CardModel card, Creature _) => 
				// 获取玩家身上的月夜层数
				card.Owner.Creature.GetPower<MoonlightPower>()?.Amount ?? 0)
	];

	// 悬浮提示显示月夜BUFF（保留你的原有逻辑）
	protected override IEnumerable<IHoverTip> GetCustomHoverTips()
	{
		return new IHoverTip[]
		{
		   HoverTipFactory.FromPower<MoonlightPower>(),
		};
	}

	// 🔥 修复3：完全参考完美打击的OnPlay写法，无需手动计算伤害
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

		// 官方标准攻击方式，动态变量自动结算最终伤害
		await DamageCmd.Attack(DynamicVars.CalculatedDamage)
			.FromCard(this, cardPlay)
			.Targeting(cardPlay.Target)
			.WithHitFx(null, null, "heavy_attack.mp3")
			.WithHitVfxNode((Creature t) => NBigSlashVfx.Create(t))
			.WithHitVfxNode((Creature t) => NBigSlashImpactVfx.Create(t))
			.Execute(choiceContext);
	}

	// 🔥 修复4：升级逻辑（完美打击同款）
	protected override void OnUpgrade()
	{
		// 基础伤害+2（原有逻辑）
		DynamicVars.CalculationBase.UpgradeValueBy(2m);
		// 每层月夜加成 +1 → 未升级1/升级后2（满足你的需求）
		DynamicVars.ExtraDamage.UpgradeValueBy(1m);
	}
}

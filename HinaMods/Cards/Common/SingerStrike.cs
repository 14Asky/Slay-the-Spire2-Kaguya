using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

// 歌者基础打击牌：1费攻击牌，8点伤害，升级11点
// 效果：弃牌堆中没有歌者牌时，此牌伤害额外+4
public sealed class SingerStrike() : HinaModsCard(1,
	CardType.Attack, CardRarity.Common,
	TargetType.AnyEnemy)
{
	// 自定义标签（保留不变）
	public override HashSet<string> CustomTags => new HashSet<string>() { CustomCardTags.SINGER };


	// 核心：弃牌堆检测逻辑 + 官方动态伤害计算
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
        // 基础伤害：10点
        new CalculationBaseVar(10m),
        // 额外伤害：5点（无歌者牌时触发）
        new ExtraDamageVar(5m),
        // 最终伤害结算（官方标准）
        new CalculatedDamageVar(ValueProp.Move)
			.WithMultiplier((CardModel card, Creature _) =>
			{
                // 获取玩家
                Player player = card.Owner;
                // ==============================================
                // 核心修改：消耗堆 → 弃牌堆 (Discard)
                // ==============================================
                CardPile discardPile = PileType.Discard.GetPile(player);
				List<CardModel> singerCards = discardPile.Cards
					.OfType<HinaModsCard>()
					.Where(c => c.CustomTags?.Contains(CustomCardTags.SINGER) == true)
					.Cast<CardModel>()
					.ToList();

                // 弃牌堆无歌者牌 → 触发额外伤害
                return !singerCards.Any() ? 1 : 0;
			})
	];

	// 标准攻击API
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await DamageCmd.Attack(DynamicVars.CalculatedDamage)
			.FromCard(this, cardPlay)
			.Targeting(cardPlay.Target)
			.Execute(choiceContext);
	}

	// 升级：8→11伤害 (+3)
	protected override void OnUpgrade()
	{
		DynamicVars.CalculationBase.UpgradeValueBy(3m);
	}
}
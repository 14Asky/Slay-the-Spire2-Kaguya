using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using System.Collections.Generic; // 新增：IEnumerable<Creature>所需
using System.Threading.Tasks;

// 严格匹配你的命名空间
namespace Kaguya.HinaMods.Legacy.Powers;

public sealed class LegacyGrowthPower : CustomPowerModel
{
	// ====================== 官方同款基础配置（Counter可叠加） ======================
	public override PowerType Type => PowerType.Buff;
	public override PowerStackType StackType => PowerStackType.Counter; // 可叠加
	public override bool AllowNegative => false;
	public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

	// 自定义图标（保持不变）
	public override string CustomPackedIconPath => "res://images/hinamods/Powers/growth_power.png";
	public override string CustomBigIconPath => "res://images/hinamods/Powers/growth_power.png";

	// 显示当前叠加层数（官方标准）
	public override int DisplayAmount => (int)Amount;

	public override async Task AfterSideTurnEnd(PlayerChoiceContext ctx, CombatSide side, IEnumerable<Creature> participants)
	{
		// ✅ 正确调用基类方法
		await base.AfterSideTurnEnd(ctx, side, participants);

		// 原有逻辑完全不变：仅自己的回合结束触发
		if (side == Owner.Side)
		{
			// 你已经修复的PowerCmd调用保持不变
			await PowerCmd.Apply<LegacyFortunePower>(ctx, Owner, 1, Owner, null);
		}
	}

	// ====================== 官方标准空实现（格式统一，无需修改） ======================
	public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		await base.AfterPlayerTurnStart(choiceContext, player);
	}

	public override async Task AfterApplied(Creature applier, CardModel cardSource)
	{
		await base.AfterApplied(applier, cardSource);
	}
}

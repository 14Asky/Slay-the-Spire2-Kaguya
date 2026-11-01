using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using System.Collections.Generic; // 新增：IEnumerable<Creature>所需
using System.Threading.Tasks;

// 统一命名空间
namespace Kaguya.HinaMods.Legacy.Powers;

public sealed class LegacyTsukimiContractPower : CustomPowerModel
{
    // ====================== 官方同款基础配置（与参考代码完全统一） ======================
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter; // 可叠加
    public override bool AllowNegative => false;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced; // 官方必备：多层独立数据

    // 图标、层数显示（保持不变）
    public override string CustomPackedIconPath => "res://images/hinamods/Powers/tsukimi_contract_power.png";
    public override string CustomBigIconPath => "res://images/hinamods/Powers/tsukimi_contract_power.png";
    public override int DisplayAmount => (int)Amount;

    // ====================== 核心逻辑：每打出一张牌，获得1层月夜（API正确，无需修改） ======================
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await base.AfterCardPlayed(choiceContext, cardPlay);

        // 官方标准判断：存活+玩家+自己打出的牌
        if (Owner == null || !Owner.IsAlive || !Owner.IsPlayer)
            return;
        if (cardPlay.Card.Owner != Owner.Player)
            return;

        // 获得 1 层 月夜(LegacyFortunePower)
        await PowerCmd.Apply<LegacyFortunePower>(new ThrowingPlayerChoiceContext(), Owner, 1m, Owner, null, false);
    }

    // ====================== 回合结束自动移除（API已更新） ======================
    // 🔥 唯一修改：将废弃的AfterTurnEnd更新为最新的AfterSideTurnEnd签名
    public override async Task AfterSideTurnEnd(PlayerChoiceContext ctx, CombatSide side, IEnumerable<Creature> participants)
    {
        // ✅ 正确调用基类方法
        await base.AfterSideTurnEnd(ctx, side, participants);

        // 原有逻辑完全不变：仅自己的回合结束移除
        if (side == Owner.Side)
        {
            await PowerCmd.Remove(this);
        }
    }
}
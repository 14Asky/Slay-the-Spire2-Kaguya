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
using MegaCrit.Sts2.Core.Models.Powers;
using System.Collections.Generic; // 新增：IEnumerable<Creature>所需
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Powers;

public sealed class LegacyTsukimiFormPower : CustomPowerModel
{
    // ====================== 基础配置 ======================
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    // 图标配置
    public override string CustomPackedIconPath => "res://images/hinamods/Powers/tsukimi_form.png";
    public override string CustomBigIconPath => "res://images/hinamods/Powers/tsukimi_form.png";
    public override int DisplayAmount => (int)Amount;

    // ====================== 回合开始逻辑（API正确，无需修改） ======================
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterPlayerTurnStart(choiceContext, player);
        if (Owner != player.Creature || !Owner.IsAlive)
            return;

        LegacyFortunePower fortunePower = Owner.GetPower<LegacyFortunePower>();
        if (fortunePower == null) return;

        // 消耗2层月夜 → 获得1能量
        await PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), fortunePower, -2m, Owner, null, false);
        await PlayerCmd.GainEnergy(1, player);

        // 月夜≥10 → 额外1能量
        if (fortunePower.Amount >= 10)
        {
            await PlayerCmd.GainEnergy(1, player);
        }
    }

    // ====================== 回合结束：动态获得月夜（API已更新） ======================
    // 🔥 唯一修改：将废弃的AfterTurnEnd更新为最新的AfterSideTurnEnd签名
    public override async Task AfterSideTurnEnd(PlayerChoiceContext ctx, CombatSide side, IEnumerable<Creature> participants)
    {
        // ✅ 正确调用基类方法
        await base.AfterSideTurnEnd(ctx, side, participants);

        // 原有逻辑完全不变
        if (side != CombatSide.Player || Owner == null || !Owner.IsAlive)
            return;

        // 核心：Amount = 卡牌动态变量（基础2，升级3）
        await PowerCmd.Apply<LegacyFortunePower>(new ThrowingPlayerChoiceContext(), Owner, Amount, Owner, null, false);
    }
}
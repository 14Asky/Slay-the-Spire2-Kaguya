using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Kaguya.HinaMods;
using Kaguya.HinaMods.Legacy.Cards;
using System.Collections.Generic; // 新增：IEnumerable<Creature>所需
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Powers;

public sealed class LegacySupportSkillPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    // 核心优化：禁用实例化，大幅降低性能消耗
    public override PowerInstanceType InstanceType => PowerInstanceType.None;
    // 优化：禁止层数为负，提升稳定性
    public override bool AllowNegative => false;
    // 核心优化：关闭层数UI显示，杜绝UI高频刷新卡顿
    public override int DisplayAmount => 0;

    public override string CustomPackedIconPath => "res://images/hinamods/Powers/support_power.png";
    public override string CustomBigIconPath => "res://images/hinamods/Powers/support_power.png";

    // 精简减费逻辑：不再排除任何支援牌（API正确，无需修改）
    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;

        // 仅保留基础条件：自身的技能牌
        bool isPlayerSkill = card.Owner.Creature == Owner && card.Type == CardType.Skill;

        // 满足条件即减费
        if (isPlayerSkill)
        {
            modifiedCost = originalCost - 1;
            modifiedCost = modifiedCost < 0 ? 0 : modifiedCost;
            return true;
        }
        return false;
    }

    // 精简消耗逻辑：不再排除任何支援牌（API正确，无需修改）
    public override async Task BeforeCardPlayed(CardPlay cardPlay)
    {
        await base.BeforeCardPlayed(cardPlay); // ✅ 补充基类调用，避免官方逻辑丢失

        if (cardPlay.Card == null) return;

        // 仅保留基础条件：自身的技能牌
        bool isPlayerSkill = cardPlay.Card.Owner.Creature == Owner && cardPlay.Card.Type == CardType.Skill;

        // 满足条件即消耗层数
        if (isPlayerSkill)
        {
            await PowerCmd.Remove(this);
        }
    }

    // ====================== 回合结束移除BUFF（API已更新） ======================
    // 🔥 唯一修改：将废弃的AfterTurnEnd更新为最新的AfterSideTurnEnd签名
    public override async Task AfterSideTurnEnd(PlayerChoiceContext ctx, CombatSide side, IEnumerable<Creature> participants)
    {
        // ✅ 正确调用基类方法
        await base.AfterSideTurnEnd(ctx, side, participants);

        // 原有逻辑完全不变：回合结束移除BUFF
        await PowerCmd.Remove(this);
    }
}
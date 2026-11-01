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
// 引入支援打击的命名空间（必须加，否则识别不了SupportStrike）
using Kaguya.HinaMods.Legacy.SupportCards.Common;

namespace Kaguya.HinaMods.Legacy.Powers;

public sealed class LegacySupportAttackPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    // 关键优化：禁用实例化，减少性能消耗
    public override PowerInstanceType InstanceType => PowerInstanceType.None;
    public override bool AllowNegative => false;

    // 关键优化：不显示层数，关闭UI高频刷新
    public override int DisplayAmount => 0;

    // 图标不变
    public override string CustomPackedIconPath => "res://images/hinamods/Powers/support_power.png";
    public override string CustomBigIconPath => "res://images/hinamods/Powers/support_power.png";

    // 减费逻辑：仅排除 支援打击(LegacySupportStrike) 这一张牌（API正确，无需修改）
    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        // 条件：玩家攻击牌
        bool isPlayerAttack = card.Owner.Creature == Owner && card.Type == CardType.Attack;
        // 🔥 核心修改：仅排除【支援打击】这一张牌
        bool isExcludeCard = card is LegacySupportStrike;

        // 满足条件：是玩家攻击牌 + 不是支援打击 → 减费
        if (isPlayerAttack && !isExcludeCard)
        {
            modifiedCost = originalCost - 1;
            modifiedCost = modifiedCost < 0 ? 0 : modifiedCost;
            return true;
        }
        return false;
    }

    // 修复：补充基类调用 + 标准async Task签名
    public override async Task BeforeCardPlayed(CardPlay cardPlay)
    {
        await base.BeforeCardPlayed(cardPlay); // ✅ 补充基类调用，避免官方逻辑丢失

        if (cardPlay.Card == null) return;

        // 条件：玩家攻击牌
        bool isPlayerAttack = cardPlay.Card.Owner.Creature == Owner && cardPlay.Card.Type == CardType.Attack;
        // 🔥 核心修改：仅排除【支援打击】这一张牌
        bool isExcludeCard = cardPlay.Card is LegacySupportStrike;

        // 满足条件：是玩家攻击牌 + 不是支援打击 → 消耗层数
        if (isPlayerAttack && !isExcludeCard)
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
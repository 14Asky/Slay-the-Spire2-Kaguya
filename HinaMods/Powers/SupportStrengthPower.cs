using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Powers;

/// <summary>
/// 支援·临时力量（SupportStrengthPower）
/// 本地化键：KAGUYA-SUPPORT_TEMP_STRENGTH_POWER.title / .description
/// 效果：本回合内获得 {Amount} 点力量，回合结束时失去（对标官方「临时力量」，如「预备打击」）。
/// 实现：施加/叠加时把层数同步到【力量】(StrengthPower)，回合结束时先把层数扣回再移除自身。
/// 触发来源：打出 token 支援攻击牌（见 HinaModsCard.AfterCardPlayed）。
/// 若身上有【交换武器】(WeaponSwapPower)，则改为直接给永久力量、不挂本能力。
/// </summary>
public sealed class SupportStrengthPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    public override int DisplayAmount => (int)Amount;

    public override string CustomPackedIconPath => "res://images/hinamods/Powers/support_temp_strength.png";
    public override string CustomBigIconPath => "res://images/hinamods/Powers/support_temp_strength.png";

    /// <summary>施加时：把层数同步给【力量】。</summary>
    public override async Task BeforeApplied(Creature target, decimal amount, Creature applier, CardModel cardSource)
    {
        await base.BeforeApplied(target, amount, applier, cardSource);
        await PowerCmd.Apply<StrengthPower>(
            new ThrowingPlayerChoiceContext(), target, amount, applier, cardSource, silent: true);
    }

    /// <summary>
    /// 层数变化时：同步增减【力量】。
    /// 首次施放时 amount == Amount（BeforeApplied 已经给过），跳过以免重复叠加。
    /// </summary>
    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature applier, CardModel cardSource)
    {
        if (power != this || amount == Amount)
            return;

        await PowerCmd.Apply<StrengthPower>(choiceContext, Owner, amount, applier, cardSource, silent: true);
    }

    /// <summary>回合结束时：先扣回力量，再移除自身。</summary>
    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner))
            return;

        Flash();
        decimal amount = Amount;
        await PowerCmd.Remove(this);
        await PowerCmd.Apply<StrengthPower>(choiceContext, Owner, -amount, Owner, null, silent: true);
    }
}

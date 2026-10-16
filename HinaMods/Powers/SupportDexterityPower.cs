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
/// 支援·临时敏捷（SupportDexterityPower）
/// 本地化键：KAGUYA-SUPPORT_TEMP_DEXTERITY_POWER.title / .description
/// 效果：本回合内获得 {Amount} 点敏捷，回合结束时失去（对标官方「临时敏捷」，如「预判」）。
/// 实现：施加/叠加时把层数同步到【敏捷】(DexterityPower)，回合结束时先把层数扣回再移除自身。
/// 触发来源：打出 token 支援技能牌（见 HinaModsCard.AfterCardPlayed）。
/// 若身上有【交换武器】(WeaponSwapPower)，则改为直接给永久敏捷、不挂本能力。
/// </summary>
public sealed class SupportDexterityPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    public override int DisplayAmount => (int)Amount;

    public override string CustomPackedIconPath => "res://images/hinamods/Powers/support_temp_dexterity.png";
    public override string CustomBigIconPath => "res://images/hinamods/Powers/support_temp_dexterity.png";

    /// <summary>施加时：把层数同步给【敏捷】。</summary>
    public override async Task BeforeApplied(Creature target, decimal amount, Creature applier, CardModel cardSource)
    {
        await base.BeforeApplied(target, amount, applier, cardSource);
        await PowerCmd.Apply<DexterityPower>(
            new ThrowingPlayerChoiceContext(), target, amount, applier, cardSource, silent: true);
    }

    /// <summary>
    /// 层数变化时：同步增减【敏捷】。
    /// 首次施放时 amount == Amount（BeforeApplied 已经给过），跳过以免重复叠加。
    /// </summary>
    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature applier, CardModel cardSource)
    {
        if (power != this || amount == Amount)
            return;

        await PowerCmd.Apply<DexterityPower>(choiceContext, Owner, amount, applier, cardSource, silent: true);
    }

    /// <summary>回合结束时：先扣回敏捷，再移除自身。</summary>
    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner))
            return;

        Flash();
        decimal amount = Amount;
        await PowerCmd.Remove(this);
        await PowerCmd.Apply<DexterityPower>(choiceContext, Owner, -amount, Owner, null, silent: true);
    }
}

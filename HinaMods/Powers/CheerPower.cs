using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Powers;

/// <summary>
/// 声援（CheerPower）
/// 本地化键：KAGUYA-CHEER_UPGRADE_POWER.title / .description
/// 卡面：本回合内，你每获得1张牌就将其升级。
/// </summary>
public sealed class CheerPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
    public override PowerInstanceType InstanceType => PowerInstanceType.None;

    public override string CustomPackedIconPath => "res://images/hinamods/Powers/cheer_upgrade_power.png";
    public override string CustomBigIconPath => "res://images/hinamods/Powers/cheer_upgrade_power.png";

    public override int DisplayAmount => (int)Amount;

    /// <summary>尝试升级一张牌（幂等：已升级或不可升级则跳过）。</summary>
    private static void TryUpgrade(CardModel card)
    {
        if (card == null || !card.IsMutable || !card.IsUpgradable || card.IsUpgraded)
            return;

        card.UpgradeInternal();
        card.FinalizeUpgradeInternal();
    }

    /// <summary>抽牌入手（含效果抽牌，fromHandDraw=false）。</summary>
    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        await base.AfterCardDrawn(choiceContext, card, fromHandDraw);

        if (Owner == null || card == null)
            return;

        // 只处理拥有者自己的牌
        if (card.Owner == null || card.Owner.Creature != Owner)
            return;

        TryUpgrade(card);
    }

    /// <summary>生成入手（护卫 / 八方援护 / 宴会 / 你和我 等）。</summary>
    public override async Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
    {
        await base.AfterCardGeneratedForCombat(card, creator);

        if (Owner == null || card == null)
            return;

        if (card.Owner == null || card.Owner.Creature != Owner)
            return;

        TryUpgrade(card);
    }

    /// <summary>拥有者回合结束即移除（效果只持续一回合）。</summary>
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, System.Collections.Generic.IEnumerable<MegaCrit.Sts2.Core.Entities.Creatures.Creature> participants)
    {
        await base.AfterSideTurnEnd(choiceContext, side, participants);

        if (Owner != null && side == Owner.Side)
        {
            await PowerCmd.Remove(this);
        }
    }
}

using BaseLib.Abstracts;
using Kaguya.CardPools;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using System.Threading.Tasks;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace Kaguya.Powers;

public sealed class LegacySurprisePerformancePower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter; // 层数表示下回合给予的创作层数

    // 可选图标
    public override string CustomPackedIconPath => "res://images/packed/card_portraits/kaguya/SurprisePerformancePower.png";
    public override string CustomBigIconPath => "res://images/packed/card_portraits/kaguya/SurprisePerformancePower.png";

    // 使用新版回合开始钩子（玩家侧）
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != Owner.Side) return;
        var player = Owner.Player;
        if (player == null) return;

        int amount = (int)Amount;
        if (amount > 0)
        {
            // 创建上下文（非关键操作，使用占位上下文）
            var choiceContext = new ThrowingPlayerChoiceContext();
            await PowerCmd.Apply<LegacyCreationPower>(choiceContext, Owner, amount, Owner, null);
            Flash();
        }
        // 移除自身（效果仅触发一次）
        await PowerCmd.Remove(this);
    }
}

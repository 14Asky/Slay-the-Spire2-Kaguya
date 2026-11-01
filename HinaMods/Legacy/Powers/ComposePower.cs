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

public sealed class LegacyComposePower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string CustomPackedIconPath => "res://images/packed/card_portraits/kaguya/ComposePower.png";
    public override string CustomBigIconPath => "res://images/packed/card_portraits/kaguya/ComposePower.png";

    // 使用新版回合开始钩子（替代 BeforeHandDraw）
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != Owner.Side) return;
        // 获取拥有者玩家（能力存在于玩家生物上）
        var player = Owner.Player;
        if (player == null) return;

        int amount = (int)Amount;
        // 注意：PowerCmd.Apply 需要 PlayerChoiceContext 参数，此处使用占位上下文
        var choiceContext = new ThrowingPlayerChoiceContext();
        await PowerCmd.Apply<LegacyCreationPower>(choiceContext, Owner, amount, Owner, null);
        Flash();
    }
}

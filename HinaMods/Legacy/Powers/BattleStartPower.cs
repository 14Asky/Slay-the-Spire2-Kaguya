using BaseLib.Abstracts;
using Kaguya.CardPools;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace Kaguya.Powers;

public sealed class LegacyBattleStartPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string CustomPackedIconPath => "res://images/packed/card_portraits/kaguya/BattleStartPower.png";
    public override string CustomBigIconPath => "res://images/packed/card_portraits/kaguya/BattleStartPower.png";

    // 使用新版回合开始钩子（替代 BeforeHandDraw）
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != CombatSide.Player) return;
        // 确保是能力拥有者的回合（能力位于玩家生物上）
        if (!CombatManager.Instance.IsPartOfPlayerTurn(Owner.Player)) return;

        int strengthAmount = (int)Amount;
        if (strengthAmount <= 0) return;

        var choiceContext = new ThrowingPlayerChoiceContext();

        // 给所有队友（包括自己）增加力量
        var teammates = combatState.GetTeammatesOf(Owner).Where(c => c != null && c.IsAlive && c.IsPlayer);
        foreach (var teammate in teammates)
        {
            await PowerCmd.Apply<StrengthPower>(choiceContext, teammate, strengthAmount, Owner, null);
        }

        // 给所有敌人增加力量
        foreach (var enemy in combatState.HittableEnemies)
        {
            await PowerCmd.Apply<StrengthPower>(choiceContext, enemy, strengthAmount, Owner, null);
        }

        Flash();
    }
}

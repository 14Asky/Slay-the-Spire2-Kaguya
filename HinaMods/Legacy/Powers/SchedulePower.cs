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

public sealed class LegacySchedulePower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string CustomPackedIconPath => "res://images/packed/card_portraits/kaguya/SchedulePower.png";
    public override string CustomBigIconPath => "res://images/packed/card_portraits/kaguya/SchedulePower.png";

    // 使用新版回合开始钩子
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != Owner.Side) return;
        var player = Owner.Player;
        if (player == null) return;

        int level = (int)Amount;

        // 获得 level 点能量
        await PlayerCmd.GainEnergy(level, player);
        // 抽 level 张牌（需要 PlayerChoiceContext，此处使用占位）
        var choiceContext = new ThrowingPlayerChoiceContext();
        await CardPileCmd.Draw(choiceContext, level, player);

        // 失去1层过劳
        var overwork = Owner.GetPower<Overwork>();
        if (overwork != null)
        {
            int currentAmount = (int)overwork.Amount;
            if (currentAmount <= 1)
            {
                await PowerCmd.Remove(overwork);
            }
            else
            {
                await PowerCmd.Remove(overwork);
                await PowerCmd.Apply<Overwork>(choiceContext, Owner, currentAmount - 1, Owner, null);
            }
        }

        Flash();
    }
}

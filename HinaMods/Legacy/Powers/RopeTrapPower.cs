using BaseLib.Abstracts;
using Kaguya.CardPools;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Kaguya.Powers;

public sealed class LegacyRopeTrapPower : CustomPowerModel
{
    private CardModel _sourceCard; // 记录施加此能力的卡牌（即绳索陷阱本身）

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter; // 层数表示每张牌施加的 debuff 层数

    // 可选：自定义图标路径
    public override string CustomPackedIconPath => "res://images/packed/card_portraits/kaguya/RopeTrapPower.png";
    public override string CustomBigIconPath => "res://images/packed/card_portraits/kaguya/RopeTrapPower.png";

    // 能力被施加时记录来源卡牌
    public override async Task AfterApplied(Creature applier, CardModel cardSource)
    {
        _sourceCard = cardSource;
        await Task.CompletedTask;
    }

    // 每打出一张牌后触发
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 只对能力拥有者打出的牌生效
        if (cardPlay.Card.Owner.Creature != Owner) return;
        // 跳过施加此能力的卡牌本身（绳索陷阱）
        if (cardPlay.Card == _sourceCard) return;

        int debuffAmount = (int)Amount;

        // 使用 base.CombatState 获取当前战斗中的敌人
        foreach (var enemy in base.CombatState.HittableEnemies)
        {
            await PowerCmd.Apply<WeakPower>(choiceContext, enemy, debuffAmount, Owner, null);
            await PowerCmd.Apply<VulnerablePower>(choiceContext, enemy, debuffAmount, Owner, null);
        }

        Flash();
    }

    // 回合结束时移除自身
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side == Owner.Side)
        {
            await PowerCmd.Remove(this);
        }
    }
}

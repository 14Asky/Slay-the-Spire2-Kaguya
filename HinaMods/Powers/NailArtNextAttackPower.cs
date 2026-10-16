using BaseLib.Abstracts;
using Kaguya.CardPools;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using System.Threading.Tasks;

namespace Kaguya.Powers;

public sealed class NailArtNextAttackPower : CustomPowerModel
{
    private CardModel _sourceCard; // 记录来源卡牌（美甲本身）

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter; // 层数表示剩余可使用次数

    public override string CustomPackedIconPath => "res://images/powers/nail_art_next_attack.png";
    public override string CustomBigIconPath => "res://images/powers/nail_art_next_attack.png";

    public override async Task AfterApplied(Creature applier, CardModel cardSource)
    {
        _sourceCard = cardSource;
        await Task.CompletedTask;
    }

    // 修改费用（打出前）
    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card.Owner.Creature != Owner) return false;
        if (card == _sourceCard) return false;        // 跳过自身（避免无限循环）
        if (card.Type != CardType.Attack) return false;

        if (Amount <= 0) return false;

        modifiedCost = originalCost - 1;
        if (modifiedCost < 0) modifiedCost = 0;
        return true;
    }

    // 打出后减少层数（而不是移除整个能力）
    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature != Owner) return;
        if (cardPlay.Card == _sourceCard) return;
        if (cardPlay.Card.Type != CardType.Attack) return;

        // 减少一层，层数归零时能力自动移除
        await PowerCmd.Decrement(this);
    }
}
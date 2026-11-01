using BaseLib.Abstracts;
using Kaguya.CardPools;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.Powers;

public sealed class LegacyKurakawaHighSchoolPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter; // 可叠加

    public override string CustomPackedIconPath => "res://images/packed/card_portraits/kaguya/KurakawaHighSchoolPower.png";
    public override string CustomBigIconPath => "res://images/packed/card_portraits/kaguya/KurakawaHighSchoolPower.png";

    private class LegacyData
    {
        public int freeUsedThisTurn;   // 本回合已经免费打出的伙伴牌数量
    }

    protected override object InitInternalData() => new LegacyData();

    public override async Task AfterApplied(Creature applier, CardModel cardSource)
    {
        await Task.CompletedTask;
    }

    // 每回合开始时重置计数
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner.Player)
            GetInternalData<LegacyData>().freeUsedThisTurn = 0;
        await Task.CompletedTask;
    }

    // 修改能量消耗：前 N 张伙伴牌免费（N = 能力层数）
    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        if (card.Owner.Creature == Owner && card.Tags.Contains((CardTag)1004))
        {
            var data = GetInternalData<LegacyData>();
            if (data.freeUsedThisTurn < (int)Amount)
            {
                modifiedCost = 0;
                return true;
            }
        }
        modifiedCost = originalCost;
        return false;
    }

    // 打出伙伴牌后记录一次免费使用
    public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner == Owner.Player && cardPlay.Card.Tags.Contains((CardTag)1004))
        {
            GetInternalData<LegacyData>().freeUsedThisTurn++;
        }
        return Task.CompletedTask;
    }
}
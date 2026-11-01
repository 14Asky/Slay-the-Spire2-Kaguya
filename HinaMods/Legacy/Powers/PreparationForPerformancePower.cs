using BaseLib.Abstracts;
using Kaguya.CardPools;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using System.Threading.Tasks;

namespace Kaguya.Powers;

public sealed class LegacyPreparationForPerformancePower : CustomPowerModel
{
    private int _cardsPlayedThisCombat = 0; // 本场战斗已打出的牌数计数器

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single; // 不可叠加

    public override string CustomPackedIconPath => "res://images/packed/card_portraits/kaguya/PreparationForPerformancePower.png";
    public override string CustomBigIconPath => "res://images/packed/card_portraits/kaguya/PreparationForPerformancePower.png";

    // 战斗开始时重置计数器
    public override async Task BeforeCombatStart()
    {
        _cardsPlayedThisCombat = 0;
        await Task.CompletedTask;
    }

    // 每打出一张牌后触发
    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        // 只对能力拥有者打出的牌生效
        if (cardPlay.Card.Owner.Creature != Owner) return;

        _cardsPlayedThisCombat++;
        // 每打出2张牌，获得1层创作
        if (_cardsPlayedThisCombat % 2 == 0)
        {
            await PowerCmd.Apply<LegacyCreationPower>(context, Owner, 1, Owner, null);
            Flash();
        }
    }

    // 可选：能力被移除时无需特殊处理
    public override async Task AfterRemoved(Creature oldOwner)
    {
        await Task.CompletedTask;
    }
}
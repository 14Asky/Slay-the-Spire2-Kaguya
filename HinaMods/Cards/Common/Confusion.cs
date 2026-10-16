using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using Kaguya.HinaMods;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 迷茫（Confusion）
/// 本地化键：KAGUYA-CONFUSION.title / .description
/// 1费 · 技能牌 · 普通 · 目标：自身
/// 卡面：抽3张牌，并使抽到的牌耗能随机化。
/// 代码实测：Cards=3
/// 升级：Cards +2
/// </summary>
public sealed class Confusion : HinaModsCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(3)
    ];

    public Confusion()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int drawAmount = (int)DynamicVars.Cards.BaseValue;

        for (int i = 0; i < drawAmount; i++)
        {
            CardModel drawnCard = await CardPileCmd.Draw(choiceContext, Owner);
            if (drawnCard == null)
                continue;

            // 1:1 复刻官方 SneckoOil 的随机费用判定（排除 X 费牌）
            if (drawnCard.EnergyCost.CostsX)
                continue;
            if (drawnCard.EnergyCost.GetWithModifiers(CostModifiers.None) < 0)
                continue;

            drawnCard.EnergyCost.SetThisTurnOrUntilPlayed(NextEnergyCost());

            // 联机保护：NCard 是本地 UI 节点，只在本地客户端播放随机费用动画，
            // 否则会操作到别的客户端/别的玩家手上的节点。
            if (LocalContext.IsMe(Owner))
            {
                NCard.FindOnTable(drawnCard)?.PlayRandomizeCostAnim();
            }
        }
    }

    private int NextEnergyCost()
    {
        return Owner.RunState.Rng.CombatEnergyCosts.NextInt(4);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(2m);
    }
}

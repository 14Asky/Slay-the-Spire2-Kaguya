using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Nodes.Cards;
using Kaguya.HinaMods;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Cards;

[Pool(typeof(StatusCardPool))]
public sealed class LegacyConfusion : HinaModsCard
{
    // 完全参考 LegacySinger 状态牌
    public override int MaxUpgradeLevel => 0;
    public override List<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];
    public override bool HasTurnEndInHandEffect => false;
    protected override IEnumerable<string> ExtraRunAssetPaths => Enumerable.Empty<string>();

    // 完全参考 LegacyHinaModsMoonDraw 抽牌变量
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new CardsVar(2)
    };

    // 官方状态牌标准构造
    public LegacyConfusion()
        : base(-1, CardType.Status, CardRarity.Status, TargetType.None)
    { }

    // 完全参考 LegacyGameLife 抽到触发逻辑
    public override async Task AfterCardDrawn(PlayerChoiceContext ctx, CardModel card, bool fromHandDraw)
    {
        await base.AfterCardDrawn(ctx, card, fromHandDraw);
        if (card == this && Owner != null)
        {
            await Cmd.Wait(0.25f);

            // 1:1 复刻 LegacyHinaModsMoonDraw 抽牌
            int drawAmount = (int)base.DynamicVars.Cards.BaseValue;
            await CardPileCmd.Draw(ctx, drawAmount, Owner);

            // 1:1 复刻官方 SneckoOil 随机费用逻辑
            await ApplySneckoCostRandomize(ctx);
        }
    }

    // 🔥 完全移植官方 SneckoOil 核心逻辑，修复 RunState 报错
    private async Task ApplySneckoCostRandomize(PlayerChoiceContext choiceContext)
    {
        Player player = Owner;
        if (player == null) return;

        // 官方原版：筛选手牌，排除X费牌
        IEnumerable<CardModel> handCards = PileType.Hand.GetPile(player).Cards.Where(c => !c.EnergyCost.CostsX);
        foreach (CardModel item in handCards)
        {
            if (item.EnergyCost.GetWithModifiers(CostModifiers.None) >= 0)
            {
                // 官方原版：设置随机费用
                item.EnergyCost.SetThisTurnOrUntilPlayed(NextEnergyCost());
                NCard.FindOnTable(item)?.PlayRandomizeCostAnim();
            }
        }

        await Task.CompletedTask;
    }

    // 🔥 完全复刻官方 SneckoOil 随机数方法（修复RunState报错）
    private int NextEnergyCost()
    {
        // 官方原版写法：使用卡牌自身 Owner.RunState，无Creature调用
        return base.Owner.RunState.Rng.CombatEnergyCosts.NextInt(4);
    }

    // 无法打出，空实现
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Task.CompletedTask;
    }

    protected override void OnUpgrade() { }
}
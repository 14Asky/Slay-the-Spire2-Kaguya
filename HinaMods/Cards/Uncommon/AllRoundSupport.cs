using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using Kaguya.HinaMods;
using Kaguya.HinaMods.Cards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards.Uncommon;

/// <summary>
/// 八方援护（AllRoundSupport）
/// 本地化键：KAGUYA-RALLY_SUPPORT_CARD.title / .description
/// 3费 · 技能牌 · 罕见 · 目标：自身
/// 卡面：每打出一张[blue]支援[/blue]牌，此牌费用减少1。此牌打出后费用增加3。获得3层[gold]月夜[/gold]，随机将3张[gold]升级后[/gold]的[blue]支援[/blue]牌加入[gold]手牌[/gold]，并使其本回合可以免费打出。
/// 升级：无
/// </summary>
public sealed class AllRoundSupport : HinaModsCard
{
    /// <summary>每打出一张支援牌的永久减费。</summary>
    private const int CostPerSupport = -1;

    /// <summary>本牌打出后的永久加费。</summary>
    private const int CostAfterPlay = 3;

    public override HashSet<string> CustomTags => new HashSet<string>() { CustomCardTags.SUPPORTCARD };

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            HoverTipFactory.FromPower<MoonlightPower>(),
        };
    }

    // 构造函数：3费 技能 罕见 目标自身
    public AllRoundSupport()
        : base(3, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    /// <summary>
    /// 费用增减是【永久】的 —— 直接改本牌的基础费用。
    /// 战斗中的卡实例在全部牌堆（手牌/抽牌堆/弃牌堆/消耗堆/打出堆）都会收到战斗钩子，
    /// 且基础费用改动随实例跨回合保留，因此无需额外的能力牌。
    /// </summary>
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await base.AfterCardPlayed(choiceContext, cardPlay);

        CardModel played = cardPlay.Card;
        if (played == null || Owner == null || played.Owner == null
            || played.Owner.Creature != Owner.Creature)
        {
            return;
        }

        // 本牌打出后费用 +3
        if (played == this)
        {
            ChangeBaseCost(CostAfterPlay);
            return;
        }

        // 每打出一张支援牌，费用 -1
        if (played is HinaModsCard modCard
            && modCard.CustomTags?.Contains(CustomCardTags.SUPPORT) == true)
        {
            ChangeBaseCost(CostPerSupport);
        }
    }

    /// <summary>按增量修改基础费用（最低 0），并刷新卡面。</summary>
    private void ChangeBaseCost(int delta)
    {
        if (delta == 0)
        {
            return;
        }

        int current = EnergyCost.GetWithModifiers(CostModifiers.None);
        int target = Math.Max(0, current + delta);
        if (target == current)
        {
            return;
        }

        EnergyCost.SetCustomBaseCost(target);
        Owner?.PlayerCombatState?.RecalculateCardValues();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player player = Owner;
        if (player == null) return;

        // 获得 3 层月夜
        await PowerCmd.Apply<MoonlightPower>(
            choiceContext,
            player.Creature,
            3,
            player.Creature,
            this);

        // 随机将 3 张支援牌加入手牌
        List<CardModel> pool = GetOfficialSupportTokenPool();
        if (pool.Count == 0) return;

        List<CardModel> cards = CardFactory.GetDistinctForCombat(
            player,
            pool,
            3,
            player.RunState.Rng.CombatCardGeneration
        ).ToList();

        foreach (var card in cards)
        {
            // 升级后生成的支援牌为升级版
            if (IsUpgraded)
            {
                CardCmd.Upgrade(card);
            }

            await CardPileCmd.AddGeneratedCardToCombat(
                card,
                PileType.Hand,
                player,
                CardPilePosition.Top);

            // 本回合可以免费打出
            card.SetToFreeThisTurn();

            // 「获得+免费」标记：这类牌打出时不消耗月夜
            HinaModsCard.MarkFreePlay(card);
        }
    }

    // 从官方TokenCardPool筛选 支援标签代币卡
    private List<CardModel> GetOfficialSupportTokenPool()
    {
        TokenCardPool tokenPool = ModelDb.CardPool<TokenCardPool>();

        return tokenPool.AllCardIds
            .Select(id => ModelDb.GetById<CardModel>(id))
            .OfType<HinaModsCard>()
            .Where(c => c.CustomTags?.Contains(CustomCardTags.SUPPORT) == true)
            .Cast<CardModel>()
            .ToList();
    }
}

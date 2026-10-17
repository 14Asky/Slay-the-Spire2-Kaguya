using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
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
/// 本地化键：KAGUYA-ALL_ROUND_SUPPORT.title / .description
/// 3费 · 技能牌 · 罕见 · 目标：自身 · 标签：支援牌
/// 卡面：每打出一张[blue]支援[/blue]牌，此牌费用减少1。此牌打出后费用增加3。
///       获得2张随机[blue]支援[/blue]牌，其费用减少{SupportDiscount}。
/// 升级：获得的支援牌费用减少 1 → 2
/// </summary>
public sealed class AllRoundSupport : HinaModsCard
{
    /// <summary>每打出一张支援牌的永久减费。</summary>
    private const int CostPerSupport = -1;

    /// <summary>本牌打出后的永久加费。</summary>
    private const int CostAfterPlay = 3;

    public override HashSet<string> CustomTags => new HashSet<string>() { CustomCardTags.SUPPORTCARD };

    /// <summary>获得的支援牌的费用减免（基础 1 / 升级 2）。</summary>
    private const string SupportDiscountKey = "SupportDiscount";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar(SupportDiscountKey, 1m)];

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

        List<CardModel> pool = GetOfficialSupportTokenPool();
        if (pool.Count == 0) return;

        // 随机将 2 张支援牌加入手牌，其费用减少 SupportDiscount（基础 1 / 升级 2）
        int discount = (int)DynamicVars[SupportDiscountKey].BaseValue;

        List<CardModel> cards = CardFactory.GetDistinctForCombat(
            player,
            pool,
            2,
            player.RunState.Rng.CombatCardGeneration
        ).ToList();

        foreach (var card in cards)
        {
            await CardPileCmd.AddGeneratedCardToCombat(
                card,
                PileType.Hand,
                player,
                CardPilePosition.Top);

            if (discount > 0)
            {
                card.EnergyCost.AddUntilPlayed(-discount);
            }
        }
    }

    /// <summary>升级：获得的支援牌费用减免 1 → 2。</summary>
    protected override void OnUpgrade()
    {
        DynamicVars[SupportDiscountKey].UpgradeValueBy(1m);
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

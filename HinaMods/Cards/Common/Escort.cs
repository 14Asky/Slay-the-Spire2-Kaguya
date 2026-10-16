using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using Kaguya.HinaMods.Cards;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Kaguya.HinaMods;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 护卫（Escort）
/// 本地化键：KAGUYA-GENERATE_SUPPORT_CARD.title / .description
/// 0费 · 技能牌 · 普通 · 目标：自身 · 关键词：消耗 · 标签：支援牌
/// 卡面：获得随机2张[blue]支援[/blue]牌。
/// 升级：获得的 2 张支援牌为【升级后】的。
/// </summary>
public sealed class Escort : HinaModsCard
{
    public Escort()
        : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    { }

    public override HashSet<string> CustomTags => new HashSet<string>() { CustomCardTags.SUPPORTCARD };

    /// <summary>消耗。</summary>
    public override List<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player player = Owner;
        if (player == null) return;

        List<CardModel> supportCards = GetSupportCardsFromOfficialTokenPool();
        if (!supportCards.Any()) return;

        // 固定 2 张（升级只改变「是否已升级」，不改变张数）
        List<CardModel> result = CardFactory.GetDistinctForCombat(
            player,
            supportCards,
            2,
            player.RunState.Rng.CombatCardGeneration
        ).ToList();

        foreach (CardModel card in result)
        {
            // 升级版：获得的支援牌自带升级
            if (IsUpgraded)
            {
                TryUpgrade(card);
            }

            await CardPileCmd.AddGeneratedCardToCombat(
                card,
                PileType.Hand,
                player,
                CardPilePosition.Top
            );
        }
    }

    /// <summary>尝试升级一张牌（幂等：已升级 / 不可升级则跳过）。</summary>
    private static void TryUpgrade(CardModel card)
    {
        if (card == null || !card.IsMutable || !card.IsUpgradable || card.IsUpgraded)
            return;

        card.UpgradeInternal();
        card.FinalizeUpgradeInternal();
    }

    private List<CardModel> GetSupportCardsFromOfficialTokenPool()
    {
        TokenCardPool tokenPool = ModelDb.CardPool<TokenCardPool>();

        return tokenPool.AllCardIds
            .Select(id => ModelDb.GetById<CardModel>(id))
            .OfType<HinaModsCard>()
            .Where(c =>
                c.CustomTags?.Contains(CustomCardTags.SUPPORT) == true
                && c.Rarity == CardRarity.Token
            )
            .Cast<CardModel>()
            .ToList();
    }
}

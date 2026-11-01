using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using Kaguya.HinaMods;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// 固定命名空间
namespace Kaguya.HinaMods.Legacy.Cards;

/// <summary>
/// 支援奔涌
/// 效果：丢弃所有手牌，将等量的【支援】牌加入手牌
/// 升级效果：生成的所有支援牌直接升级
/// </summary>
public sealed class LegacySupportSurge : HinaModsCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    // 1费 技能 稀有 自身目标
    public LegacySupportSurge()
        : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    { }

    // 耗尽关键词（按你的模组习惯）
    public override List<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player player = Owner;
        if (player == null)
            return;

        // 严格复刻官方写法：获取手牌 + 丢弃所有手牌
        IEnumerable<CardModel> handCards = PileType.Hand.GetPile(player).Cards.ToList();
        int handCount = handCards.Count();
        await CardCmd.Discard(choiceContext, handCards);

        await Cmd.CustomScaledWait(0f, 0.25f);

        List<CardModel> supportPool = GetOfficialSupportTokenPool();
        if (!supportPool.Any())
            return;

        // 官方联机安全随机生成
        List<CardModel> generatedCards = CardFactory.GetDistinctForCombat(
            player,
            supportPool,
            handCount,
            player.RunState.Rng.CombatCardGeneration
        ).ToList();

        // 升级自动升级卡牌
        if (IsUpgraded)
        {
            foreach (CardModel card in generatedCards)
            {
                CardCmd.Upgrade(card, CardPreviewStyle.HorizontalLayout);
            }
        }

        // 修复：官方标准加入手牌 参数规范
        var addResults = await CardPileCmd.AddGeneratedCardsToCombat(
            generatedCards,
            PileType.Hand,
            player,
            CardPilePosition.Top
        );

        CardCmd.PreviewCardPileAdd(addResults);
    }

    // 官方支援牌池获取
    private List<CardModel> GetOfficialSupportTokenPool()
    {
        TokenCardPool tokenPool = ModelDb.CardPool<TokenCardPool>();
        return tokenPool.AllCardIds
            .Select(id => ModelDb.GetById<CardModel>(id))
            .OfType<HinaModsCard>()
            .Where(c => c.CustomTags?.Contains(LegacyCustomCardTags.SUPPORT) == true)
            .Cast<CardModel>()
            .ToList();
    }

    protected override void OnUpgrade()
    {
    }
}
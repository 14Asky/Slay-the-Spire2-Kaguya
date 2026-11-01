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
namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 你和我（YouAndMe）
/// 本地化键：KAGUYA-SUPPORT_SURGE.title / .description
/// 2费 · 技能牌 · 稀有 · 目标：自身 · 无关键词 · 标签：支援牌
/// 卡面：[gold]丢弃[/gold]所有[gold]手牌[/gold]，获得等量[blue]支援[/blue]牌，并使生成的[blue]支援[/blue]牌费用-1。
/// 代码实测：CostReduce=1
/// 升级：无
/// </summary>
public sealed class YouAndMe : HinaModsCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("CostReduce", 1m)
    ];

    // 2费 技能 稀有 自身目标
    public YouAndMe()
        : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
    { }

    /// <summary>支援牌标签（本牌会获得支援牌）。</summary>
    public override HashSet<string> CustomTags => new HashSet<string>() { CustomCardTags.SUPPORTCARD };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player player = Owner;
        if (player == null)
            return;

        // 获取手牌 + 丢弃所有手牌
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

        // 生成的支援牌费用 -N（N：基础 1 / 升级 2）
        int reduce = (int)DynamicVars["CostReduce"].BaseValue;
        foreach (CardModel card in generatedCards)
        {
            card.EnergyCost.AddUntilPlayed(-reduce);

            // 「获得+免费」标记：这类牌打出时不消耗月夜
            HinaModsCard.MarkFreePlay(card);
        }

        var addResults = await CardPileCmd.AddGeneratedCardsToCombat(
            generatedCards,
            PileType.Hand,
            player,
            CardPilePosition.Top
        );

        CardCmd.PreviewCardPileAdd(addResults);

        player.PlayerCombatState?.RecalculateCardValues();
    }

    // 官方支援牌池获取
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

    // 升级：用户描述未给升级效果
    /// <summary>升级：费用 -1（2 → 1），生成牌的费用减免额度不变。</summary>
    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}

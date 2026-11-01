using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using Kaguya.HinaMods;
using Kaguya.HinaMods.Legacy.Cards;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Cards;

/// <summary>
/// 支援集结
/// 1费 技能牌 | 自身目标
/// 将所有支援牌加入抽牌堆，本场战斗免费打出。消耗。
/// 升级：生成的所有支援牌直接升级。
/// </summary>
public sealed class LegacySupportAssemble : HinaModsCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    public LegacySupportAssemble()
        : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    { }

    public override List<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player player = Owner;
        if (player == null)
            return;

        List<CardModel> supportCardTemplates = GetUniqueSupportCards();
        if (!supportCardTemplates.Any())
            return;

        List<CardModel> generatedCards = new List<CardModel>();
        foreach (CardModel template in supportCardTemplates)
        {
            // 对齐参考代码的卡牌创建方式
            CardModel newCard = CombatState.CreateCard(template, player);
            generatedCards.Add(newCard);
            newCard.SetToFreeThisCombat();
        }

        if (IsUpgraded)
        {
            foreach (CardModel card in generatedCards)
            {
                CardCmd.Upgrade(card, CardPreviewStyle.HorizontalLayout);
            }
        }

        // 随机加入抽牌堆 + 参考代码标准参数
        var addResults = await CardPileCmd.AddGeneratedCardsToCombat(
            generatedCards,
            PileType.Draw,
            player,
            CardPilePosition.Random // 保持随机
        );
        CardCmd.PreviewCardPileAdd(addResults);
    }

    private List<CardModel> GetUniqueSupportCards()
    {
        TokenCardPool tokenPool = ModelDb.CardPool<TokenCardPool>();

        return tokenPool.AllCardIds
            .Select(id => ModelDb.GetById<CardModel>(id))
            .OfType<HinaModsCard>()
            .Where(c => c.CustomTags?.Contains(LegacyCustomCardTags.SUPPORT) == true)
            .GroupBy(c => c.Id)
            .Select(g => g.First())
            .Cast<CardModel>()
            .ToList();
    }

    protected override void OnUpgrade()
    {
    }
}
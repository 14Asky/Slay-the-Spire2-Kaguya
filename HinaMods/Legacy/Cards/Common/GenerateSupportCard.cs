using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using Kaguya.HinaMods.Legacy.Cards;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Kaguya.HinaMods;

namespace Kaguya.HinaMods.Legacy.Cards;

public sealed class LegacyGenerateSupportCard : HinaModsCard
{
    public LegacyGenerateSupportCard()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    { }
    public override HashSet<string> CustomTags => new HashSet<string>() { LegacyCustomCardTags.SUPPORTCARD };
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player player = Owner;
        if (player == null) return;

        List<CardModel> supportCards = GetSupportCardsFromOfficialTokenPool();
        if (!supportCards.Any()) return;

        List<CardModel> result = CardFactory.GetDistinctForCombat(
            player,
            supportCards,
            1,
            player.RunState.Rng.CombatCardGeneration
        ).ToList();

        if (IsUpgraded)
        {
            CardCmd.Upgrade(result, CardPreviewStyle.HorizontalLayout);
        }

        foreach (CardModel card in result)
        {
            await CardPileCmd.AddGeneratedCardToCombat(
                card,
                PileType.Hand,
                player,
                CardPilePosition.Top
            );
        }
    }

    private List<CardModel> GetSupportCardsFromOfficialTokenPool()
    {
        TokenCardPool tokenPool = ModelDb.CardPool<TokenCardPool>();

        return tokenPool.AllCardIds
            .Select(id => ModelDb.GetById<CardModel>(id))
            .OfType<HinaModsCard>()
            .Where(c =>
                c.CustomTags?.Contains(LegacyCustomCardTags.SUPPORT) == true
                && c.Rarity == CardRarity.Token
            )
            .Cast<CardModel>()
            .ToList();
    }
}
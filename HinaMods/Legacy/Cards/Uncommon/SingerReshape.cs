using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using Kaguya.HinaMods;
using Kaguya.HinaMods.Character;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Cards;

public sealed class LegacySingerReshape : HinaModsCard
{
    public override HashSet<string> CustomTags => new HashSet<string>() { LegacyCustomCardTags.SINGER };

    // 核心：变量名 SingerCount 完全保留
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new IntVar("SingerCount", 1)
    };

    public LegacySingerReshape()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player player = Owner;
        if (player == null)
            return;

        // 读取动态变量 SingerCount
        int selectCount = (int)DynamicVars["SingerCount"].BaseValue;

        CardSelectorPrefs prefs = new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, selectCount);

        IEnumerable<CardModel> selectedCards = await CardSelectCmd.FromHand(
            choiceContext,
            player,
            prefs,
            null,
            this
        );

        if (!selectedCards.Any())
            return;

        List<CardModel> singerCardPool = GetSingerCardsFromCharacterPool(player);
        if (!singerCardPool.Any())
            return;

        foreach (CardModel oldCard in selectedCards)
        {
            CardModel newCard = CardFactory.GetDistinctForCombat(
                player,
                singerCardPool,
                1,
                player.RunState.Rng.CombatCardGeneration
            ).FirstOrDefault();

            if (newCard == null)
                continue;

            await CardPileCmd.RemoveFromCombat(oldCard);

            // 🔥 修复：官方标准参数格式（联机稳定）
            await CardPileCmd.AddGeneratedCardToCombat(
                newCard,
                PileType.Hand,
                player,
                CardPilePosition.Top);

            newCard.SetToFreeThisTurn();
        }
    }

    private List<CardModel> GetSingerCardsFromCharacterPool(Player player)
    {
        HinaModsCardPool characterPool = player.Character.CardPool as HinaModsCardPool;
        if (characterPool == null)
            return new List<CardModel>();

        IEnumerable<CardModel> allCards = characterPool.GetUnlockedCards(
            player.UnlockState,
            player.RunState.CardMultiplayerConstraint
        );

        return allCards
            .OfType<HinaModsCard>()
            .Where(c => c.CustomTags?.Contains(LegacyCustomCardTags.SINGER) == true)
            .Cast<CardModel>()
            .ToList();
    }

    // 升级：SingerCount +1（1→2）
    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["SingerCount"].UpgradeValueBy(1);
    }
}
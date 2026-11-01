using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using Kaguya.HinaMods;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 世界第一公主殿下（WorldsNumberOnePrincess）
/// 本地化键：KAGUYA-SINGER_ECHO_COPY.title / .description
/// 1费 · 技能牌 · 稀有 · 目标：自身 · 关键词：Exhaust
/// 卡面：从[gold]消耗堆[/gold]中选择1张[purple]歌声[/purple]牌，将其的2张复制品加入[gold]手牌[/gold]。
/// 升级：费用 -1
/// </summary>
public sealed class WorldsNumberOnePrincess : HinaModsCard
{
    public WorldsNumberOnePrincess()
        : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    { }

    public override List<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player player = Owner;
        if (player == null) return;

        await CreatureCmd.TriggerAnim(player.Creature, "Cast", player.Character.CastAnimDelay);

        CardPile exhaustPile = PileType.Exhaust.GetPile(player);
        List<CardModel> singerCards = exhaustPile.Cards
            .OfType<HinaModsCard>()
            .Where(c => c.HasEcho)
            .Cast<CardModel>()
            .ToList();

        if (!singerCards.Any())
            return;

        CardSelectorPrefs prefs = new CardSelectorPrefs(base.SelectionScreenPrompt, 1);

        IEnumerable<CardModel> selectedCards = await CardSelectCmd.FromSimpleGrid(
            context: choiceContext,
            cardsIn: singerCards,
            player: player,
            prefs: prefs
        );

        CardModel targetCard = selectedCards.FirstOrDefault();
        if (targetCard == null)
            return;

        for (int i = 0; i < 2; i++)
        {
            CardModel clone = targetCard.CreateClone();
            await CardPileCmd.AddGeneratedCardToCombat(
                clone,
                PileType.Hand,
                player,
                CardPilePosition.Top
            );
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}

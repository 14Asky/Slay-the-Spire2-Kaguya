using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Kaguya.HinaMods;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 歌者专属：等待
/// 效果：选择{Cards:diff()}张消耗堆中的【歌者】牌，加入手牌并获得保留
/// </summary>
public sealed class SingerWaitBlock : HinaModsCard
{
    // 1费 技能 稀有
    public SingerWaitBlock()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    { }

    // 歌者标签
    public override HashSet<string> CustomTags => new HashSet<string>() { CustomCardTags.SINGER };
    public override List<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal];

    //固定选择1张，无需数值
    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player player = Owner;
        if (player == null) return;

        // 官方动画
        await CreatureCmd.TriggerAnim(player.Creature, "Cast", player.Character.CastAnimDelay);

        // 筛选消耗堆中的歌者牌
        CardPile exhaustPile = PileType.Exhaust.GetPile(player);
        List<CardModel> singerCards = exhaustPile.Cards
            .OfType<HinaModsCard>()
            .Where(c => c.CustomTags?.Contains(CustomCardTags.SINGER) == true)
            .Cast<CardModel>()
            .ToList();

        if (!singerCards.Any())
            return;

        //固定选择 1 张
        CardSelectorPrefs prefs = new CardSelectorPrefs(SelectionScreenPrompt, 1, 1);

        IEnumerable<CardModel> selectedCards = await CardSelectCmd.FromSimpleGrid(
            context: choiceContext,
            cardsIn: singerCards,
            player: player,
            prefs: prefs
        );

        // 打出选中的卡牌
        foreach (CardModel card in selectedCards)
        {
            // 从消耗堆打出卡牌
            await CardCmd.AutoPlay(choiceContext, card, null);
        }
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Ethereal);
    }
}
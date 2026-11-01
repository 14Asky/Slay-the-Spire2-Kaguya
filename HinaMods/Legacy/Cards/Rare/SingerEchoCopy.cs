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

namespace Kaguya.HinaMods.Legacy.Cards;

/// <summary>
/// 回响复现
/// 1费 技能牌 | 自身目标
/// 从消耗堆选择1张【歌者】标签卡牌，复制2张加入手牌。消耗。
/// 升级：保留。
/// </summary>
public sealed class LegacySingerEchoCopy : HinaModsCard
{
    public LegacySingerEchoCopy()
        : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    { }
    public override List<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public override HashSet<string> CustomTags => new HashSet<string>() { LegacyCustomCardTags.SINGERCARD };
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player player = Owner;
        if (player == null) return;

        await CreatureCmd.TriggerAnim(player.Creature, "Cast", player.Character.CastAnimDelay);

        CardPile exhaustPile = PileType.Exhaust.GetPile(player);
        List<CardModel> singerCards = exhaustPile.Cards
            .OfType<HinaModsCard>()
            .Where(c => c.CustomTags?.Contains(LegacyCustomCardTags.SINGER) == true)
            .Cast<CardModel>()
            .ToList();

        if (!singerCards.Any())
            return;

        // 🔥 仅修改此处，其余代码100%不变
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
            // 修复：替换为正确的参数
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
        AddKeyword(CardKeyword.Retain);
    }
}
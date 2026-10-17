using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Powers;
using Kaguya.HinaMods.SupportCards.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 八千代分身（YachiyoClone）
/// 本地化键：KAGUYA-CONCERTED_PREPARATION.title / .description
/// 0费 · 技能牌 · 稀有 · 目标：自身 · 关键词：Exhaust
/// 卡面：获得1张[blue]月见[/blue]。
///       抽2张牌。
///       获得2层[gold]月夜[/gold]。
/// 代码实测：Cards=1；MoonlightPower=2
/// 升级：新增关键词 Innate
/// </summary>
public sealed class YachiyoClone : HinaModsCard
{

    /// <summary>支援牌标签（本牌会获得支援牌）。</summary>
    public override HashSet<string> CustomTags => new HashSet<string>() { CustomCardTags.SUPPORTCARD };

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(2),
        new PowerVar<MoonlightPower>(2m)
    ];

    public YachiyoClone()
        : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    { }

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            HoverTipFactory.FromCard<MoonView>(),
            HoverTipFactory.FromPower<MoonlightPower>()
        };
    }
    public override List<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int tokenCount = (int)DynamicVars.Cards.BaseValue;
        List<CardModel> tokens = new List<CardModel>();
        for (int i = 0; i < tokenCount; i++)
        {
            tokens.Add(CardScope.CreateCard<MoonView>(Owner));
        }

        // 加入抽牌堆（带过渡动画）
        CardCmd.PreviewCardPileAdd(
            await CardPileCmd.AddGeneratedCardsToCombat(tokens, PileType.Draw, Owner));

        await CardPileCmd.Draw(choiceContext, 2, Owner);

        await PowerCmd.Apply<MoonlightPower>(
            choiceContext,
            base.Owner.Creature,
            base.DynamicVars[nameof(MoonlightPower)].BaseValue,
            base.Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
    }
}

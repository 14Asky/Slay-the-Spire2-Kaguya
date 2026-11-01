using Godot;
using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Kaguya.HinaMods.Cards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 新的开始（NewBeginning）
/// 本地化键：KAGUYA-TSUKIMI_CHOICE.title / .description
/// 0费 · 技能牌 · 稀有 · 目标：自身
/// 卡面：[gold]消耗[/gold]1张牌。
///       [gold]消耗[/gold]攻击牌获得3层[gold]力量[/gold]，
///       [gold]消耗[/gold]技能牌抽3张牌，
///       [gold]消耗[/gold]能力牌时打出该牌。
/// 升级：无
/// </summary>
public sealed class NewBeginning : HinaModsCard
{
    public override List<CardKeyword> CanonicalKeywords => new List<CardKeyword> { CardKeyword.Exhaust };

    public NewBeginning()
        : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }
    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            HoverTipFactory.FromPower<StrengthPower>()
        };
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null) return;

        // 官方标准 手牌选择器
        var selectedCards = await CardSelectCmd.FromHand(
            choiceContext,
            Owner,
            new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 1),
            null,
            this
        );

        CardModel selectedCard = selectedCards.FirstOrDefault();
        if (selectedCard == null) return;

        // 消耗选中卡牌
        await CardCmd.Exhaust(choiceContext, selectedCard);

        // 核心效果分支
        switch (selectedCard.Type)
        {
            case CardType.Attack:
                int strength = IsUpgraded ? 3 : 2;
                // 修复：补全 PowerCmd.Apply 必需的 choiceContext 参数
                await PowerCmd.Apply<StrengthPower>(
                    choiceContext,
                    Owner.Creature,
                    strength,
                    Owner.Creature,
                    this);
                break;

            case CardType.Skill:
                int draw = IsUpgraded ? 3 : 2;
                await CardPileCmd.Draw(choiceContext, draw, Owner);
                break;

            case CardType.Power:
                await CardCmd.AutoPlay(choiceContext, selectedCard, null);
                break;
        }
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
    }
}
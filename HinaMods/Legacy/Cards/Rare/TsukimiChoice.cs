using Godot;
using Kaguya.HinaMods.Legacy.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Kaguya.HinaMods.Legacy.Cards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Cards;

/// <summary>
/// 月见抉择
/// 0费 技能牌 | 消耗
/// 选择一张手牌并将其消耗。
/// 消耗攻击牌：获得2点力量（升级：3点）
/// 消耗技能牌：抽2张牌（升级：3张）
/// 消耗能力牌：自动打出该能力牌
/// </summary>
public sealed class LegacyTsukimiChoice : HinaModsCard
{
    public override List<CardKeyword> CanonicalKeywords => new List<CardKeyword> { CardKeyword.Exhaust };

    public LegacyTsukimiChoice()
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
using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Legacy.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using Kaguya.HinaMods;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Cards;

/// <summary>
/// 支援·月夜召还 | 月夜体系支援选牌
/// 0费技能牌 | 消耗2层月夜 | 自选支援牌
/// </summary>
public class LegacySupportMoonlightCall() : HinaModsCard(0,
    CardType.Skill, CardRarity.Uncommon,
    TargetType.Self)
{
    // 动态变量：完全对标你的月夜消耗卡牌
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<LegacyFortunePower>(2m) // 基础消耗2层月夜
    ];
    public override HashSet<string> CustomTags => new HashSet<string>() { LegacyCustomCardTags.SUPPORTCARD };
    protected override bool IsPlayable
    {
        get
        {
            if (!base.IsPlayable)
                return false;

            LegacyFortunePower fortunePower = base.Owner.Creature.GetPower<LegacyFortunePower>();
            return fortunePower != null && fortunePower.Amount >= 2;
        }
    }

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            HoverTipFactory.FromPower<LegacyFortunePower>()
        };
    }

    // 打出逻辑：月夜消耗 + 选牌（仅修改选牌框，其余完全不变）
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 仅第一次打出触发消耗
        if (cardPlay.IsFirstInSeries)
        {
            LegacyFortunePower fortunePower = Owner.Creature.GetPower<LegacyFortunePower>();
            decimal requiredMoon = DynamicVars["LegacyFortunePower"].BaseValue;

            if (fortunePower != null && fortunePower.Amount >= requiredMoon)
            {
                // 消耗2层月夜
                await PowerCmd.ModifyAmount(
                    choiceContext,
                    fortunePower,
                    -requiredMoon,
                    Owner.Creature,
                    this);

                List<HinaModsCard> allSupportCards = ModelDb.AllCards
                    .OfType<HinaModsCard>() // 只看你的模组卡牌
                    .Where(c =>
                        c.CustomTags != null
                        && c.CustomTags.Any()
                        && c.CustomTags.Contains(LegacyCustomCardTags.SUPPORT))
                    .ToList();

                // 随机3张不重复支援卡（无修改）
                List<CardModel> selectedCards = CardFactory
                    .GetDistinctForCombat(Owner, allSupportCards.Cast<CardModel>().ToList(), 3, Owner.RunState.Rng.CombatCardGeneration)
                    .ToList();

                if (IsUpgraded)
                {
                    CardCmd.Upgrade(selectedCards, CardPreviewStyle.HorizontalLayout);
                }

                CardModel chosenCard = await CardSelectCmd.FromChooseACardScreen(choiceContext, selectedCards, base.Owner, canSkip: true);

                // 加入手牌（无修改）
                if (chosenCard != null)
                {
                    await CardPileCmd.AddGeneratedCardToCombat(
                        chosenCard,
                        PileType.Hand,
                        Owner,
                        CardPilePosition.Top);
                }
            }
        }
    }

    protected override void OnUpgrade()
    {
    }
}
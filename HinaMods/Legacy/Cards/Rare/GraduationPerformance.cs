using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Legacy.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Cards;

/// <summary>
/// 毕业演出
/// 0费 技能牌 | 自身目标
/// 消耗所有月夜层数，移除所有歌者牌，抽牌至手牌满（10张）。消耗。
/// 升级：保留。
/// </summary>
public class LegacyGraduationPerformance() : HinaModsCard(0,
    CardType.Skill, CardRarity.Rare,
    TargetType.Self)
{
    public override List<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            HoverTipFactory.FromPower<LegacyFortunePower>()
        };
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 修复：使用 ModifyAmount 将月夜层数减至 0
        int moonStacks = Owner.Creature.GetPowerAmount<LegacyFortunePower>();
        if (moonStacks > 0)
        {
            var moonPower = Owner.Creature.GetPower<LegacyFortunePower>();
            if (moonPower != null)
            {
                await PowerCmd.ModifyAmount(
                    choiceContext,
                    moonPower,
                    -moonStacks,
                    Owner.Creature,
                    this);
            }
        }

        List<CardModel> singerCards = Owner.PlayerCombatState.AllCards
            .OfType<HinaModsCard>()
            .Where(c => c.CustomTags?.Contains(LegacyCustomCardTags.SINGER) == true)
            .Cast<CardModel>()
            .ToList();

        foreach (CardModel card in singerCards)
        {
            await CardPileCmd.RemoveFromCombat(card);
        }

        int drawCount = 10 - Owner.PlayerCombatState.Hand.Cards.Count;
        await CardPileCmd.Draw(choiceContext, drawCount, Owner);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}
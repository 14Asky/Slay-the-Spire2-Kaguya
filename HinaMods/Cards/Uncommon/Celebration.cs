using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Kaguya.HinaMods;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 庆祝（Celebration）
/// 本地化键：KAGUYA-SUPPORT_BURST.title / .description
/// 1费 · 技能牌 · 罕见 · 目标：自身 · 标签：支援牌（无关键词）
/// 卡面：随机获得2张[blue]支援[/blue]牌，[gold]手牌[/gold]中每有一种不同费用就获得1点能量。
/// 代码实测：CardCount=2
/// 升级：CardCount +1（2 → 3）
/// </summary>
public sealed class Celebration : HinaModsCard
{
    public override HashSet<string> CustomTags => new HashSet<string>() { CustomCardTags.SUPPORTCARD };

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("CardCount", 2m)
    ];

    public Celebration()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        int count = (int)DynamicVars["CardCount"].BaseValue;

        List<CardModel> supportPool = ModelDb.AllCards
            .OfType<HinaModsCard>()
            .Where(c => c.CustomTags != null && c.CustomTags.Contains(CustomCardTags.SUPPORT))
            .Where(c => HinaModsCard.AllowedInRun(c, Owner))
            .Cast<CardModel>()
            .ToList();

        if (supportPool.Count == 0)
            return;

        List<CardModel> cards = CardFactory.GetDistinctForCombat(
            Owner,
            supportPool,
            count,
            Owner.RunState.Rng.CombatCardGeneration).ToList();

        if (cards.Count == 0)
            return;

        foreach (CardModel card in cards)
        {
            // 升级后：获得的支援牌为升级版
            if (IsUpgraded && card.IsMutable && card.IsUpgradable && !card.IsUpgraded)
            {
                card.UpgradeInternal();
                card.FinalizeUpgradeInternal();
            }

            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner, CardPilePosition.Top);
        }

        // 手牌中每有一种不同费用 → 获得 1 点能量
        int distinctCosts = PileType.Hand.GetPile(Owner).Cards
            .Select(c => c.EnergyCost.GetWithModifiers(CostModifiers.None))
            .Distinct()
            .Count();

        if (distinctCosts > 0)
        {
            await PlayerCmd.GainEnergy(distinctCosts, Owner);
        }
    }

    /// <summary>升级：获得的支援牌变为升级版（张数仍为 2）。</summary>
    protected override void OnUpgrade()
    {
    }
}

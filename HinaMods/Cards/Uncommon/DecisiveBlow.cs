using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Kaguya.HinaMods;
using Kaguya.HinaMods.Cards;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.SupportCards.Common;

/// <summary>
/// 关键一击（DecisiveBlow）
/// 本地化键：KAGUYA-DECISIVE_BLOW.title / .description
/// 2费 · 攻击牌 · 稀有 · 目标：单个敌人 · 标签：支援牌
/// 卡面：造成15点伤害。获得随机3张0费[blue]支援[/blue]牌。
/// 代码实测：Damage=15；SupportCount=3
/// 升级：伤害与张数不变；获得的支援牌变为【升级后】的
/// </summary>
public sealed class DecisiveBlow : HinaModsCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(15m, ValueProp.Move),
        new DynamicVar("SupportCount", 3m)
    ];

    /// <summary>支援牌标签（本牌会获得支援牌）。</summary>
    public override HashSet<string> CustomTags => new HashSet<string>() { CustomCardTags.SUPPORTCARD };

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        // 「支援」是卡牌标签，不是某一张具体牌（旧版挂的「支援打击」会误导成只有它算数），故改用标签提示。
        return new IHoverTip[]
        {
            new HoverTip(
                new LocString("card_keywords", "SUPPORT.title"),
                new LocString("card_keywords", "SUPPORT.description"))
        };
    }

    public DecisiveBlow()
        : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target is not Creature target || target.IsDead)
            return;

        // 造成伤害（无条件获得支援牌）
        await CreatureCmd.Damage(
            choiceContext: choiceContext,
            targets: new List<Creature> { target },
            amount: DynamicVars.Damage.BaseValue,
            props: ValueProp.Move,
            cardSource: this,
            dealer: Owner.Creature,
            cardPlay: null);

        int count = (int)DynamicVars["SupportCount"].BaseValue;
        if (count <= 0)
            return;

        List<CardModel> supportPool = ModelDb.AllCards
            .OfType<HinaModsCard>()
            .Where(c => c.CustomTags != null && c.CustomTags.Contains(CustomCardTags.SUPPORT))
            .Cast<CardModel>()
            .ToList();

        if (supportPool.Count == 0)
            return;

        List<CardModel> cards = CardFactory.GetDistinctForCombat(
            Owner,
            supportPool,
            count,
            Owner.RunState.Rng.CombatCardGeneration).ToList();

        foreach (CardModel card in cards)
        {
            // 升级后：获得的是升级版支援牌
            if (IsUpgraded && card.IsMutable && card.IsUpgradable && !card.IsUpgraded)
            {
                card.UpgradeInternal();
                card.FinalizeUpgradeInternal();
            }

            // 0 费支援牌
            card.EnergyCost.SetCustomBaseCost(0);
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner, CardPilePosition.Top);
        }

        Owner.PlayerCombatState?.RecalculateCardValues();
    }

    /// <summary>升级：获得的 3 张支援牌变为升级版（伤害与张数不变）。</summary>
    protected override void OnUpgrade()
    {
    }
}

using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
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
/// 本地化键：KAGUYA-HINA_MODS_WIN_STRIKE.title / .description
/// 2费 · 攻击牌 · 稀有 · 目标：单个敌人 · 标签：支援牌
/// 卡面：造成12点伤害。获得随机2张0费[blue]支援[/blue]牌。
/// 代码实测：Damage=12；SupportCount=2
/// 升级：Damage +3（12 → 15）；SupportCount +1（2 → 3）
/// </summary>
public sealed class DecisiveBlow : HinaModsCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(12m, ValueProp.Move),
        new DynamicVar("SupportCount", 2m)
    ];

    /// <summary>支援牌标签（本牌会获得支援牌）。</summary>
    public override HashSet<string> CustomTags => new HashSet<string>() { CustomCardTags.SUPPORTCARD };

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
           HoverTipFactory.FromCard<SupportStrike>(),
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
            // 0 费支援牌
            card.EnergyCost.SetCustomBaseCost(0);
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner, CardPilePosition.Top);
        }

        Owner.PlayerCombatState?.RecalculateCardValues();
    }

    /// <summary>升级：伤害 12 → 15；支援牌 2 → 3 张。</summary>
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
        DynamicVars["SupportCount"].UpgradeValueBy(1m);
    }
}

using BaseLib.Abstracts;
using BaseLib.Utils;
using Kaguya.CardPools;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace Kaguya.Cards;

[Pool(typeof(EventCardPool))]
public class ElementalArrow : CustomCardModel
{
    private const int energyCost = 0;
    private const CardType type = CardType.Attack;
    private const CardRarity rarity = CardRarity.Uncommon;
    private const TargetType targetType = TargetType.AnyEnemy;
    private const bool shouldShowInCardLibrary = true;

    private const int baseDamage = 6;
    private const int weakAmount = 1;
    private const int vulnerableAmount = 1;
    private const int poisonAmount = 7;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(baseDamage, ValueProp.Move)
    };

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new[]
    {
        HoverTipFactory.FromKeyword(ElementKeyword.Element),
        HoverTipFactory.FromPower<WeakPower>(),
        HoverTipFactory.FromPower<VulnerablePower>(),
        HoverTipFactory.FromPower<PoisonPower>()
    };

    public ElementalArrow() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary) { }

    private CardModel GetPreviousPlayedCard()
    {
        if (CombatState == null) return null;
        return CombatManager.Instance.History.CardPlaysStarted
            .LastOrDefault(e => e.CardPlay.Card.Owner == Owner && e.CardPlay.Card != this)
            ?.CardPlay.Card;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target;
        if (target == null) return;

        int damage = (int)DynamicVars["Damage"].BaseValue;
        await DamageCmd.Attack(damage)
            .FromCard(this, cardPlay)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash", null, "blunt_attack.mp3")
            .Execute(choiceContext);

        var lastCard = GetPreviousPlayedCard();
        if (lastCard != null)
        {
            switch (lastCard.Type)
            {
                case CardType.Attack:
                    await PowerCmd.Apply<WeakPower>(choiceContext, target, weakAmount, Owner.Creature, this);
                    break;
                case CardType.Skill:
                    await PowerCmd.Apply<VulnerablePower>(choiceContext, target, vulnerableAmount, Owner.Creature, this);
                    break;
                default:
                    await PowerCmd.Apply<PoisonPower>(choiceContext, target, poisonAmount, Owner.Creature, this);
                    break;
            }
        }

        // 抽1张牌
        await CardPileCmd.Draw(choiceContext, 1, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Damage"].UpgradeValueBy(3);
    }

    public override string PortraitPath => $"res://images/packed/card_portraits/kaguya/{nameof(ElementalArrow)}.png";
}

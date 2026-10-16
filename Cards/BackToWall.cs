using BaseLib.Abstracts;
using BaseLib.Utils;
using Kaguya.CardPools;
using Kaguya.Monsters;
using Kaguya.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.Cards;

[Pool(typeof(EventCardPool))]
public class BackToWall : CustomCardModel
{
    private const int energyCost = 1;
    private const CardType type = CardType.Power;
    private const CardRarity rarity = CardRarity.Ancient;
    private const TargetType targetType = TargetType.Self;
    private const bool shouldShowInCardLibrary = true;

    public BackToWall() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => System.Array.Empty<DynamicVar>();
    protected override IEnumerable<IHoverTip> ExtraHoverTips => new[]
    {
        HoverTipFactory.FromPower<HigherDimensionalBeingPower>()
    };

    private bool IsTsukitoBossPresent()
    {
        return CombatState.ContainsMonster<TsukitoBag>()
            || CombatState.ContainsMonster<TsukitoBosatsu>()
            || CombatState.ContainsMonster<TsukitoKongo>()
            || CombatState.ContainsMonster<TsukitoTenchu>()
            || CombatState.ContainsMonster<TsukitoZuisho>();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 检测场上是否存在月人Boss → 给予封号补救
        if (IsTsukitoBossPresent())
        {
            await PowerCmd.Apply<AccountRemedyPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
        }

        // 清除所有敌人身上的所有高维生物
        foreach (var enemy in CombatState.HittableEnemies)
        {
            var enemyHighDim = enemy.Powers.OfType<HigherDimensionalBeingPower>().ToList();
            foreach (var power in enemyHighDim)
            {
                await PowerCmd.Remove(power);
            }
        }

        // 给自身添加 99 层高维生物
        await PowerCmd.Apply<HigherDimensionalBeingPower>(choiceContext, Owner.Creature, 99, Owner.Creature, this);

        // 施加限制解除 + 2回合后死亡能力
        await PowerCmd.Apply<LimitReleasePower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }

    public override string PortraitPath => $"res://images/packed/card_portraits/kaguya/{nameof(BackToWall)}.png";
}

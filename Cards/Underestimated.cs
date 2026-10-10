using BaseLib.Abstracts;
using BaseLib.Utils;
using Kaguya.CardPools;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.Cards;

[Pool(typeof(KaguyaCardPool))]
public class Underestimated : CustomCardModel
{
    private const int energyCost = 1;
    private const CardType type = CardType.Attack;
    private const CardRarity rarity = CardRarity.Uncommon;
    private const TargetType targetType = TargetType.AnyEnemy;
    private const bool shouldShowInCardLibrary = true;

    private const int baseDamage = 8;
    private const int baseStrength = 3;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(baseDamage, ValueProp.Move),
        new PowerVar<StrengthPower>(baseStrength)
    };

    // 高亮提示（当敌人处于非攻击意图时）
    protected override bool ShouldGlowGoldInternal
    {
        get
        {
            if (!base.ShouldGlowGoldInternal) return false;
            if (CombatState == null) return false;
            foreach (var enemy in CombatState.HittableEnemies)
            {
                if (enemy.Monster != null && !enemy.Monster.IntendsToAttack)
                    return true;
            }
            return false;
        }
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new[]
    {
        HoverTipFactory.FromPower<StrengthPower>()
    };

    public Underestimated() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target;

        // 先获得力量（若敌人处于非攻击意图）
        if (target != null && target.Monster != null && !target.Monster.IntendsToAttack)
        {
            int strengthAmount = (int)DynamicVars["StrengthPower"].BaseValue;
            await PowerCmd.Apply<StrengthPower>(choiceContext, Owner.Creature, strengthAmount, Owner.Creature, this);
        }

        // 再造成伤害
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_blunt", null, "blunt_attack.mp3")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        // 升级：力量增加1（2 → 3）
        DynamicVars["StrengthPower"].UpgradeValueBy(1);
    }

    public override string PortraitPath => $"res://images/packed/card_portraits/kaguya/{nameof(Underestimated)}.png";
}
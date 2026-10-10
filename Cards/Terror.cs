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
public class Terror : CustomCardModel
{
    private const int energyCost = 1;
    private const CardType type = CardType.Attack;
    private const CardRarity rarity = CardRarity.Uncommon;
    private const TargetType targetType = TargetType.AnyEnemy;
    private const bool shouldShowInCardLibrary = true;

    private const int baseDamage = 5;
    private const int vulnerableAmount = 10;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(baseDamage, ValueProp.Move),
        new PowerVar<VulnerablePower>(vulnerableAmount)
    };

    // 高亮提示（当敌人处于非攻击意图时）
    protected override bool ShouldGlowGoldInternal
    {
        get
        {
            if (!base.ShouldGlowGoldInternal) return false;
            // 简单处理：只要存在非攻击意图的敌人就高亮（可根据当前选中目标优化）
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
        HoverTipFactory.FromPower<VulnerablePower>()
    };

    public Terror() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target;

        // 造成8点伤害
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_blunt", null, "blunt_attack.mp3")
            .Execute(choiceContext);

        // 若敌人处于非攻击意图，给予10层易伤
        if (target != null && target.Monster != null && !target.Monster.IntendsToAttack)
        {
            await PowerCmd.Apply<VulnerablePower>(choiceContext, target, 10, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        // 升级：费用减少1（1 → 0）
        EnergyCost.UpgradeBy(-1);
    }

    public override string PortraitPath => $"res://images/packed/card_portraits/kaguya/{nameof(Terror)}.png";
}
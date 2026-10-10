using BaseLib.Abstracts;
using BaseLib.Utils;
using Kaguya.CardPools;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.Cards;

[Pool(typeof(KaguyaCardPool))]
public class Bankruptcy : CustomCardModel
{
    private const int energyCost = 2;
    private const CardType type = CardType.Attack;
    private const CardRarity rarity = CardRarity.Rare;
    private const TargetType targetType = TargetType.AllEnemies;
    private const bool shouldShowInCardLibrary = true;

    private const int baseDamage = 2;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(baseDamage, ValueProp.Move)
    };

    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

    public Bankruptcy() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary) { }

    // 辅助方法：检查是否还有存活的敌人
    private bool HasAliveEnemies()
    {
        

        // 2. 尝试直接获取敌人列表属性（根据实际 API 调整）
         if (CombatState.Enemies != null && CombatState.Enemies.Any(e => !e.IsDead))
             return true;

        return false;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int damagePerHit = (int)DynamicVars.Damage.BaseValue;

        // 循环：金币足够且还有存活的敌人
        while (Owner.Gold >= 10 && HasAliveEnemies())
        {
            await PlayerCmd.LoseGold(10, Owner);

            await DamageCmd.Attack(damagePerHit)
                .FromCard(this, cardPlay)
                .TargetingAllOpponents(CombatState)
                .WithHitFx("vfx/vfx_attack_blunt", null, "blunt_attack.mp3")
                .Execute(choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1);
    }

    public override string PortraitPath => $"res://images/packed/card_portraits/kaguya/{nameof(Bankruptcy)}.png";
}
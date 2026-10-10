using BaseLib.Abstracts;
using BaseLib.Utils;
using Kaguya.CardPools;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace Kaguya.Cards;

[Pool(typeof(KaguyaCardPool))]
public class DareYouUpload : CustomCardModel
{
    private const int energyCost = 1;
    private const CardType type = CardType.Attack;
    private const CardRarity rarity = CardRarity.Uncommon;
    private const TargetType targetType = TargetType.AnyEnemy;
    private const bool shouldShowInCardLibrary = true;

    private const int baseDamage = 10;
    private const int baseDebuff = 1;      // 易伤和虚弱的层数

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(baseDamage, ValueProp.Move),
        new DynamicVar("DebuffAmount", baseDebuff)  // 共用层数
    };

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new[]
    {
        HoverTipFactory.FromPower<WeakPower>(),
        HoverTipFactory.FromPower<VulnerablePower>()
    };

    public DareYouUpload() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target;

        // 造成伤害
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 获取层数
        int amount = (int)DynamicVars["DebuffAmount"].BaseValue;

        // 施加虚弱和易伤（补充 choiceContext 参数）
        await PowerCmd.Apply<WeakPower>(choiceContext, target, amount, Owner.Creature, this);
        await PowerCmd.Apply<VulnerablePower>(choiceContext, target, amount, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        // 升级：伤害 +3
        DynamicVars.Damage.UpgradeValueBy(3);
        // 注意：debuff层数升级不变，可按需调整
    }

    public override string PortraitPath => $"res://images/packed/card_portraits/kaguya/{nameof(DareYouUpload)}.png";
}

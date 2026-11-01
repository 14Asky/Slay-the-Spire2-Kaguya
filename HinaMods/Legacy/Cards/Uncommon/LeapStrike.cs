using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Legacy.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Kaguya.HinaMods;
using Kaguya.HinaMods.Character;
using Kaguya.HinaMods.Legacy.Powers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Cards;

public class LegacyLeapStrike() : HinaModsCard(1,
    CardType.Attack,
    CardRarity.Uncommon,
    TargetType.AnyEnemy)
{
    // 对标模板：动态变量 伤害+敏捷
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8m, ValueProp.Move),
        new PowerVar<DexterityPower>(1m)
    ];

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            HoverTipFactory.FromPower<DexterityPower>(),
            HoverTipFactory.FromPower<LegacyFortunePower>()
        };
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null) return;

        // 基础单体伤害
        await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);

        // ==============================================
        // 【修改点】判断：自身拥有 月夜(LegacyFortunePower) 时触发
        // ==============================================
        bool hasMoonFortune = Owner.Creature.HasPower<LegacyFortunePower>();

        if (hasMoonFortune)
        {
            // 给自己施加敏捷（官方联机安全API）
            await PowerCmd.Apply<DexterityPower>(
                choiceContext,
                Owner.Creature,
                DynamicVars["DexterityPower"].BaseValue,
                Owner.Creature,
                this
            );
        }
    }

    // 升级逻辑（模板同款格式）
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m);
        DynamicVars["DexterityPower"].UpgradeValueBy(1m);
    }
}

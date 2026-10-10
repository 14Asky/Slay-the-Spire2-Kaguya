using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Kaguya.HinaMods;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 月蚀打击
/// 2费 攻击牌 | 单体目标
/// 造成13点伤害。若为第一次打出，消耗1层月夜，对目标施加1层易伤。
/// 升级：伤害变为18点，易伤变为2层。
/// </summary>
public class HinaModsMoonEclipseStrike() : HinaModsCard(2,
    CardType.Attack, CardRarity.Common,
    TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(13m, ValueProp.Move),
        new PowerVar<VulnerablePower>(1m),
        new PowerVar<FortunePower>(1m)
    ];
    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            HoverTipFactory.FromPower<VulnerablePower>(),
            HoverTipFactory.FromPower<FortunePower>()
        };
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Attack", 0.2f);
        await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);

        if (cardPlay.IsFirstInSeries)
        {
            FortunePower fortunePower = Owner.Creature.GetPower<FortunePower>();
            decimal requiredMoon = DynamicVars["FortunePower"].BaseValue;
            decimal vulnAmount = DynamicVars.Vulnerable.BaseValue;

            if (fortunePower != null && fortunePower.Amount >= requiredMoon)
            {
                // 修复：添加 choiceContext 参数
                await PowerCmd.ModifyAmount(
                    choiceContext,
                    fortunePower,
                    -requiredMoon,
                    Owner.Creature,
                    this);

                // 修复：添加 choiceContext 参数
                await PowerCmd.Apply<VulnerablePower>(
                    choiceContext,
                    cardPlay.Target,
                    vulnAmount,
                    Owner.Creature,
                    this
                );
            }
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(5m);
        DynamicVars.Vulnerable.UpgradeValueBy(1m);
    }
}

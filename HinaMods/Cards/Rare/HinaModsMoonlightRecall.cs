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
using Kaguya.HinaMods.Cards;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards.Rare;

/// <summary>
/// 月华回闪
/// 2费 攻击牌 | 单体目标
/// 造成15点伤害，对目标施加1层易伤。
/// 若消耗5层月夜，将此牌返回手牌，本回合费用-1。
/// 升级：伤害变为20点，易伤变为2层。
/// </summary>
public class HinaModsMoonlightRecall : HinaModsCard
{
    public HinaModsMoonlightRecall() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }

    private bool _hasTriggeredMoonlightDiscount;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(15m, ValueProp.Move),
        new PowerVar<VulnerablePower>(1m),
        new PowerVar<FortunePower>(5m)
    ];

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
           HoverTipFactory.FromPower<VulnerablePower>(),
           HoverTipFactory.FromPower<FortunePower>(),
        };
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);

        if (cardPlay.IsFirstInSeries)
        {
            FortunePower fortunePower = Owner.Creature.GetPower<FortunePower>();
            decimal requiredMoon = DynamicVars["FortunePower"].BaseValue;
            decimal vulnAmount = DynamicVars.Vulnerable.BaseValue;

            // 修复：添加 choiceContext 参数
            await PowerCmd.Apply<VulnerablePower>(
                choiceContext,
                cardPlay.Target,
                vulnAmount,
                Owner.Creature,
                this);

            if (fortunePower != null && fortunePower.Amount >= requiredMoon)
            {
                // 修复：添加 choiceContext 参数
                await PowerCmd.ModifyAmount(
                    choiceContext,
                    fortunePower,
                    -requiredMoon,
                    Owner.Creature,
                    this);
                await CardPileCmd.Add(this, PileType.Hand);
                _hasTriggeredMoonlightDiscount = true;
            }
        }
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card != this || !_hasTriggeredMoonlightDiscount)
        {
            return;
        }

        base.EnergyCost.AddThisTurn(-1);
        _hasTriggeredMoonlightDiscount = false;

        await base.AfterCardPlayed(context, cardPlay);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(5m);
        DynamicVars.Vulnerable.UpgradeValueBy(1m);
    }
}

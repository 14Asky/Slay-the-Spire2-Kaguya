using Kaguya.HinaMods.Legacy.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Kaguya.HinaMods.Legacy.Powers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Cards;

/// <summary>
/// 月华流转
/// 0费 攻击牌 | 单体目标
/// 造成4点伤害，获得1层月夜。
/// 每累计消耗4层月夜，将此牌返回手牌。
/// 升级：伤害变为6点，获得的月夜变为2层。
/// </summary>
public sealed class LegacyMoonlightFlow : HinaModsCard
{
    protected override DynamicVar[] CanonicalVars => new[]
    {
        new DamageVar(4m, ValueProp.Move),
        new DynamicVar("MoonlightGain", 1m),
        new DynamicVar("MoonlightCost", 4m)
    };

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            HoverTipFactory.FromPower<LegacyFortunePower>()
        };
    }

    private int _resolvedMoonlight;

    public LegacyMoonlightFlow()
        : base(0, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null || Owner == null) return;

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_starry_impact")
            .Execute(choiceContext);

        // 修复：添加 choiceContext 参数
        await PowerCmd.Apply<LegacyFortunePower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["MoonlightGain"].BaseValue,
            Owner.Creature,
            this);
    }

    public override async Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner || Pile.Type == PileType.Hand)
            return;

        int totalConsumed = CombatManager.Instance.History.Entries
            .OfType<PowerReceivedEntry>()
            .Where(e =>
                e.Power is LegacyFortunePower &&
                e.Actor == Owner.Creature &&
                e.Amount < 0)
            .Sum(e => Math.Abs((int)e.Amount));

        int unResolved = totalConsumed - _resolvedMoonlight;
        int requireCost = DynamicVars["MoonlightCost"].IntValue;

        while (unResolved >= requireCost)
        {
            await CardPileCmd.Add(this, PileType.Hand);
            _resolvedMoonlight += requireCost;
            unResolved -= requireCost;
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars["MoonlightGain"].UpgradeValueBy(1m);
    }
}
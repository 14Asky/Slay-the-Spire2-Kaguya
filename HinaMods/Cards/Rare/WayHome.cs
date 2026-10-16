using Kaguya.HinaMods.Powers;
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
using Kaguya.HinaMods.Powers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 归途（WayHome）
/// 本地化键：KAGUYA-MOONLIGHT_FLOW.title / .description
/// 0费 · 攻击牌 · 稀有 · 目标：单个敌人
/// 卡面：造成5点伤害。
///       获得1层[gold]月夜[/gold]。
///       每当你[gold]消耗[/gold]4层[gold]月夜[/gold]时，将此牌放回[gold]手牌[/gold]。
/// 代码实测：Damage=5；MoonlightGain=1；MoonlightCost=4
/// 升级：MoonlightGain +1；MoonlightCost -1；Damage +1
/// </summary>
public sealed class WayHome : HinaModsCard
{
    protected override DynamicVar[] CanonicalVars => new[]
    {
        new DamageVar(5m, ValueProp.Move),
        new DynamicVar("MoonlightGain", 1m),
        new DynamicVar("MoonlightCost", 4m)
    };

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            HoverTipFactory.FromPower<MoonlightPower>()
        };
    }

    private int _resolvedMoonlight;

    public WayHome()
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

        await PowerCmd.Apply<MoonlightPower>(
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
                e.Power is MoonlightPower &&
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
        DynamicVars.Damage.UpgradeValueBy(1m);              // 5 → 6
        DynamicVars["MoonlightGain"].UpgradeValueBy(1m);    // 1 → 2
        DynamicVars["MoonlightCost"].UpgradeValueBy(-1m);   // 4 → 3
    }
}

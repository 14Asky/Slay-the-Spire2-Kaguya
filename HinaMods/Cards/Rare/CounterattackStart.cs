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
/// 反击开始（CounterattackStart）
/// 本地化键：KAGUYA-HINA_MODS_MOONLIGHT_RECALL.title / .description
/// 2费 · 攻击牌 · 稀有 · 目标：单个敌人
/// 卡面：造成12点伤害，给予1层[gold]易伤[/gold]和1层[gold]虚弱[/gold]。
///       [gold]月夜5[/gold]：将此牌返回[gold]手牌[/gold]，其本回合费用减少1。
/// 代码实测：Damage=12；VulnerablePower=1；WeakPower=1；MoonCost=5
/// 升级：虚弱 +1；Damage +3；Vulnerable +1
/// </summary>
public class CounterattackStart : HinaModsCard
{
    public CounterattackStart() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }

    private bool _returnedThisPlay;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(12m, ValueProp.Move),
        new PowerVar<VulnerablePower>(1m),
        new PowerVar<WeakPower>(1m),
        new PowerVar<MoonlightPower>(MoonCostVarName, 5m)
    ];

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
           HoverTipFactory.FromPower<VulnerablePower>(),
           HoverTipFactory.FromPower<WeakPower>(),
           HoverTipFactory.FromPower<MoonlightPower>(),
        };
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);

        if (!cardPlay.IsFirstInSeries)
            return;

        decimal vulnAmount = DynamicVars.Vulnerable.BaseValue;
        decimal weakAmount = DynamicVars[nameof(WeakPower)].BaseValue;

        await PowerCmd.Apply<VulnerablePower>(
            choiceContext,
            cardPlay.Target,
            vulnAmount,
            Owner.Creature,
            this);

        await PowerCmd.Apply<WeakPower>(
            choiceContext,
            cardPlay.Target,
            weakAmount,
            Owner.Creature,
            this);

        // 月夜5：消耗 5 层月夜 → 返回手牌 + 本回合费用 -1
        decimal requiredMoon = DynamicVars[MoonCostVarName].BaseValue;
        if (!await TryConsumeMoonlight(choiceContext, requiredMoon, this))
            return;

        await CardPileCmd.Add(this, PileType.Hand);
        _returnedThisPlay = true;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card == this && _returnedThisPlay)
        {
            EnergyCost.AddThisTurn(-1);
            _returnedThisPlay = false;
        }

        await base.AfterCardPlayed(context, cardPlay);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
        DynamicVars.Vulnerable.UpgradeValueBy(1m);
        DynamicVars[nameof(WeakPower)].UpgradeValueBy(1m);
    }
}

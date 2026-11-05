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
/// 卡面：造成12点伤害，给予1层[gold]易伤[/gold]。
///       [gold]月夜5[/gold]：将此牌返回[gold]手牌[/gold]，其本回合费用减少1。
/// 代码实测：Damage=12；VulnerablePower=1；MoonCost=5
/// 升级：Damage +2（12→14）；MoonCost -2（5→3）；易伤层数不变
/// </summary>
public class CounterattackStart : HinaModsCard
{
    public CounterattackStart() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }

    private bool _returnedThisPlay;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(12m, ValueProp.Move),
        new PowerVar<VulnerablePower>(1m),
        new PowerVar<MoonlightPower>(MoonCostVarName, 5m)
    ];

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
           HoverTipFactory.FromPower<VulnerablePower>(),
           HoverTipFactory.FromPower<MoonlightPower>(),
        };
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);

        if (!cardPlay.IsFirstInSeries)
            return;

        await PowerCmd.Apply<VulnerablePower>(
            choiceContext,
            cardPlay.Target,
            DynamicVars.Vulnerable.BaseValue,
            Owner.Creature,
            this);

        // 月夜5：消耗 5 层月夜 → 返回手牌 + 本回合费用 -1
        //
        // ⚠ 这里**不用** EffectiveMoonCost / SkipsMoonlightCost，直接取变量值：
        //    本牌的月夜是「效果代价」，不是「打出门槛」，所以无论从哪个渠道获得
        //    （癫狂之触 / 欧洛巴斯之酸 / 液态记忆 / 攻击药水 …）都必须照扣。
        //    历史坑：本牌每次返回都 AddThisTurn(-1)，费用累积到 0 后，旧的
        //    SkipsMoonlightCost（含 CostModifiers.Local == 0 分支）会把 0 费误判成免费，
        //    导致月夜不再扣除、牌无限返回手牌。
        //    写死取值后，即使基类规则再变，这张牌也不会退化成无限循环。
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

    /// <summary>升级：伤害 12 → 14；月夜门槛 5 → 3（易伤层数不变）。</summary>
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars[MoonCostVarName].UpgradeValueBy(-2m);
    }
}

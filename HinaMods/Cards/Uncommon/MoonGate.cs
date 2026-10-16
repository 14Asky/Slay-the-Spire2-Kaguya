using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards.Uncommon;

/// <summary>
/// 月扉（MoonGate，旧显示名「增援」）
/// 本地化键：KAGUYA-HEAVENLY_RAIMENT_CARD.title / .description
/// 1费 · 攻击牌 · 罕见 · 目标：全体敌人 · 无关键词
/// 卡面：对敌方全体造成{CalculatedDamage:diff()}点伤害，共{HitCount:diff()}次。
///       （伤害 = 本回合内获得[gold]月夜[/gold]的层数，实时变化）
/// 代码实测：CalculationBase=0；ExtraDamage=1；HitCount=1
/// 升级：HitCount +1（1→2）
/// </summary>
public sealed class MoonGate : HinaModsCard
{
    private const string HitCountKey = "HitCount";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CalculationBaseVar(0m),
        new ExtraDamageVar(1m),
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier(MoonGainedThisTurn),
        new DynamicVar(HitCountKey, 1m)
    ];

    /// <summary>
    /// 本回合内「获得月夜」的层数总和（每次获得月夜后卡面数值都会实时变化）。
    /// 数据源：战斗历史里 MoonlightPower 的获得记录（只算自己、只算本回合、只算正数）。
    /// </summary>
    private static decimal MoonGainedThisTurn(CardModel card, Creature _)
    {
        if (!CombatManager.Instance.IsInProgress || card.CombatState == null)
            return 0m;

        return CombatManager.Instance.History.Entries
            .OfType<PowerReceivedEntry>()
            .Where(e => e.HappenedThisTurn(card.CombatState)
                        && e.Power is MoonlightPower
                        && e.Actor == card.Owner.Creature
                        && e.Amount > 0m)
            .Sum(e => e.Amount);
    }

    public MoonGate()
        : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
    { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        await DamageCmd.Attack(DynamicVars.CalculatedDamage)
            .WithHitCount(DynamicVars[HitCountKey].IntValue)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(CombatState)
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars[HitCountKey].UpgradeValueBy(1m);
    }
}

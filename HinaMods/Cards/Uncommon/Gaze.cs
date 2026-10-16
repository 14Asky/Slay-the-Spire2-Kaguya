using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 注视（Gaze）
/// 本地化键：KAGUYA-SINGER_MOON_STRIKE.title / .description
/// 1费 · 攻击牌 · 罕见 · 目标：全体敌人 · 无关键词
/// 卡面：对敌方全体造成{Damage:diff()}点伤害。本回合内，你每造成一次伤害就获得{GazePower:diff()}层[gold]月夜[/gold]。
/// 代码实测：Damage=8；GazePower=1
/// 升级：GazePower +1（1→2）
/// </summary>
public sealed class Gaze : HinaModsCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8m, ValueProp.Move),
        new PowerVar<GazePower>(1m)
    ];

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            HoverTipFactory.FromPower<GazePower>(),
            HoverTipFactory.FromPower<MoonlightPower>()
        };
    }

    public Gaze()
        : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        // 先挂本回合的「注视」：本牌自己的这段伤害也算在内
        await PowerCmd.Apply<GazePower>(
            choiceContext,
            Owner.Creature,
            DynamicVars[nameof(GazePower)].BaseValue,
            Owner.Creature,
            this);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(CombatState)
            .Execute(choiceContext);
    }

    // 升级：每造成一次伤害获得的月夜 1 → 2
    protected override void OnUpgrade()
    {
        DynamicVars[nameof(GazePower)].UpgradeValueBy(1m);
    }
}

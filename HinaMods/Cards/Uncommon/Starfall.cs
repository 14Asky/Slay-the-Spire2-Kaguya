using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 星落（Starfall）
/// 本地化键：KAGUYA-TSUKIMI_FRENZY.title / .description
/// 1费 · 攻击牌 · 罕见 · 目标：全体敌人 · 无关键词
/// 卡面：[gold]消耗[/gold]所有[gold]月夜[/gold]，对所有敌人造成{Damage:diff()}点伤害；
///       你每有{MoonPerHit:diff()}层[gold]月夜[/gold]额外造成1次伤害。
/// 代码实测：Damage=5；MoonPerHit=2
/// 升级：MoonPerHit -1（2→1）
/// </summary>
public class Starfall() : HinaModsCard(1,
    CardType.Attack, CardRarity.Uncommon,
    TargetType.AllEnemies)
{
    private const string MoonPerHitKey = "MoonPerHit";

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            HoverTipFactory.FromPower<MoonlightPower>()
        };
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(5m, ValueProp.Move),
        new DynamicVar(MoonPerHitKey, 2m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int moonStacks = Owner.Creature.GetPowerAmount<MoonlightPower>();
        int perHit = DynamicVars[MoonPerHitKey].IntValue;
        int totalHits = perHit > 0 ? 1 + moonStacks / perHit : 1;

        // 先消耗所有月夜（被「获得+免费」途径取得的牌不消耗月夜）
        MoonlightPower moonPower = Owner.Creature.GetPower<MoonlightPower>();
        if (!SkipsMoonlightCost && moonPower != null && moonStacks > 0)
        {
            await PowerCmd.ModifyAmount(
                choiceContext,
                moonPower,
                -moonStacks,
                Owner.Creature,
                this);
        }

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(totalHits)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_attack_slash", null, SfxHitName)   // 每段命中都响声（引擎默认按段播，别用 OnlyPlayAnimOnce 关掉）
            .SpawningHitVfxOnEachCreature()
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars[MoonPerHitKey].UpgradeValueBy(-1m);
    }
}

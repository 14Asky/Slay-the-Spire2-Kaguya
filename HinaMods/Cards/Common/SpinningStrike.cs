
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Kaguya.HinaMods;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 旋转打击（SpinningStrike）
/// 本地化键：KAGUYA-SINGER_ALL_STRIKE.title / .description
/// 1费 · 攻击牌 · 普通 · 目标：全体敌人 · 关键词：歌声
/// 卡面：对所有敌人造成4点伤害2次。
/// 代码实测：Damage=4；HitCount=2
/// 升级：Damage +1
/// </summary>
public sealed class SpinningStrike : HinaModsCard
{
    /// <summary>歌声关键词（本牌自带）。</summary>
    public override HashSet<string> CustomTags => new HashSet<string>() { CustomCardTags.ECHO };

    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(4m, ValueProp.Move),
        new DynamicVar("HitCount", 2m)
    ];

    public SpinningStrike()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int hitCount = base.DynamicVars["HitCount"].IntValue;

        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
            .WithHitCount(hitCount)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(base.CombatState)
            .Execute(choiceContext);
    }

    // 升级：伤害 4 → 5
    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(1m);
    }
}

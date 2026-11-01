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

namespace Kaguya.HinaMods.Legacy.Cards;

// 歌者全体攻击牌：1费 对全体敌人造成多段伤害
public class LegacySingerAllStrike() : HinaModsCard(1,
    CardType.Attack, CardRarity.Common,
    TargetType.AllEnemies)
{
    // 歌者自定义标签
    public override HashSet<string> CustomTags => new HashSet<string>() { LegacyCustomCardTags.SINGER };

    // 打击标签
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];

    // 动态变量：伤害4点 + 攻击段数(基础2)
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(4m, ValueProp.Move),
        new DynamicVar("HitCount", 2m) // 官方标准：段数用动态变量存储
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 读取动态变量段数（自动适配升级）
        int hitCount = (int)base.DynamicVars["HitCount"].BaseValue;

        // 官方多段伤害写法
        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
            .WithHitCount(hitCount)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(base.CombatState)
            .Execute(choiceContext);
    }

    // 升级：段数 2→3
    protected override void OnUpgrade()
    {
        base.DynamicVars["HitCount"].UpgradeValueBy(1m);
    }
}
using BaseLib.Utils;
using Godot;
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
using Kaguya.HinaMods;
using Kaguya.HinaMods.Cards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards.Uncommon;

/// <summary>
/// 歌者连击
/// 1费 攻击 稀有
/// 对全体敌人造成6点伤害
/// 本场战斗中，此牌每打出1次，此牌额外命中1次
/// 升级：伤害+2（6→8）
/// </summary>
public class SingerComb : HinaModsCard
{
    // 完全复刻官方：私有常量键名
    private const string _calculatedHitsKey = "CalculatedHits";

    // 歌者专属标签
    public override HashSet<string> CustomTags => new HashSet<string>() { CustomCardTags.SINGER };


    // 100%复刻官方动态变量 + 修复静态委托/可空类型（无报错）
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        // 基础伤害：5点
        new DamageVar(5m, ValueProp.Move),
        // 基础命中次数：0次（官方原版）
        new CalculationBaseVar(0m),
        // 每次额外命中：1次（官方原版）
        new CalculationExtraVar(1m),
        // 静态委托 + 可空类型，彻底解决static报错，逻辑仅统计自身打出次
        new CalculatedVar(_calculatedHitsKey).WithMultiplier(static (CardModel card, Creature _) =>
            CombatManager.Instance.History.Entries
                .OfType<CardPlayFinishedEntry>()
                .Count(e => e.CardPlay.Card.Owner == card.Owner && e.CardPlay.Card.Id == card.Id)
        )
    };

    // 构造函数：改为 全体敌人目标（AllEnemies），其余和官方一致
    public SingerComb()
        : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
    { }

    // 官方原版逻辑 + 适配全体攻击（删除空目标校验，修改攻击方式）
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 全体攻击无单目标，删除官方单目标的空校验（核心修复）
        int hitCount = (int)((CalculatedVar)base.DynamicVars[_calculatedHitsKey]).Calculate(Owner.Creature);

        // 官方原版攻击逻辑 → 改为全体攻击（对齐你的SingerComboStrike）
        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
            .WithHitCount(hitCount)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(base.CombatState)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    // 官方原版升级逻辑（伤害+2）
    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(2m);
    }
}
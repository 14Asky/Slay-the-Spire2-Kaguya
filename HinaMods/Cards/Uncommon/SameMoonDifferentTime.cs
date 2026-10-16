using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Kaguya.HinaMods;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 同月殊时（SameMoonDifferentTime）
/// 本地化键：KAGUYA-MOONLIGHT_OVERFLOW.title / .description
/// 2费 · 攻击牌 · 稀有 · 目标：全体敌人 · 关键词：Exhaust
/// 卡面：对敌方全体造成12点伤害；
///       使你当前的[gold]月夜[/gold]层数翻倍。
/// 代码实测：Damage=12
/// 升级：Damage +3
/// </summary>
public sealed class SameMoonDifferentTime : HinaModsCard
{
    // 【消耗】关键词
    public override List<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    // 基础数值：12点伤害
    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new DamageVar(12m, ValueProp.Move)
    };

    // 月夜BUFF悬浮提示
    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
           HoverTipFactory.FromPower<MoonlightPower>(),
        };
    }

    // 构造函数：2费 攻击 稀有 全体
    public SameMoonDifferentTime()
        : base(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 对敌方全体造成伤害
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 使当前月夜层数翻倍：额外获得与当前层数等量的月夜
        int moonlightAmount = Owner.Creature.GetPowerAmount<MoonlightPower>();
        if (moonlightAmount > 0)
        {
            await PowerCmd.Apply<MoonlightPower>(
                choiceContext,
                Owner.Creature,
                moonlightAmount,
                Owner.Creature,
                this);
        }
    }

    // 升级：伤害 12 → 15
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}

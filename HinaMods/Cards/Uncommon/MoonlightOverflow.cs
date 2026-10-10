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

// 严格对标 MoltenFist 官方结构
public sealed class MoonlightOverflow : HinaModsCard
{
    // 【消耗】关键词 官方标准写法
    public override List<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    // 自定义月夜标签
    

    // 基础数值：13点伤害
    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new DamageVar(13m, ValueProp.Move)
    };

    // 月夜BUFF悬浮提示
    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
           HoverTipFactory.FromPower<FortunePower>(),
        };
    }


    // 构造函数：2费 攻击 普通 单体
    public MoonlightOverflow()
        : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    // 核心打出逻辑（完全复刻MoltenFist结构）
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        // 官方标准伤害调用
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 核心逻辑：获取月夜层数，>8则翻倍
        int moonlightAmount = Owner.Creature.GetPowerAmount<FortunePower>();
        if (moonlightAmount > 8)
        {
            // 🔥 唯一修复：补全官方强制参数 choiceContext
            await PowerCmd.Apply<FortunePower>(
                choiceContext,
                Owner.Creature,
                moonlightAmount,
                Owner.Creature,
                this);
        }
    }

    // 升级逻辑：13→16点伤害
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
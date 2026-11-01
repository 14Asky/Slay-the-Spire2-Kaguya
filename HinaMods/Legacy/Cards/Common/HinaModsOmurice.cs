using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Legacy.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Kaguya.HinaMods;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Cards;

public sealed class LegacyHinaModsOmurice : HinaModsCard
{
    // 关键词：消耗（官方标准写法）
    public override List<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    // 月夜标签
    

    // 动态变量：仅回血（格挡为动态月夜层数，无需变量）
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new HealVar(2m)
    };

    // 月夜悬浮提示
    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
           HoverTipFactory.FromPower<LegacyFortunePower>(),
        };
    }

    // 构造函数：1费 技能 白卡 目标自身
    public LegacyHinaModsOmurice()
        : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 获取当前月夜层数（无则为0）
        LegacyFortunePower moonPower = Owner.Creature.GetPower<LegacyFortunePower>();
        decimal blockAmount = moonPower?.Amount ?? 0m;

        // ===================== 核心修复 =====================
        // 使用官方正确重载 GainBlock(生物, 格挡数值, 属性, cardPlay)
        // 彻底解决参数转换错误！
        await CreatureCmd.GainBlock(Owner.Creature, blockAmount, ValueProp.Move, cardPlay);

        // 2. 回复生命值（参考模板原版写法）
        await CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.IntValue);
    }

    // 升级效果：回血+2（3→5），严格按你要求实现
    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars.Heal.UpgradeValueBy(2m);
    }
}
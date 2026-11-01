using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Kaguya.HinaMods.Cards;
using System.Collections.Generic;
using System.Threading.Tasks;

// 🔥 仅修改此处命名空间，其余代码完全不变
namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 过往（Past）
/// 本地化键：KAGUYA-MOONLIGHT_STRIKE_CARD.title / .description
/// 0费 · 攻击牌 · 稀有 · 目标：单个敌人
/// 卡面：造成你的[gold]月夜[/gold]层数的伤害X+1次。
/// 升级：无
/// </summary>
public sealed class Past : HinaModsCard
{
    // 官方标准：X费卡牌
    protected override bool HasEnergyCostX => true;

    // 官方标准构造函数
    public Past()
        : base(0, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }
    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
           HoverTipFactory.FromPower<MoonlightPower>(),
        };
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player player = Owner;
        if (player == null) return;

        // 官方标准：获取X费用数值（攻击次数）
        int xValue = ResolveEnergyXValue();
        // 升级：攻击次数+1
        if (IsUpgraded)
            xValue++;

        // 获取月夜层数 = 单次伤害值
        int moonStacks = (int)(player.Creature.GetPower<MoonlightPower>()?.Amount ?? 0);

        // 🔥 1:1 参考 DanceSteps 官方攻击写法
        AttackCommand attack = DamageCmd.Attack(moonStacks)
            .WithHitCount(xValue) // 核心：攻击次数 = X值
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target) // 官方原生单体目标
            .WithHitFx("vfx/vfx_attack_slash", null, SfxHitName); // 统一攻击特效 + 每段命中音

        await attack.Execute(choiceContext);
    }

    // 升级逻辑
    protected override void OnUpgrade()
    {
        base.OnUpgrade();
    }
}
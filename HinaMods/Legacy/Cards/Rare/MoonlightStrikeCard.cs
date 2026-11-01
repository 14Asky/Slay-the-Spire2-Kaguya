using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Legacy.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Kaguya.HinaMods.Legacy.Cards;
using System.Collections.Generic;
using System.Threading.Tasks;

// 🔥 仅修改此处命名空间，其余代码完全不变
namespace Kaguya.HinaMods.Legacy.Cards;

public sealed class LegacyMoonlightStrikeCard : HinaModsCard
{
    // 官方标准：X费卡牌
    protected override bool HasEnergyCostX => true;

    // 官方标准构造函数
    public LegacyMoonlightStrikeCard()
        : base(0, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }
    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
           HoverTipFactory.FromPower<LegacyFortunePower>(),
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
        int moonStacks = (int)(player.Creature.GetPower<LegacyFortunePower>()?.Amount ?? 0);

        // 🔥 1:1 参考 LegacySingerFugue 官方攻击写法
        AttackCommand attack = DamageCmd.Attack(moonStacks)
            .WithHitCount(xValue) // 核心：攻击次数 = X值
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target) // 官方原生单体目标
            .WithHitFx("vfx/vfx_attack_slash"); // 统一攻击特效

        await attack.Execute(choiceContext);
    }

    // 升级逻辑
    protected override void OnUpgrade()
    {
        base.OnUpgrade();
    }
}
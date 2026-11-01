using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Powers;

public sealed class LegacySupportForgePower : CustomPowerModel
{
    // 无图标配置
    public override string CustomPackedIconPath => "res://images/hinamods/Powers/support_forge_power.png";
    public override string CustomBigIconPath => "res://images/hinamods/Powers/support_forge_power.png";

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override bool AllowNegative => false;
    public override PowerInstanceType InstanceType => PowerInstanceType.None;
    public override int DisplayAmount => 1;

    // 严格匹配官方方法签名
    public override async Task AfterCardEnteredCombat(CardModel card)
    {
        await base.AfterCardEnteredCombat(card);

        if (Owner != null            // BUFF有持有者
            && card != null
            && card.Owner != null
            && card.Owner.Creature == this.Owner  // 卡牌主人 = 自己
            && !card.IsUpgraded)     // 卡牌未升级
        {
            // 官方原生升级卡牌方法
            CardCmd.Upgrade(card);
            Flash(); // 特效反馈
        }
    }
}
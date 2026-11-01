using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using System.Collections.Generic;

namespace Kaguya
{
    public sealed class LegacyMeltPower : PowerModel
    {
        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Counter;

        // 增加能量上限（每回合多1点能量）
        public override decimal ModifyMaxEnergy(Player player, decimal amount)
        {
            if (player != Owner.Player) return amount;
            return amount + base.Amount; // base.Amount 为层数（此处为1）
        }

        // 增加每回合抽牌数（每回合多抽1张）
        public override decimal ModifyHandDraw(Player player, decimal count)
        {
            if (player != Owner.Player) return count;
            return count + base.Amount;
        }


    }
}
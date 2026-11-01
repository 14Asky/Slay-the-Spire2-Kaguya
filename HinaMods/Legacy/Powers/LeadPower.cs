using BaseLib.Abstracts;
using Godot;
using Kaguya.HinaMods.Legacy.SupportCards.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Kaguya.HinaMods.Legacy.Cards;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Powers;

public sealed class LegacyLeadPower : CustomPowerModel
{
    // ====================== 基础配置（完全参考你的模板） ======================
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    // 官方必备：独立实例
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    // 图标配置
    public override string CustomPackedIconPath => "res://images/hinamods/Powers/lead_power.png";
    public override string CustomBigIconPath => "res://images/hinamods/Powers/lead_power.png";
    public override int DisplayAmount => (int)Amount;

    // ====================== 回合开始触发 ======================
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterPlayerTurnStart(choiceContext, player);
        // 仅持有者生效
        if (Owner != player.Creature || !Owner.IsAlive)
            return;

        // 🔥 这一行 完 全 不 动
        await PlayerCmd.GainEnergy(2, player);

        await CreateSupportDexterity();
        await CreateSupportStrength();

        await PowerCmd.Remove(this);
    }

    // ====================== 生成卡牌（只修复这里） ======================
    private async Task CreateSupportDexterity()
    {
        CardModel newCard = Owner.CombatState.CreateCard(ModelDb.Card<LegacySupportDexterity>(), Owner.Player);
        // 🔥 只删除错误的 addedByPlayer:true，其余完全不变
        await CardPileCmd.AddGeneratedCardToCombat(newCard, PileType.Hand, Owner.Player);
    }

    private async Task CreateSupportStrength()
    {
        CardModel newCard = Owner.CombatState.CreateCard(ModelDb.Card<LegacySupportStrength>(), Owner.Player);
        // 🔥 只删除错误的 addedByPlayer:true，其余完全不变
        await CardPileCmd.AddGeneratedCardToCombat(newCard, PileType.Hand, Owner.Player);
    }
}
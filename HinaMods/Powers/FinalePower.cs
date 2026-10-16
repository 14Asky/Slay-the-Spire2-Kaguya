using BaseLib.Abstracts;
using Godot;
using Kaguya.HinaMods.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Powers;

/// <summary>
/// 收场（FinalePower）
/// 本地化键：KAGUYA-FINALE_HOME_RUN_POWER.title / .description
/// 卡面：下个回合开始时，获得{Amount}张[blue]全垒打[/blue]。
///       （若打出「收场」时消耗了月夜，则获得的牌费用减少2点）
/// </summary>
public sealed class FinalePower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerInstanceType InstanceType => PowerInstanceType.None;
    public override bool AllowNegative => false;
    public override int DisplayAmount => (int)Amount;

    public override string CustomPackedIconPath => "res://images/hinamods/Powers/finale_home_run_power.png";
    public override string CustomBigIconPath => "res://images/hinamods/Powers/finale_home_run_power.png";

    /// <summary>获得的「全垒打」费用减少量（由「收场」打出时是否消耗月夜决定）。</summary>
    public int Discount { get; set; }

    /// <summary>下个回合开始时给「全垒打」，然后自删（只触发一次）。</summary>
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterPlayerTurnStart(choiceContext, player);

        if (Owner == null || Owner.Player != player || !Owner.IsAlive)
            return;

        int count = (int)Amount;
        try
        {
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    CardModel card = player.RunState.CreateCard<HomeRun>(player);
                    if (Discount > 0)
                    {
                        card.EnergyCost.AddThisCombat(-Discount);
                    }

                    Flash();
                    await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player, CardPilePosition.Top);
                }

                player.PlayerCombatState?.RecalculateCardValues();
            }
        }
        finally
        {
            await PowerCmd.Remove(this);
        }
    }
}

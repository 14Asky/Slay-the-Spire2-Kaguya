using BaseLib.Abstracts;
using Kaguya.HinaMods;
using Kaguya.HinaMods.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Powers;

/// <summary>
/// 桃子（PeachPower）
/// 本地化键：KAGUYA-PEACH_BLESSING_POWER.title / .description
/// 卡面：下个回合开始时，随机获得{Amount}张0费[blue]支援[/blue]牌。
/// </summary>
public sealed class PeachPower : CustomPowerModel
{
    // ====================== 基础配置 ======================
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
    public override PowerInstanceType InstanceType => PowerInstanceType.None;

    public override string CustomPackedIconPath => "res://images/hinamods/Powers/peach_blessing_power.png";
    public override string CustomBigIconPath => "res://images/hinamods/Powers/peach_blessing_power.png";

    public override int DisplayAmount => (int)Amount;

    // ====================== 下个回合开始：给支援牌，然后自删 ======================
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterPlayerTurnStart(choiceContext, player);

        if (Owner == null || Owner.Player != player || !Owner.IsAlive)
            return;

        int count = (int)Amount;
        if (count <= 0)
        {
            await PowerCmd.Remove(this);
            return;
        }

        try
        {
            List<CardModel> pool = ModelDb.CardPool<TokenCardPool>()
                .AllCardIds
                .Select(id => ModelDb.GetById<CardModel>(id))
                .OfType<HinaModsCard>()
                .Where(c => c.CustomTags?.Contains(CustomCardTags.SUPPORT) == true
                            && c.EnergyCost.Canonical == 0)
                .Cast<CardModel>()
                .ToList();

            if (pool.Count == 0)
                return;

            List<CardModel> cards = CardFactory.GetDistinctForCombat(
                player,
                pool,
                count,
                player.RunState.Rng.CombatCardGeneration).ToList();

            if (cards.Count == 0)
                return;

            Flash();

            foreach (CardModel card in cards)
            {
                await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player, CardPilePosition.Top);
            }

            player.PlayerCombatState?.RecalculateCardValues();
        }
        finally
        {
            // 「下个回合开始时」只触发一次
            await PowerCmd.Remove(this);
        }
    }
}

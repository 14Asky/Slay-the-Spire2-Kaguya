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
using MegaCrit.Sts2.Core.Models.Powers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Powers;

/// <summary>
/// 宴会（BanquetPower）
/// 本地化键：KAGUYA-SUPPORT_CHANNELING_POWER.title / .description
/// 效果：每回合开始时，随机将一张支援牌加入手牌。
///       若打出的是【升级版宴会】，该牌的费用额外 -1（<see cref="ReduceCost"/>）。
/// </summary>
public sealed class BanquetPower : CustomPowerModel
{
    // ====================== 基础配置 ======================
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
    public override PowerInstanceType InstanceType => PowerInstanceType.None;

    // 图标配置
    public override string CustomPackedIconPath => "res://images/hinamods/Powers/support_channeling.png";
    public override string CustomBigIconPath => "res://images/hinamods/Powers/support_channeling.png";

    // 每回合固定给 1 张，不显示层数
    public override int DisplayAmount => 0;

    /// <summary>
    /// 给牌的减费量。0 = 不减费（普通宴会）；1 = 费用-1（升级版宴会）。
    /// 由 <see cref="Banquet"/> 在施加时写入（打出过升级版就置 1）。
    /// </summary>
    public int ReduceCost { get; set; }

    // ====================== 回合开始：随机支援牌入手（+可选减费） ======================
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterPlayerTurnStart(choiceContext, player);

        if (Owner == null || Owner.Player != player || !Owner.IsAlive)
            return;

        TokenCardPool tokenPool = ModelDb.CardPool<TokenCardPool>();
        List<CardModel> pool = tokenPool.AllCardIds
            .Select(id => ModelDb.GetById<CardModel>(id))
            .OfType<HinaModsCard>()
            .Where(c => c.CustomTags?.Contains(CustomCardTags.SUPPORT) == true)
            .Cast<CardModel>()
            .ToList();

        if (pool.Count == 0)
            return;

        List<CardModel> cards = CardFactory.GetDistinctForCombat(
            player,
            pool,
            1,
            player.RunState.Rng.CombatCardGeneration).ToList();

        if (cards.Count == 0)
            return;

        Flash();

        int reduce = ReduceCost;
        foreach (CardModel card in cards)
        {
            if (reduce > 0)
            {
                card.EnergyCost.AddUntilPlayed(-reduce);

                // 「获得+免费」标记：这类牌打出时不消耗月夜
                if (card is HinaModsCard modCard)
                {
                    modCard.MarkFreePlayByEffect();
                }
            }

            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player, CardPilePosition.Top);
        }

        player.PlayerCombatState?.RecalculateCardValues();
    }
}

using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Powers;

/// <summary>
/// 锦鳞（GoldenScalePower）
/// 本地化键：KAGUYA-SUPPORT_ORBIT_POWER.title / .description
/// 效果：回合开始时，把你【抽牌阶段抽到】的牌里随机 {Amount} 张（1 费及以上）费用 -1。
///       打出多张锦鳞可叠加（2 张 → 每回合随机 2 张；3 张 → 3 张）。
/// 时序：抽牌阶段之后触发（<c>AfterPlayerTurnStart</c> 在 <c>CardPileCmd.Draw</c> 之后，
///       见引擎 CombatManager 回合开始流程）——避免「牌还没进手牌就减费」导致的减费失败。
/// 联机：随机取牌用 <c>Rng.CombatCardSelection</c>（确定性 RNG），不会不同步。
/// </summary>
public sealed class GoldenScalePower : CustomPowerModel
{
    // ====================== 基础配置 ======================
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter; // 可叠加
    public override bool AllowNegative => false;
    public override PowerInstanceType InstanceType => PowerInstanceType.None;

    public override string CustomPackedIconPath => "res://images/hinamods/Powers/support_orbit_power.png";
    public override string CustomBigIconPath => "res://images/hinamods/Powers/support_orbit_power.png";

    /// <summary>层数 = 每回合减费的牌数。</summary>
    public override int DisplayAmount => (int)Amount;

    /// <summary>本回合「抽牌阶段」抽到的牌（由 AfterCardDrawn 收集）。</summary>
    private readonly List<CardModel> _drawnThisTurn = new List<CardModel>();

    // ====================== 收集抽牌阶段的牌 ======================
    /// <summary>只收「抽牌阶段抽到」的牌（fromHandDraw = true），效果抽牌不算。</summary>
    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        await base.AfterCardDrawn(choiceContext, card, fromHandDraw);

        if (!fromHandDraw || card == null || Owner == null)
            return;

        // 只处理拥有者自己的牌
        if (card.Owner == null || card.Owner.Creature != Owner)
            return;

        _drawnThisTurn.Add(card);
    }

    // ====================== 抽牌阶段之后：随机减费 ======================
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterPlayerTurnStart(choiceContext, player);

        if (Owner == null || Owner.Player != player || !Owner.IsAlive)
        {
            _drawnThisTurn.Clear();
            return;
        }

        int count = (int)Amount;
        try
        {
            if (count <= 0 || _drawnThisTurn.Count == 0)
                return;

            // 「1 费及以上」= 卡面基础费用 ≥ 1（X 费牌不参与）
            List<CardModel> candidates = _drawnThisTurn
                .Where(c => c != null
                            && !c.EnergyCost.CostsX
                            && c.EnergyCost.GetWithModifiers(CostModifiers.None) >= 1m
                            && c.Pile != null
                            && c.Pile.Type == PileType.Hand)
                .Distinct()
                .ToList();

            if (candidates.Count == 0)
                return;

            Flash();

            for (int i = 0; i < count && candidates.Count > 0; i++)
            {
                // 确定性 RNG：联机安全
                CardModel pick = player.RunState.Rng.CombatCardSelection.NextItem(candidates);
                if (pick == null)
                    break;

                candidates.Remove(pick);
                pick.EnergyCost.AddUntilPlayed(-1);
            }

            player.PlayerCombatState?.RecalculateCardValues();
        }
        finally
        {
            // 抽牌阶段的牌只在「本回合开始」结算一次
            _drawnThisTurn.Clear();
        }
    }

    /// <summary>回合结束时清空缓存，避免把本回合的效果抽牌带到下回合。</summary>
    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        await base.AfterSideTurnEnd(choiceContext, side, participants);

        if (Owner != null && side == Owner.Side)
        {
            _drawnThisTurn.Clear();
        }
    }
}

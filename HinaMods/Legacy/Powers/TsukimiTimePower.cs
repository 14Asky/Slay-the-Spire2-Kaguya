using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Kaguya.HinaMods.Legacy.Cards;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Powers;

public sealed class LegacyTsukimiTimePower : CustomPowerModel
{
    public const int MAX_STACK = 8000;
    private bool _isProcessing;

    // 🔥 修复：为每个阈值添加独立发放标记（每场战斗重置）
    private bool _granted2000;
    private bool _granted4000;
    private bool _granted6000;
    private bool _granted8000;

    // BUFF基础配置
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    // 自定义图标
    public override string CustomPackedIconPath => "res://images/hinamods/Powers/tsukimi_time_power.png";
    public override string CustomBigIconPath => "res://images/hinamods/Powers/tsukimi_time_power.png";
    public override int DisplayAmount => (int)Amount;

    // 🔥 官方正确重写：层数≥8000时隐藏图标，否则显示
    protected override bool IsVisibleInternal => Amount < MAX_STACK;

    // 应用后初始化封顶 + 重置所有标记
    public override async Task AfterApplied(Creature applier, CardModel cardSource)
    {
        await base.AfterApplied(applier, cardSource);
        await ClampPowerAmount(new ThrowingPlayerChoiceContext());
        // 重置所有发牌标记
        _granted2000 = false;
        _granted4000 = false;
        _granted6000 = false;
        _granted8000 = false;
    }

    // 层数变化回调
    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature applier, CardModel cardSource)
    {
        await base.AfterPowerAmountChanged(choiceContext, power, amount, applier, cardSource);

        if (_isProcessing || Owner == null) return;

        try
        {
            _isProcessing = true;
            await ClampPowerAmount(choiceContext);

            // 财运联动
            if (power is LegacyFortunePower && power.Owner == Owner && amount > 0 && Amount < MAX_STACK)
            {
                int addAmount = (int)(amount * 80);
                int maxAdd = MAX_STACK - (int)Amount;
                int finalAdd = Mathf.Min(addAmount, maxAdd);

                if (finalAdd > 0)
                {
                    await PowerCmd.ModifyAmount(choiceContext, this, finalAdd, applier, cardSource);
                }
            }

            // 检测发牌
            await CheckAndGrantMindCard(choiceContext);
        }
        finally
        {
            _isProcessing = false;
        }
    }

    // 战斗开始触发 + 重置所有标记
    public override async Task BeforeCombatStart()
    {
        await base.BeforeCombatStart();
        // 每场战斗重置所有发牌标记
        _granted2000 = false;
        _granted4000 = false;
        _granted6000 = false;
        _granted8000 = false;

        if (Owner == null || !Owner.IsPlayer) return;
        var ctx = new ThrowingPlayerChoiceContext();
        await CheckAndGrantMindCard(ctx);
    }

    // 强制封顶
    private async Task ClampPowerAmount(PlayerChoiceContext choiceContext)
    {
        int current = (int)Amount;
        if (current > MAX_STACK)
        {
            await PowerCmd.ModifyAmount(choiceContext, this, MAX_STACK - current, Owner, null);
        }
        else if (current < 0)
        {
            await PowerCmd.ModifyAmount(choiceContext, this, -current, Owner, null);
        }
    }

    // 🔥 修复：仅首次突破阈值发牌，区间内不重复发
    private async Task CheckAndGrantMindCard(PlayerChoiceContext ctx)
    {
        if (Owner == null || !Owner.IsPlayer || Owner.CombatState == null)
            return;

        int stack = (int)Amount;

        // 2000 阈值：仅未发放 + 达到条件时发牌
        if (stack >= 2000 && stack < 4000 && !_granted2000)
        {
            await CreateMindCardToDrawPile<LegacyConfusion>(ctx);
            _granted2000 = true;
        }
        // 4000 阈值
        else if (stack >= 4000 && stack < 6000 && !_granted4000)
        {
            await CreateMindCardToDrawPile<LegacyLoneliness>(ctx);
            _granted4000 = true;
        }
        // 6000 阈值
        else if (stack >= 6000 && stack < 8000 && !_granted6000)
        {
            await CreateMindCardToDrawPile<LegacyRelief>(ctx);
            _granted6000 = true;
        }
        // 8000 阈值
        else if (stack >= 8000 && !_granted8000)
        {
            await CreateMindCardToDrawPile<LegacyExpectation>(ctx);
            _granted8000 = true;
        }
    }

    /// <summary>
    /// 生成心境牌加入抽牌堆
    /// </summary>
    private async Task CreateMindCardToDrawPile<T>(PlayerChoiceContext ctx) where T : CardModel
    {
        if (Owner?.CombatState == null || Owner.Player == null)
            return;

        CardModel card = Owner.CombatState.CreateCard<T>(Owner.Player);
        if (card == null) return;

        var results = await CardPileCmd.AddGeneratedCardsToCombat(
            new List<CardModel> { card },
            PileType.Draw,
            Owner.Player,
            CardPilePosition.Random);

        if (results != null)
            CardCmd.PreviewCardPileAdd(results, 1f);
    }
}
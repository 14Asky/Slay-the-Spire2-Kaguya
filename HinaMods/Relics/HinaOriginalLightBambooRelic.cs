using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using Kaguya.HinaMods;
using Kaguya.HinaMods.Relics;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Relics;

/// <summary>
/// 原光之竹（HinaOriginalLightBambooRelic）
/// 本地化键：KAGUYA-HINA_ORIGINAL_LIGHT_BAMBOO_RELIC.title / .description
/// 卡面：你的回合结束时，获得等同于你当前回合打出的卡牌数除以3的[gold]月夜[/gold]层数。
/// </summary>
public sealed class HinaOriginalLightBambooRelic : HinaRelics
{
    // 官方标准私有字段
    private int _cardsPlayedThisTurn;

    // ====================== 基础配置（不变） ======================
    public override RelicRarity Rarity => RelicRarity.Rare;
    // 参考官方：战斗中显示计数
    public override bool ShowCounter => CombatManager.Instance.IsInProgress;

    // 图标路径（自行替换）
    public override string PackedIconPath => "res://images/hinamods/relics/hina_original_light_bamboo_relic.png";
    protected override string PackedIconOutlinePath => "res://images/hinamods/relics/hina_original_light_bamboo_relic.png";
    protected override string BigIconPath => "res://images/hinamods/relics/hina_original_light_bamboo_relic.png";

    // ====================== 【官方标准】计数显示 ======================
    public override int DisplayAmount => CardsPlayedThisTurn;

    // 官方标准：带Mutable校验的计数属性
    private int CardsPlayedThisTurn
    {
        get => _cardsPlayedThisTurn;
        set
        {
            AssertMutable();
            _cardsPlayedThisTurn = value;
            UpdateDisplay();
        }
    }

    // 官方标准：刷新显示
    private void UpdateDisplay()
    {
        InvokeDisplayAmountChanged();
    }

    // ====================== 回合开始：重置计数（官方标准写法） ======================
    public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(Owner.Creature))
        {
            return Task.CompletedTask;
        }

        CardsPlayedThisTurn = 0;
        return Task.CompletedTask;
    }

    // ====================== 卡牌打出：计数+1（逻辑不变） ======================
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await base.AfterCardPlayed(choiceContext, cardPlay);
        // 仅统计玩家自身打出的卡牌
        if (Owner?.Creature != null && cardPlay.Card != null && cardPlay.Card.Owner == Owner)
        {
            CardsPlayedThisTurn++;
        }
    }

    // ====================== 回合结束：计算并获取月夜 ======================
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        await base.AfterSideTurnEnd(choiceContext, side, participants);

        if (side != Owner?.Creature.Side || CardsPlayedThisTurn <= 0 || Owner?.Creature == null)
            return;

        // 整数除法自动向下取整：3/2=1，4/2=2，符合需求
        int gainLayer = CardsPlayedThisTurn / 3;
        if (gainLayer <= 0)
            return;

        var player = Owner.Creature;
        var fortunePower = player.GetPower<MoonlightPower>();
        var context = new ThrowingPlayerChoiceContext();

        if (fortunePower == null)
        {
            await PowerCmd.Apply<MoonlightPower>(context, player, gainLayer, player, null, false);
        }
        else
        {
            await PowerCmd.ModifyAmount(context, fortunePower, gainLayer, player, null, false);
        }

        Flash();
    }

    // ====================== 战斗结束：重置计数（官方标准写法） ======================
    public override Task AfterCombatEnd(CombatRoom _)
    {
        CardsPlayedThisTurn = 0;
        return Task.CompletedTask;
    }
}
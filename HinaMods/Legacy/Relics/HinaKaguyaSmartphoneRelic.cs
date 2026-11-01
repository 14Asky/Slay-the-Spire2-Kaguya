using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Legacy.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using Kaguya.HinaMods;
using Kaguya.HinaMods.Legacy.Relics;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace Kaguya.HinaMods.Legacy.Relics;

/// <summary>
/// 辉夜的智能手机
/// 稀有遗物
/// 每回合中，每当你首次打出5张牌，立即获得1点能量
/// </summary>
public sealed class LegacyHinaKaguyaSmartphoneRelic : HinaRelics
{
    // 官方标准私有字段
    private int _cardsPlayedThisTurn;
    private bool _hasTriggeredThisTurn;

    /// <summary>
    /// 回合内进度必须走 [SavedProperty]：联机重连时 <c>RelicModel.FromSerializable</c>
    /// 重建实例不会调用 AfterObtained，未标记的字段会回默认值，
    /// 导致计数归零、遗物在同一回合内提前/重复触发。
    /// </summary>
    [SavedProperty]
    private bool HasTriggeredThisTurn
    {
        get => _hasTriggeredThisTurn;
        set
        {
            AssertMutable();
            _hasTriggeredThisTurn = value;
        }
    }

    // ====================== 基础配置 ======================
    public override RelicRarity Rarity => RelicRarity.Uncommon;
    public override bool ShowCounter => CombatManager.Instance.IsInProgress;

    // 图标路径
    public override string PackedIconPath => "res://images/hinamods/relics/hina_kaguya_smartphone_relic.png";
    protected override string PackedIconOutlinePath => "res://images/hinamods/relics/hina_kaguya_smartphone_relic.png";
    protected override string BigIconPath => "res://images/hinamods/relics/hina_kaguya_smartphone_relic.png";

    // ====================== 完全对齐参考代码：表达式体属性 ======================
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new EnergyVar(1)
    };

    // ====================== 计数显示 ======================
    public override int DisplayAmount => CardsPlayedThisTurn;

    /// <summary>本回合已打出的牌数，同样需要跨重连保存。</summary>
    [SavedProperty]
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

    private void UpdateDisplay()
    {
        InvokeDisplayAmountChanged();
    }

    // ====================== 回合开始重置计数 ======================
    public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(Owner.Creature))
            return Task.CompletedTask;

        CardsPlayedThisTurn = 0;
        HasTriggeredThisTurn = false;
        return Task.CompletedTask;
    }

    // ====================== 核心逻辑：出牌计数 & 触发能量 ======================
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await base.AfterCardPlayed(choiceContext, cardPlay);

        // 修复Player类型（严格对照官方Player源码）
        if (Owner == null || cardPlay.Card?.Owner != Owner)
            return;

        CardsPlayedThisTurn++;
        const int TRIGGER_COUNT = 5;
            
        if (CardsPlayedThisTurn >= TRIGGER_COUNT && !HasTriggeredThisTurn)
        {
            HasTriggeredThisTurn = true;
            await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
            Flash();
        }
    }

    // ====================== 固定原有代码 ======================
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        await base.AfterSideTurnEnd(choiceContext, side, participants);
    }

    public override Task AfterCombatEnd(CombatRoom _)
    {
        CardsPlayedThisTurn = 0;
        HasTriggeredThisTurn = false;
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterPlayerTurnStart(choiceContext, player);
    }
}

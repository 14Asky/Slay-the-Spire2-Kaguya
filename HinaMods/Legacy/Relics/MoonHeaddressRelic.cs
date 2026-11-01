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
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using Kaguya.HinaMods;
using Kaguya.HinaMods.Legacy.Cards;
using Kaguya.HinaMods.Legacy.Relics;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace Kaguya.HinaMods.Legacy.Relics;

/// <summary>
/// 月亮头饰
/// 专属遗物
/// 每回合第一次打出卡牌获得月夜时，额外获得2层月夜
/// </summary>
public sealed class LegacyMoonHeaddressRelic : HinaRelics
{
    // ====================== 基础配置 ======================
    public override RelicRarity Rarity => RelicRarity.Uncommon;
    public override bool ShowCounter => false;

    // 图标路径
    public override string PackedIconPath => "res://images/hinamods/relics/moon_headdress_relic.png";
    protected override string PackedIconOutlinePath => "res://images/hinamods/relics/moon_headdress_relic.png";
    protected override string BigIconPath => "res://images/hinamods/relics/moon_headdress_relic.png";

    // 回合触发标记（对标参考代码格式）
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

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromPower<LegacyFortunePower>()
    };

    public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        // 完全复刻你的参考代码格式
        if (!participants.Contains(Owner.Creature))
            return Task.CompletedTask;

        // 回合开始重置触发标记
        HasTriggeredThisTurn = false;
        return Task.CompletedTask;
    }

    // ====================== 【参考2】监听力量层数变化 ======================
    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature applier, CardModel cardSource)
    {
        await base.AfterPowerAmountChanged(choiceContext, power, amount, applier, cardSource);

        // 🔥【修复】必须是【自己身上】的月夜Buff，不是则返回
        if (power.Owner != Owner.Creature)
            return;

        // 2. 必须是月夜(LegacyFortunePower)
        if (power is not LegacyFortunePower)
            return;

        // 3. 仅【获得层数】时触发（失去层数不触发）
        if (amount <= 0)
            return;

        // 4. 必须是【打出卡牌】获得的月夜（核心判定）
        if (cardSource == null)
            return;

        // 5. 每回合仅触发一次
        if (HasTriggeredThisTurn)
            return;

        // 标记已触发
        HasTriggeredThisTurn = true;

        // 额外获得2层月夜（完全照搬你的参考代码）
        await PowerCmd.Apply<LegacyFortunePower>(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            2m,
            Owner.Creature,
            null
        );
        Flash();
    }

    // ====================== 固定空实现 ======================
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await base.AfterCardPlayed(choiceContext, cardPlay);
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        await base.AfterSideTurnEnd(choiceContext, side, participants);
    }

    public override Task AfterCombatEnd(CombatRoom _)
    {
        return Task.CompletedTask;
    }
}
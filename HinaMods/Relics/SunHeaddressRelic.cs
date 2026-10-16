using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using Kaguya.HinaMods.Cards;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using Kaguya.HinaMods;
using Kaguya.HinaMods.Relics;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Relics;

/// <summary>
/// 太阳头饰（SunHeaddressRelic）
/// 本地化键：KAGUYA-SUN_HEADDRESS_RELIC.title / .description
/// 卡面：你的[gold]抽牌堆[/gold]中没有牌时，你的手牌会被额外打出一次。
/// </summary>
public sealed class SunHeaddressRelic : HinaRelics
{
    // ====================== 基础配置 ======================
    public override RelicRarity Rarity => RelicRarity.Shop;
    public override bool ShowCounter => false;

    // 图标路径
    public override string PackedIconPath => "res://images/hinamods/relics/sun_headdress_relic.png";
    protected override string PackedIconOutlinePath => "res://images/hinamods/relics/sun_headdress_relic.png";
    protected override string BigIconPath => "res://images/hinamods/relics/sun_headdress_relic.png";

    // ====================== 回合开始 ======================
    public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(Owner.Creature))
            return Task.CompletedTask;

        return Task.CompletedTask;
    }

    // ====================== 核心逻辑（适配原版CardPlay，无任何报错） ======================
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await base.AfterCardPlayed(choiceContext, cardPlay);

        // 仅自身生效
        if (Owner == null || cardPlay.Card?.Owner != Owner)
            return;

        // 【防无限递归】原生判断：自动打出的牌不再触发（完美替代SetFlag）
        if (cardPlay.IsAutoPlay)
            return;

        // 核心判定：抽牌堆为空
        bool isDrawPileEmpty = Owner.PlayerCombatState.DrawPile.Cards.Count == 0;
        if (!isDrawPileEmpty)
            return;

        // 原生API额外打出一次（IsAutoPlay=true 阻断递归）
        // 离场防护：能力牌打出后会被 RemoveFromCombat，此处若再 AutoPlay 会抛异常。
        if (HinaModsCard.CanReplayAutomatically(cardPlay.Card))
        {
            await CardCmd.AutoPlay(
                choiceContext,
                cardPlay.Card,
                cardPlay.Target,
                AutoPlayType.Default
            );
        }

        // 触发遗物闪光
        Flash();
    }

    // ====================== 固定代码结构 ======================
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        await base.AfterSideTurnEnd(choiceContext, side, participants);
    }

    public override Task AfterCombatEnd(CombatRoom _)
    {
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterPlayerTurnStart(choiceContext, player);
    }
}
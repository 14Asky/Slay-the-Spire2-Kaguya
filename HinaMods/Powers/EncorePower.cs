using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Powers;

/// <summary>
/// 安可（EncorePower）
/// 本地化键：KAGUYA-ENCORE.title / .description
/// 卡面：每回合使你打出的前{EncorePower:diff()}张牌返回[gold]手牌[/gold]。
/// </summary>
public sealed class EncorePower : CustomPowerModel
{
    // ====================== 基础配置 ======================
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
    public override PowerInstanceType InstanceType => PowerInstanceType.None;

    // 图标配置
    public override string CustomPackedIconPath => "res://images/hinamods/Powers/encore_power.png";
    public override string CustomBigIconPath => "res://images/hinamods/Powers/encore_power.png";
    public override int DisplayAmount => (int)Amount;

    // ====================== 核心：出牌结果堆拦截 ======================
    public override CardLocation ModifyCardPlayResultLocation(CardModel card, bool isAutoPlay, ResourceInfo resources, CardLocation location)
    {
        if (Owner == null || card.Owner.Creature != Owner)
            return location;

        // 自动打出的牌不算「你打出的牌」，也不回收（避免与余音/跃击等自动打出形成循环）
        if (isAutoPlay)
            return location;

        if (card.IsDupe)
            return location;

        // 只回收原本会进入弃牌堆的牌：能力牌（PileType.None）与消耗牌（PileType.Exhaust）不受影响
        if (location.pileType != PileType.Discard)
            return location;

        // 本回合（不含当前这张）已经打出过的牌数。
        //
        // ⚠ 时序（引擎 OnPlayWrapper 的实际顺序）：
        //     GetResultLocationForCardPlay → ModifyCardPlayResultLocation(此处读历史)
        //     → History.CardPlayStarted → OnPlay
        //   也就是说「读历史」发生在「当前这张被记入历史」之前，所以 playedThisTurn
        //   天然不含当前这张，语义正确。
        //
        // ⚠ 关键：安可自己是 CardType.Power，打出后结局堆为 PileType.None
        //   （见 CardModel.GetResultLocationForCardPlay），本方法在上面的
        //   `location.pileType != PileType.Discard` 处就提前 return 了 —— 安可不消耗额度。
        //   但安可仍然会被引擎记入 History.CardPlaysStarted，所以计数时必须按
        //   「能力牌不计入」来排除，否则安可之后的下一张牌会被误判为「已打出 1 张」
        //   而享受不到第 1 张的回收。
        //   这里刻意不比较 SourceCard 引用：多张安可时 SourceCard 只保留最后一张，
        //   引用比较不可靠；按卡牌类型判定是稳定口径。
        int playedThisTurn = CountPlayedCardsThisTurn();

        if (playedThisTurn >= Amount)
            return location;

        location.pileType = PileType.Hand;
        location.position = CardPilePosition.Top;
        location.player = Owner.Player;
        return location;
    }

    /// <summary>
    /// 本回合本玩家已打出的「普通牌」数量（不含当前这张、不含自动打出、不含能力牌）。
    /// 能力牌不计入回收额度 —— 安可自己因此不会占用第 1 张的回收。
    /// </summary>
    private int CountPlayedCardsThisTurn()
    {
        MegaCrit.Sts2.Core.Combat.ICombatState? combatState = Owner?.CombatState;
        if (Owner == null || combatState == null)
            return 0;

        return CombatManager.Instance.History.CardPlaysStarted.Count(e =>
            e.HappenedThisTurn(combatState)
            && e.CardPlay.Player == Owner.Player
            && !e.CardPlay.IsAutoPlay
            && !e.CardPlay.Card.IsDupe
            && e.CardPlay.Card.Type != CardType.Power);
    }

    public override Task AfterModifyingCardPlayResultLocation(CardModel card, CardLocation cardLocation)
    {
        if (Owner != null && card.Owner.Creature == Owner)
            Flash();

        return Task.CompletedTask;
    }
}

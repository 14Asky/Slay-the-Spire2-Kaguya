using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Kaguya.HinaMods;
using Kaguya.HinaMods.Cards;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Powers;

/// <summary>
/// 余音（ReverberationPower）
/// 本地化键：KAGUYA-SINGER_CASCADE.title / .description
/// 卡面：在你的回合结束时，自动打出你本回合打出的最后一张牌。
/// </summary>
public sealed class ReverberationPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    public override string CustomPackedIconPath => "res://images/hinamods/Powers/singer_cascade_power.png";
    public override string CustomBigIconPath => "res://images/hinamods/Powers/singer_cascade_power.png";

    public override int DisplayAmount => (int)Amount;

    // ====================== 回合结束前钩子（先出牌、再回抽卡牌） ======================
    public override async Task BeforeSideTurnEnd(PlayerChoiceContext ctx, CombatSide side, IEnumerable<Creature> participants)
    {
        await base.BeforeSideTurnEnd(ctx, side, participants);

        // 仅BUFF拥有者自身回合触发
        if (side != Owner.Side)
        {
            return;
        }

        // 取本回合打出的最后一张牌（不限标签）
        // 用 Owner.Creature.CombatState 而非 CombatManager.Instance.DebugOnlyGetState()：
        // 后者在联机/非战斗上下文可能返回 null，会让 HappenedThisTurn 恒为 false（余音永不触发）。
        MegaCrit.Sts2.Core.Combat.ICombatState? combatState = Owner.CombatState;
        CardPlayFinishedEntry lastEntry = combatState == null ? null : CombatManager.Instance.History.CardPlaysFinished
            .Where(delegate (CardPlayFinishedEntry e)
            {
                bool isOwnerCard = e.CardPlay.Card.Owner == Owner.Player
                    && e.HappenedThisTurn(combatState);

                // 排除自动出牌、复制牌，防止循环触发
                return isOwnerCard && !e.CardPlay.IsAutoPlay && !e.CardPlay.Card.IsDupe;
            })
            .LastOrDefault();

        if (lastEntry != null)
        {
            // 离场防护：若「最后一张牌」是能力牌，它打出后已被 RemoveFromCombat，
            // 此时再 AutoPlay 会让引擎抛 InvalidOperationException。
            // 同时必须排除副本（IsDupe），副本打出后同样会离场。
            if (HinaModsCard.CanReplayAutomatically(lastEntry.CardPlay.Card))
            {
                await CardCmd.AutoPlay(ctx, lastEntry.CardPlay.Card, null);
            }
        }
    }
}

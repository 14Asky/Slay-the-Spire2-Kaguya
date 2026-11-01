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
using Kaguya.HinaMods.Legacy.Cards;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Powers;

/// <summary>
/// 余音
/// 每回合结束时，自动打出你**当前回合**中最后N张打出的歌声牌
/// N = 当前余音层数（永久叠加）
/// </summary>
public sealed class LegacySingerCascadePower : CustomPowerModel
{
    // ====================== 官方标准Power配置 ======================
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    public override string CustomPackedIconPath => "res://images/hinamods/Powers/singer_cascade_power.png";
    public override string CustomBigIconPath => "res://images/hinamods/Powers/singer_cascade_power.png";

    public override int DisplayAmount => (int)Amount;

    // ====================== 回合结束前钩子（保证执行顺序：先出牌、再回抽卡牌） ======================
    public override async Task BeforeSideTurnEnd(PlayerChoiceContext ctx, CombatSide side, IEnumerable<Creature> participants)
    {
        await base.BeforeSideTurnEnd(ctx, side, participants);

        // 仅BUFF拥有者自身回合触发
        if (side != Owner.Side)
        {
            return;
        }

        int playCount = (int)Amount;
        if (playCount <= 0)
        {
            return;
        }

        var cardList = CombatManager.Instance.History.CardPlaysFinished
            .Where(delegate (CardPlayFinishedEntry e)
            {
                // ========== 唯一修改：改为判定【当前回合】打出的卡牌 ==========
                bool isOwnerCard = e.CardPlay.Card.Owner == Owner.Player 
                    && e.HappenedThisTurn(CombatManager.Instance.DebugOnlyGetState());

                bool validCard = isOwnerCard;

                if (validCard)
                {
                    // 仅筛选歌声标签卡牌
                    bool isSingerCard = e.CardPlay.Card is HinaModsCard hinaCard && hinaCard.CustomTags.Contains(LegacyCustomCardTags.SINGER);
                    validCard = isSingerCard;
                }

                // 排除自动出牌、复制牌，防止循环触发
                return validCard && !e.CardPlay.IsAutoPlay && !e.CardPlay.Card.IsDupe;
            })
            .TakeLast(playCount)   // 取当前回合最后 N 张
            .Reverse()             // 逆序恢复原始打出顺序
            .Select(e => e.CardPlay.Card)
            .ToList();

        // 直接打出原卡牌（不复制）
        if (cardList.Count > 0)
        {
            foreach (CardModel card in cardList)
            {
                await CardCmd.AutoPlay(ctx, card, null);
            }
        }
    }
}
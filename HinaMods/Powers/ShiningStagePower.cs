using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Powers;

/// <summary>
/// 闪耀舞台（ShiningStagePower）
/// 本地化键：KAGUYA-SINGER_CONVERT.title / .description
/// 卡面：回合结束时，获得等同于你本回合打出卡牌数的[gold]活力[/gold]。
/// </summary>
public sealed class ShiningStagePower : CustomPowerModel
{
    // ====================== 基础配置 ======================
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
    public override PowerInstanceType InstanceType => PowerInstanceType.None;

    // 图标配置
    public override string CustomPackedIconPath => "res://images/hinamods/Powers/singer_convert_power.png";
    public override string CustomBigIconPath => "res://images/hinamods/Powers/singer_convert_power.png";
    public override int DisplayAmount => (int)Amount;

    // 活力悬浮提示
    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<VigorPower>();
        }
    }

    // ====================== 回合结束：按本回合打出牌数获得活力 ======================
    public override async Task AfterSideTurnEnd(PlayerChoiceContext ctx, CombatSide side, IEnumerable<Creature> participants)
    {
        await base.AfterSideTurnEnd(ctx, side, participants);

        if (Owner == null || side != Owner.Side || !Owner.IsAlive)
            return;

        int playedThisTurn = CombatManager.Instance.History.CardPlaysFinished.Count(e =>
            e.HappenedThisTurn(CombatState)
            && e.CardPlay.Player == Owner.Player
            && !e.CardPlay.IsAutoPlay);

        if (playedThisTurn <= 0)
            return;

        Flash();
        await PowerCmd.Apply<VigorPower>(ctx, Owner, playedThisTurn * Amount, Owner, null);
    }
}

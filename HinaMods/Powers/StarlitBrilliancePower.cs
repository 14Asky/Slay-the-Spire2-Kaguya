using BaseLib.Abstracts;
using Godot;
using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Powers;

/// <summary>
/// 星夜璀璨（StarlitBrilliancePower）
/// 本地化键：KAGUYA-STARLIT_BRILLIANCE_POWER.title / .description
/// 卡面：每个回合每当你打出3张牌时，获得{Amount}层[gold]月夜[/gold]。
/// 说明：计数按「本回合打出的牌数」累计，每满 3 张结算一次并清零；回合开始时清零。
/// </summary>
public sealed class StarlitBrilliancePower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    public override string CustomPackedIconPath => "res://images/hinamods/Powers/tsukimi_draw_buff.png";
    public override string CustomBigIconPath => "res://images/hinamods/Powers/tsukimi_draw_buff.png";

    // 用 Counter 叠加（Amount=1/2）；不用 Instanced —— 后者每次施放会多挂一个独立实例
    public override PowerInstanceType InstanceType => PowerInstanceType.None;
    public override int DisplayAmount => (int)Amount;

    /// <summary>本回合已打出的牌数（每满 3 张结算一次并清零）。</summary>
    private int _playedThisTurn;

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await base.AfterCardPlayed(choiceContext, cardPlay);

        if (Owner == null || cardPlay.Player?.Creature != Owner)
            return;

        _playedThisTurn++;
        if (_playedThisTurn < 3)
            return;

        _playedThisTurn = 0;
        await PowerCmd.Apply<MoonlightPower>(choiceContext, Owner, Amount, Owner, null, false);
    }

    /// <summary>每个回合重新开始计数。</summary>
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterPlayerTurnStart(choiceContext, player);

        if (Owner?.Player != player)
            return;

        _playedThisTurn = 0;
    }
}

using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Kaguya.HinaMods.Legacy.Cards;
using System.Collections.Generic; // 新增：IEnumerable<Creature>所需
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Powers;

public sealed class LegacySupportFormationBuff : CustomPowerModel
{
    // ====================== 基础配置（无修改） ======================
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override bool AllowNegative => false;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
    public override int DisplayAmount => (int)Amount;

    // 图标路径（无修改）
    public override string CustomPackedIconPath => "res://images/hinamods/Powers/support_formation.png";
    public override string CustomBigIconPath => "res://images/hinamods/Powers/support_formation.png";

    // 回合触发标记（无修改）
    private bool _playedSupportAttack;
    private bool _playedSupportDefense;

    // ====================== 回合开始重置标记（API正确，无需修改） ======================
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterPlayerTurnStart(choiceContext, player);
        if (player == Owner.Player)
        {
            _playedSupportAttack = false;
            _playedSupportDefense = false;
        }
    }

    // ====================== 卡牌打出监听（API正确，无需修改） ======================
    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        await base.AfterCardPlayed(context, cardPlay);

        if (Owner == null || !Owner.IsPlayer || !Owner.IsAlive)
            return;
        if (cardPlay.Card.Owner != Owner.Player)
            return;

        CardModel playedCard = cardPlay.Card;

        if (playedCard is not HinaModsCard modCard
            || modCard.CustomTags?.Contains(LegacyCustomCardTags.SUPPORT) != true)
        {
            return;
        }

        if (modCard.Type == CardType.Attack)
        {
            _playedSupportAttack = true;
        }
        else if (modCard.Type == CardType.Skill)
        {
            _playedSupportDefense = true;
        }
    }

    // ====================== 回合结束结算（API已更新） ======================
    // 🔥 唯一修改：将废弃的AfterTurnEnd更新为最新的AfterSideTurnEnd签名
    public override async Task AfterSideTurnEnd(PlayerChoiceContext ctx, CombatSide side, IEnumerable<Creature> participants)
    {
        // ✅ 正确调用基类方法
        await base.AfterSideTurnEnd(ctx, side, participants);

        // 原有逻辑完全不变
        if (side != Owner.Side)
            return;

        if (_playedSupportAttack && _playedSupportDefense)
        {
            // 🔥 你已经修复的PowerCmd调用保持不变
            await PowerCmd.Apply<StrengthPower>(ctx, Owner, Amount, Owner, null);
            await PowerCmd.Apply<DexterityPower>(ctx, Owner, Amount, Owner, null);
            Flash();
        }
    }
}
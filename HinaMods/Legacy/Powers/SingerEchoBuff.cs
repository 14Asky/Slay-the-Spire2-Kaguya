using BaseLib.Abstracts;
using Godot;
using Kaguya.HinaMods.Legacy.SupportCards.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using Kaguya.HinaMods;
using Kaguya.HinaMods.Legacy.Cards;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Powers;

public sealed class LegacySingerEchoBuff : CustomPowerModel
{
    // 防递归触发
    private bool _isTriggering;
    // 每回合触发标记
    private bool _hasPlayedSingerCard;

    // 官方标准BUFF配置
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override bool AllowNegative => false;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
    public override int DisplayAmount => (int)Amount;

    // 图标路径（自行替换）
    public override string CustomPackedIconPath => "res://images/hinamods/Powers/singer_echo_buff.png";
    public override string CustomBigIconPath => "res://images/hinamods/Powers/singer_echo_buff.png";

    // 回合开始重置标记
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterPlayerTurnStart(choiceContext, player);
        if (player == Owner.Player)
        {
            _hasPlayedSingerCard = false;
        }
    }

    // 战斗结束重置
    public override async Task AfterCombatEnd(CombatRoom room)
    {
        await base.AfterCombatEnd(room);
        _hasPlayedSingerCard = false;
    }

    // 核心：打出卡牌后监听
    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        await base.AfterCardPlayed(context, cardPlay);

        // 基础校验
        if (Owner == null || !Owner.IsPlayer || !Owner.IsAlive)
            return;
        if (cardPlay.Card.Owner != Owner.Player)
            return;
        // 本回合已触发过 / 正在触发
        if (_hasPlayedSingerCard || _isTriggering)
            return;

        CardModel playedCard = cardPlay.Card;

        // 🔥 判断：是否为【歌者（LegacySinger）牌】
        // 用法1：使用你的自定义标签（和支援标签一致，推荐）
        if (playedCard is not HinaModsCard modCard
            || modCard.CustomTags?.Contains(LegacyCustomCardTags.SINGER) != true)
        {
            return;
        }

        // 标记已触发
        _hasPlayedSingerCard = true;
        _isTriggering = true;

        try
        {
            // 🔥 额外免费打出一次同一张牌（继承目标/升级状态）
            await CardCmd.AutoPlay(
                choiceContext: context,
                card: playedCard,
                target: cardPlay.Target,
                type: AutoPlayType.Default
            );
        }
        finally
        {
            _isTriggering = false;
        }
    }
}
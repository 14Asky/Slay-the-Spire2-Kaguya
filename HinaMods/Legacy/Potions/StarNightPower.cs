// 保留原有全部引用
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;

namespace Kaguya.HinaMods.Legacy.Powers;

/// <summary>
/// 星夜之力
/// 下一张打出的牌：重放次数+1，获得消耗
/// </summary>
// 🔥 修改为继承 CustomPowerModel（和FortunePower一致）
public sealed class LegacyStarNightPower : CustomPowerModel
{
    // 官方标准BUFF配置
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    // 🔥 新增：和FortunePower统一配置
    public override bool AllowNegative => false;

    // 🔥 新增：自定义图标路径（完全匹配你的模组格式）
    public override string CustomPackedIconPath => "res://images/hinamods/Powers/star_night_power.png";
    public override string CustomBigIconPath => "res://images/hinamods/Powers/star_night_power.png";

    // ========== 核心逻辑：完全不变，卡牌打出后触发 ==========
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await base.AfterCardPlayed(choiceContext, cardPlay);

        // 仅对自己的卡牌生效
        if (cardPlay.Card.Owner.Creature != base.Owner)
            return;

        var card = cardPlay.Card;
        // 完全照搬你的 LegacyEncore 卡牌重放逻辑
        card.BaseReplayCount += 1;
        CardCmd.Preview(card);

        //添加消耗
        CardCmd.ApplyKeyword(card, CardKeyword.Ethereal);

        // 触发后立即移除BUFF（仅限一次）
        await PowerCmd.Remove(this);
    }

    // 回合结束自动移除（防止残留）：完全不变
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(base.Owner))
        {
            await PowerCmd.Remove(this);
        }
    }
}
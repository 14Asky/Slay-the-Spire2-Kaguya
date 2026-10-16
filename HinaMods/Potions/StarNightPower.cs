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

namespace Kaguya.HinaMods.Powers;

/// <summary>
/// 循声（StarNightPower）
/// 本地化键：KAGUYA-STAR_NIGHT_POWER.title / .description
/// 卡面：你打出的下一张牌后，该牌获得[gold]重放[/gold]1和[gold]虚无[/gold]。
/// 代码实测：下一张牌 重放 +1（BaseReplayCount）、施加 Ethereal（虚无）；触发后或回合结束时移除本能力。
/// 由「循声药水」（StarNightPotion）施加。
/// </summary>
public sealed class StarNightPower : CustomPowerModel
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
        // 完全照搬你的 Encore 卡牌重放逻辑
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
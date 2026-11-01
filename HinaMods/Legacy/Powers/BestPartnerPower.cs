using BaseLib.Abstracts;
using Kaguya.CardPools;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace Kaguya.Powers;

public sealed class LegacyBestPartnerPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter; // 层数表示抽牌数量

    // 可选：自定义图标路径
    public override string CustomPackedIconPath => "res://images/packed/card_portraits/kaguya/BestPartnerPower.png";
    public override string CustomBigIconPath => "res://images/packed/card_portraits/kaguya/BestPartnerPower.png";

    // 每打出一张牌后触发
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 只对能力拥有者打出的牌生效
        if (cardPlay.Card.Owner.Creature != Owner) return;

        // 检查打出的牌是否为伙伴牌（包含标签 (CardTag)1004）
        bool isPartner = cardPlay.Card.Tags.Contains((CardTag)1004);
        if (!isPartner) return;

        int drawAmount = (int)Amount;
        if (drawAmount > 0)
        {
            await CardPileCmd.Draw(choiceContext, drawAmount, cardPlay.Card.Owner);
        }

        Flash();
    }
}

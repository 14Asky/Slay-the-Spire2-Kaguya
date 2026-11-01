using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Kaguya.Powers
{
    public sealed class LegacyChaoticSoundPowerV2 : CustomPowerModel
    {
        public override PowerType Type => PowerType.Debuff;
        public override PowerStackType StackType => PowerStackType.Single; // 不叠加层数

        // 图标路径（可根据需要调整）
        public override string CustomPackedIconPath => "res://images/powers/ChaoticSoundPower.png";
        public override string CustomBigIconPath => "res://images/powers/ChaoticSoundPower.png";

        // 每次抽牌后触发
        public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
        {
            // 只对能力拥有者自身抽牌时生效
            if (card.Owner.Creature != Owner) return;

            // 对能力拥有者造成3点伤害（可被格挡）
            await CreatureCmd.Damage(choiceContext, Owner, 2, ValueProp.Unpowered, Owner, null, null);
        }

        // 回合结束时移除自身
        public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
        {
            if (side == Owner.Side)
                await PowerCmd.Remove(this);
        }
    }
}

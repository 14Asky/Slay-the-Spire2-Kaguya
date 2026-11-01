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
    public sealed class LegacyChaoticSoundPower : CustomPowerModel
    {
        public override PowerType Type => PowerType.Debuff;
        public override PowerStackType StackType => PowerStackType.Single;

        public override string CustomPackedIconPath => "res://images/powers/ChaoticSoundPower.png";
        public override string CustomBigIconPath => "res://images/powers/ChaoticSoundPower.png";

        public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            if (cardPlay.Card.Owner.Creature != Owner) return;
            await CreatureCmd.Damage(choiceContext, Owner, 3, ValueProp.Unpowered, Owner, null, null);
        }

        public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
        {
            if (side == Owner.Side)
                await PowerCmd.Remove(this);
        }
    }
}

using BaseLib.Abstracts;
using Kaguya.CardPools;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Kaguya.Powers;

public sealed class LegacyLimitBreakPower : CustomPowerModel
{
    private int _turnCounter = 0;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override string CustomPackedIconPath => "res://images/powers/limit_break.png";
    public override string CustomBigIconPath => "res://images/powers/limit_break.png";

    public override decimal ModifyDamageMultiplicative(Creature target, decimal amount, ValueProp props, Creature dealer, CardModel cardSource, MegaCrit.Sts2.Core.Entities.Cards.CardPlay? cardPlay)
    {
        if (dealer == Owner && props.HasFlag(ValueProp.Move))
            return 2m;
        return 1m;
    }

    public override decimal ModifyBlockMultiplicative(Creature target, decimal block, ValueProp props, CardModel cardSource, CardPlay cardPlay)
    {
        if (target == Owner && props.HasFlag(ValueProp.Move))
            return 2m;
        return 1m;
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != Owner.Side) return;

        _turnCounter++;
        if (_turnCounter >= 3)
        {
            // 先清除自身所有高维生物能力
            var highDimPowers = Owner.Powers.OfType<LegacyHighDimensionalBeingPower>().ToList();
            foreach (var power in highDimPowers)
            {
                await PowerCmd.Remove(power);
            }

            // 对玩家自身施加极大灾厄并结算（即死亡）
            decimal doomAmount = int.MaxValue;
            await PowerCmd.Apply<DoomPower>(choiceContext, Owner, doomAmount, Owner, null);
            var doomedCreatures = DoomPower.GetDoomedCreatures(new[] { Owner });
            await DoomPower.DoomKill(doomedCreatures);

            // 移除自身的所有灾厄（确保没有残留）
            if (Owner.HasPower<DoomPower>())
            {
                await PowerCmd.Remove<DoomPower>(Owner);
            }

            // 移除自身能力（避免重复触发）
            await PowerCmd.Remove(this);
        }
    }

    public override async Task AfterApplied(Creature applier, CardModel cardSource)
    {
        Flash();
        await Task.CompletedTask;
    }
}


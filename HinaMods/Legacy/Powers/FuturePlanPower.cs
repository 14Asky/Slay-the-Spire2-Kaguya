using BaseLib.Abstracts;
using Kaguya.CardPools;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Random;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace Kaguya.Powers;

public sealed class LegacyFuturePlanPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override string CustomPackedIconPath => "res://images/packed/card_portraits/kaguya/FuturePlanPower.png";
    public override string CustomBigIconPath => "res://images/packed/card_portraits/kaguya/FuturePlanPower.png";

    // 玩家回合开始时触发（替代已弃用的 BeforeHandDraw）
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != CombatSide.Player) return;
        var player = Owner.Player;
        if (player == null) return;

        var allCards = player.Character.CardPool.GetUnlockedCards(
            player.UnlockState,
            player.RunState.CardMultiplayerConstraint);
        var zeroCostCards = allCards.Where(c =>
            c.EnergyCost != null &&
            c.EnergyCost.Canonical == 0 &&
            !c.EnergyCost.CostsX).ToList();

        if (zeroCostCards.Count == 0) return;

        Rng rng = player.RunState.Rng.CombatCardGeneration;
        var selected = CardFactory.GetDistinctForCombat(player, zeroCostCards, 1, rng).FirstOrDefault();
        if (selected == null) return;

        var generated = selected.CreateClone();
        await CardPileCmd.AddGeneratedCardToCombat(generated, PileType.Hand, player);

        Flash();
    }
}

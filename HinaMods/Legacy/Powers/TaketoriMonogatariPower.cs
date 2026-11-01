using BaseLib.Abstracts;
using Kaguya.CardPools;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace Kaguya.Powers;

public sealed class LegacyTaketoriMonogatariPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override string CustomPackedIconPath => "res://images/packed/card_portraits/kaguya/TaketoriMonogatariPower.png";
    public override string CustomBigIconPath => "res://images/packed/card_portraits/kaguya/TaketoriMonogatariPower.png";

    public bool ShouldUpgradeGeneratedCards { get; set; } = false;

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != Owner.Side) return;
        var player = Owner.Player;
        if (player == null) return;

        var partnerCards = player.Character.CardPool
            .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
            .Where(c => c.Tags.Contains((CardTag)1004))
            .ToList();

        if (partnerCards.Count == 0) return;

        Rng rng = player.RunState.Rng.CombatCardGeneration;
        CardModel selected = CardFactory.GetDistinctForCombat(player, partnerCards, 1, rng).FirstOrDefault();
        if (selected == null) return;

        CardModel generated = selected.CreateClone();

        if (ShouldUpgradeGeneratedCards)
        {
            CardCmd.Upgrade(generated);
        }

        generated.AddKeyword(CardKeyword.Ethereal);
        generated.SetToFreeThisTurn();

        // 修正：移除 addedByPlayer 参数，使用正确签名 (card, pileType, creator)
        await CardPileCmd.AddGeneratedCardToCombat(generated, PileType.Hand, player);

        Flash();
    }
}

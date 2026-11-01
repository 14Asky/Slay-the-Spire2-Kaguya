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

public sealed class LegacyModelStudentPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string CustomPackedIconPath => "res://images/packed/card_portraits/kaguya/ModelStudentPower.png";
    public override string CustomBigIconPath => "res://images/packed/card_portraits/kaguya/ModelStudentPower.png";

    // 使用新版钩子：玩家回合开始时触发
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != Owner.Side) return;
        var player = Owner.Player;
        if (player == null) return;

        var skillCards = player.Character.CardPool
            .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
            .Where(c => c.Type == CardType.Skill && c.Rarity != CardRarity.Basic && c.Rarity != CardRarity.Ancient)
            .ToList();

        if (skillCards.Count == 0) return;

        int amount = (int)Amount;
        for (int i = 0; i < amount; i++)
        {
            var rng = player.RunState.Rng.CombatCardGeneration;
            var selected = CardFactory.GetDistinctForCombat(player, skillCards, 1, rng).FirstOrDefault();
            if (selected == null) continue;

            var generated = selected.CreateClone();
            generated.SetToFreeThisTurn();

            // 方法签名：AddGeneratedCardToCombat(CardModel card, PileType newPileType, Player? creator, CardPilePosition position = CardPilePosition.Bottom)
            await CardPileCmd.AddGeneratedCardToCombat(generated, PileType.Hand, player);
        }

        Flash();
    }
}

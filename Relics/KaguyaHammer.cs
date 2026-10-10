using BaseLib.Abstracts;
using BaseLib.Utils;
using Kaguya.RelicPools;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.Relics
{
    [Pool(typeof(KaguyaRelicPool))]
    public sealed class KaguyaHammer : CustomRelicModel
    {
        private const int AttackThreshold = 6;

        private bool _isActivating;
        private int _attacksPlayedThisCombat;
        private CardModel? _attackToDouble;

        public override RelicRarity Rarity => RelicRarity.Ancient;

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new DynamicVar("Threshold", AttackThreshold)
        };

        public override string PackedIconPath => "res://images/relics/kaguya_hammer.png";
        protected override string PackedIconOutlinePath => "res://images/relics/kaguya_hammer_outline.png";
        protected override string BigIconPath => "res://images/relics/kaguya_hammer_big.png";

        public override bool ShowCounter => true;

        public override int DisplayAmount
        {
            get
            {
                if (!IsActivating)
                    return AttacksPlayedThisCombat;
                return AttackThreshold;
            }
        }

        private bool IsActivating
        {
            get => _isActivating;
            set
            {
                AssertMutable();
                _isActivating = value;
                UpdateDisplay();
            }
        }

        [SavedProperty]
        public int AttacksPlayedThisCombat
        {
            get => _attacksPlayedThisCombat;
            private set
            {
                AssertMutable();
                _attacksPlayedThisCombat = value % AttackThreshold;
                UpdateDisplay();
            }
        }

        private CardModel? AttackToDouble
        {
            get => _attackToDouble;
            set
            {
                AssertMutable();
                _attackToDouble = value;
            }
        }

        private void UpdateDisplay()
        {
            if (IsActivating)
                Status = RelicStatus.Normal;
            else
                Status = (AttacksPlayedThisCombat == AttackThreshold - 1) ? RelicStatus.Active : RelicStatus.Normal;
            InvokeDisplayAmountChanged();
        }

        private void NotifyAttackPlayed()
        {
            AttacksPlayedThisCombat++;
            if (AttacksPlayedThisCombat == 0)
            {
                TaskHelper.RunSafely(DoActivateVisuals());
            }
        }

        public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, MegaCrit.Sts2.Core.Entities.Cards.CardPlay? cardPlay)
        {
            if (!props.IsPoweredAttack())
                return 1m;
            if (cardSource == null)
                return 1m;
            if (dealer != Owner.Creature && dealer != Owner.Osty)
                return 1m;
            if (AttackToDouble == null)
            {
                CardPile? pile = cardSource.Pile;
                if ((pile == null || pile.Type != PileType.Play) && AttacksPlayedThisCombat == AttackThreshold - 1)
                    return 2m;
                return 1m;
            }
            if (cardSource == AttackToDouble)
                return 2m;
            return 1m;
        }

        public override Task BeforeCardPlayed(CardPlay cardPlay)
        {
            if (cardPlay.Card.Type != CardType.Attack)
                return Task.CompletedTask;
            if (cardPlay.Card.Owner != Owner)
                return Task.CompletedTask;

            NotifyAttackPlayed();
            if (AttacksPlayedThisCombat == 0)
            {
                AttackToDouble = cardPlay.Card;
            }
            return Task.CompletedTask;
        }

        public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            if (AttackToDouble == null)
                return Task.CompletedTask;
            if (cardPlay.Card != AttackToDouble)
                return Task.CompletedTask;

            AttackToDouble = null;
            return Task.CompletedTask;
        }

        private async Task DoActivateVisuals()
        {
            IsActivating = true;
            Flash();
            await Cmd.Wait(1f);
            IsActivating = false;
        }
    }
}


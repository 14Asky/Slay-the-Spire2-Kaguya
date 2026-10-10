using BaseLib.Abstracts;
using BaseLib.Utils;
using Kaguya.RelicPools;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.Relics
{
    [Pool(typeof(KaguyaRelicPool))]
    public sealed class FlatSheathCharm : CustomRelicModel
    {
        private const int MaxTriggers = 4;

        private int _remainingTriggers = MaxTriggers;

        public override RelicRarity Rarity => RelicRarity.Rare;

        public override bool ShowCounter => true;
        public override int DisplayAmount => _remainingTriggers;

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new CardsVar(1)
        };

        public override string PackedIconPath => "res://images/relics/bianmianqiao.png";
        protected override string PackedIconOutlinePath => "res://images/relics/bianmianqiao.png";
        protected override string BigIconPath => "res://images/relics/bianmianqiao.png";

        private int RemainingTriggers
        {
            get => _remainingTriggers;
            set
            {
                AssertMutable();
                _remainingTriggers = value;
                InvokeDisplayAmountChanged();
            }
        }

        public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
        {
            if (card.Owner != Owner) return;
            if (card.Type != CardType.Status) return;
            if (RemainingTriggers <= 0) return;

            RemainingTriggers--;
            Flash();

            await CardCmd.Exhaust(choiceContext, card);
            await CardPileCmd.Draw(choiceContext, 1, Owner);
        }

        public override Task BeforeCombatStart()
        {
            RemainingTriggers = MaxTriggers;
            return Task.CompletedTask;
        }
    }
}

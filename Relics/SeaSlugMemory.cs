using BaseLib.Abstracts;
using BaseLib.Utils;
using Kaguya.CardPools;
using Kaguya.RelicPools;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace Kaguya.Relics
{
    [Pool(typeof(KaguyaRelicPool))]
    public sealed class SeaSlugMemory : CustomRelicModel
    {
        public override RelicRarity Rarity => RelicRarity.Ancient;

        protected override IEnumerable<DynamicVar> CanonicalVars => new[]
        {
            new CardsVar(8)
        };

        public override string PackedIconPath => "res://images/relics/sea_slug_memory.png";
        protected override string PackedIconOutlinePath => "res://images/relics/sea_slug_memory.png";
        protected override string BigIconPath => "res://images/relics/sea_slug_memory.png";

        private const int MaxTransformCount = 8;
        private const int HpLoss = 16;

        public override async Task AfterObtained()
        {
            // 使用标准提示（无需自定义本地化键）
            var prefs = new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 0, MaxTransformCount)
            {
                Cancelable = true,
                RequireManualConfirmation = true
            };

            var selectedCards = (await CardSelectCmd.FromDeckForTransformation(Owner, prefs)).ToList();

            foreach (var card in selectedCards)
            {
                await CardCmd.TransformToRandom(card, Owner.RunState.Rng.Niche);
            }

            await CreatureCmd.LoseMaxHp(new ThrowingPlayerChoiceContext(), Owner.Creature, HpLoss, cardSource: null);
        }
    }
}

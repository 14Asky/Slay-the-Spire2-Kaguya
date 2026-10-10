using Godot;
using HarmonyLib;
using Kaguya.Relics;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Events;
using System.Collections.Generic;
using System.Linq;

namespace Kaguya
{
    public sealed class FushiAncient : AncientEventModel
    {
        public override Color ButtonColor => new Color(0.2f, 0.5f, 0.3f, 0.6f);
        public override Color DialogueColor => new Color("2E6B3E");

        private List<EventOption> OptionPool1 => new List<EventOption>
        {
            RelicOption<SeaSlugMemory>(),
            RelicOption<SeaSlugBlessing>()
        };

        private EventOption Option2 => RelicOption<TimelessBond>();

        private EventOption Option3 => RelicOption<SuperGashaponMachine>();

        public override IEnumerable<EventOption> AllPossibleOptions =>
            OptionPool1.Concat(new[] { Option2, Option3 });

        protected override AncientDialogueSet DefineDialogues()
        {
            return new AncientDialogueSet
            {
                FirstVisitEverDialogue = new AncientDialogue("", "", "") { VisitIndex = 0 },
                CharacterDialogues = new Dictionary<string, IReadOnlyList<AncientDialogue>>(),
                AgnosticDialogues = new AncientDialogue[] { new AncientDialogue("") }
            };
        }

        protected override IReadOnlyList<EventOption> GenerateInitialOptions()
        {
            var hasLonging = base.Owner.Relics.Any(r => r is EightThousandYearsLonging);
            if (hasLonging)
            {
                var option1 = Rng.NextItem(OptionPool1);
                var option2 = Option2;
                var option3 = Option3;
                return new List<EventOption> { option1, option2, option3 };
            }
            else
            {
                var option1 = RelicOption<SeaSlugMemory>();
                var option2 = RelicOption<SeaSlugBlessing>();
                var option3 = Option3;
                return new List<EventOption> { option1, option2, option3 };
            }
        }
    }
}

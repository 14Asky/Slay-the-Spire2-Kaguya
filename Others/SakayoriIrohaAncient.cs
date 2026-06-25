using Godot;
using HarmonyLib;
using Kaguya;
using Kaguya.Relics;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Random;
using System.Collections.Generic;
using System.Linq;

namespace Kaguya
{
    public sealed class SakayoriIrohaAncient : AncientEventModel
    {
        public override Color ButtonColor => new Color(0f, 0.5f, 0.8f, 0.6f);
        public override Color DialogueColor => new Color("1E3A5F");

        // 选项�?：PartingGift, Ideal, OrdinaryHappiness 中随机一�?
        private List<EventOption> OptionPool1 => new List<EventOption>
        {
            RelicOption<PartingGift>(),
            RelicOption<Ideal>(),
            RelicOption<OrdinaryHappiness>()
        };

        // 选项�?：FushiDoll, IrohaIpad, IrohaSchedule 中随机一�?
        private List<EventOption> OptionPool2 => new List<EventOption>
        {
            RelicOption<FushiDoll>(),
            RelicOption<IrohaIpad>(),
            RelicOption<IrohaSchedule>()
        };

        // 选项�?：固定为 GashaponMachineL3
        private List<EventOption> OptionPool3 => new List<EventOption>
        {
            RelicOption<GashaponMachineL3>()
        };

        public override IEnumerable<EventOption> AllPossibleOptions =>
            OptionPool1.Concat(OptionPool2).Concat(OptionPool3);

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
            var option1 = Rng.NextItem(OptionPool1);
            var option2 = Rng.NextItem(OptionPool2);
            var option3 = Rng.NextItem(OptionPool3);
            return new List<EventOption> { option1, option2, option3 };
        }
    }
}

// 补丁：覆�?Glory 章节的先古之民池
[HarmonyPatch(typeof(Glory), nameof(Glory.AllAncients), MethodType.Getter)]
public static class Glory_AllAncients_Patch
{
    static void Postfix(ref IEnumerable<AncientEventModel> __result)
    {
            if (!KaguyaContentFilter.ShouldShowKaguyaContent())
                return;
        __result = new AncientEventModel[]
        {
            ModelDb.AncientEvent<SakayoriIrohaAncient>(),
            ModelDb.AncientEvent<RokaAndMamiAncient>()
        };
    }
}
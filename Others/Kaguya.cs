using BaseLib.Audio;
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
    public sealed class KaguyaAncient : AncientEventModel
    {
        public override Color ButtonColor => new Color(0.08f, 0.04f, 0f, 0.75f);
        public override Color DialogueColor => new Color("33251E");

        private List<EventOption> OptionPool1 => new List<EventOption>
        {
            RelicOption<HappySynthesizerRelic>(),
            RelicOption<PerformanceCostume>(),
            RelicOption<TearsOfMemory>()
        };

        // 基础池（不含太阳头饰�?
        private List<EventOption> OptionPool2Base => new List<EventOption>
        {
            RelicOption<KaguyaHammer>(),
            RelicOption<GluttonousFlyingFish>(),
            RelicOption<MoonHeaddress>()
        };

        // 太阳头饰单独选项
        private List<EventOption> OptionPool2Sun => new List<EventOption>
        {
            RelicOption<SunHeaddress>()
        };

        private List<EventOption> OptionPool3 => new List<EventOption>
        {
            RelicOption<GashaponMachineL2>()
        };

        // 所有可能出现的选项（用于图鉴和对话生成�?
        public override IEnumerable<EventOption> AllPossibleOptions =>
            OptionPool1.Concat(OptionPool2Base).Concat(OptionPool2Sun).Concat(OptionPool3);
        public override void OnRoomEnter()
        {
            base.OnRoomEnter();
            ModAudio.PlaySound("res://audios/kaguya.ogg", volumeAdd: 15f);
        }

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

            // 选项2�?0%概率出现太阳头饰�?0%概率从基础池中随机
            EventOption option2;
            if (Rng.NextDouble() < 0.1)
            {
                option2 = OptionPool2Sun[0]; // 太阳头饰
            }
            else
            {
                option2 = Rng.NextItem(OptionPool2Base);
            }

            var option3 = Rng.NextItem(OptionPool3);
            return new List<EventOption> { option1, option2, option3 };
        }
    }
}
// 补丁保持不变
[HarmonyPatch(typeof(Hive), nameof(Hive.AllAncients), MethodType.Getter)]
public static class Hive_AllAncients_Patch
{
    static void Postfix(ref IEnumerable<AncientEventModel> __result)
    {
            if (!KaguyaContentFilter.ShouldShowKaguyaContent())
                return;
        __result = new AncientEventModel[]
        {
            ModelDb.AncientEvent<KaguyaAncient>(),
            ModelDb.AncientEvent<BlackOnyxAncient>()
        };
    }
}
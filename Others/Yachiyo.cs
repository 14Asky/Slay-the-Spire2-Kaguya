using BaseLib.Audio;
using Godot;
using HarmonyLib;
using Kaguya;
using Kaguya.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Random;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya
{
    public sealed class YachiyoAncient : Neow
    {
        public override Color ButtonColor => new Color(0f, 0.1f, 0.2f, 0.5f);
        public override Color DialogueColor => new Color("28454F");

        // 选项�?：三个遗物中随机一�?
        private List<EventOption> OptionPool1 => new List<EventOption>
        {
            RelicOption<YachiyoShrine>(),
            RelicOption<TsukuyomiShrine>(),
            RelicOption<KaguyaAccount>()
        };

        // 选项�?：三个遗物中随机一�?
        private List<EventOption> OptionPool2 => new List<EventOption>
        {
            RelicOption<LiveStreaming>(),
            RelicOption<CostumeSystem>(),
            RelicOption<ThoughtStrand>(),
            RelicOption<YacchoSpecialCare>()
        };

        // 选项�?：固定一个遗�?
        private List<EventOption> OptionPool3 => new List<EventOption>
        {
            RelicOption<GashaponMachineL1>()
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

        // 进入事件房间时给予“记忆碎片·起始�?
        public override void OnRoomEnter()
        {
            base.OnRoomEnter();
            // 播放事件音频
            ModAudio.PlaySound("res://audios/yachiyo.ogg", volumeAdd: 10f);
            TaskHelper.RunSafely(GiveMemoryFragment());
        }

        private async Task GiveMemoryFragment()
        {
            foreach (var player in Owner.RunState.Players)
            {
                if (!player.Relics.Any(r => r is MemoryFragmentStart))
                {
                    var relic = ModelDb.Relic<MemoryFragmentStart>().ToMutable();
                    await RelicCmd.Obtain(relic, player);
                }
            }
        }
    }

    // 章节补丁（根据需要修改，确保出现在第一层和第二层）
    [HarmonyPatch(typeof(Overgrowth), nameof(Overgrowth.AllAncients), MethodType.Getter)]
    public static class Overgrowth_AllAncients_Patch
    {
        static void Postfix(ref IEnumerable<AncientEventModel> __result)
        {
            if (!KaguyaContentFilter.ShouldShowKaguyaContent())
                return;
            __result = new[] { ModelDb.AncientEvent<YachiyoAncient>() };
        }
    }

    [HarmonyPatch(typeof(Underdocks), nameof(Underdocks.AllAncients), MethodType.Getter)]
    public static class Underdocks_AllAncients_Patch
    {
        static void Postfix(ref IEnumerable<AncientEventModel> __result)
        {
            if (!KaguyaContentFilter.ShouldShowKaguyaContent())
                return;
            __result = new[] { ModelDb.AncientEvent<YachiyoAncient>() };
        }
    }
}
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Audio.Debug;
using Kaguya.Cards;
using Kaguya.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Runs;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.Events
{
    public sealed class RainbowTelephonePole : EventModel
    {
        private int? _musicHandle;

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new StringVar("Boring", "枯燥"),
            new StringVar("BlockTheDoor", "堵门"),
            new StringVar("BabyRelic", "襁褓中的女婴")
        };

        public override bool IsAllowed(IRunState runState) => true;

        // 进入事件房间时播放音乐
        public override void OnRoomEnter()
        {
            base.OnRoomEnter();
            _musicHandle = NDebugAudioManager.Instance.Play("Conisch - Kaguya Is Born.ogg");
        }

        protected override IReadOnlyList<EventOption> GenerateInitialOptions()
        {
            return new EventOption[]
            {
                new EventOption(this, AcceptLight, InitialOptionKey("ACCEPT_LIGHT"),
                    HoverTipFactory.FromRelic<SwaddledInfant>()),
                new EventOption(this, RefuseButTake, InitialOptionKey("REFUSE_BUT_TAKE"),
                    HoverTipFactory.FromCardWithCardHoverTips<Boring>()),
            };
        }

        private async Task AcceptLight()
        {
            var relic = ModelDb.Relic<SwaddledInfant>().ToMutable();
            await RelicCmd.Obtain(relic, Owner);
            StopMusic();
            SetEventFinished(L10NLookup("RAINBOW_TELEPHONE_POLE.pages.ACCEPT_LIGHT.description"));
        }

        private async Task RefuseButTake()
        {
            var boring = Owner.RunState.CreateCard<Boring>(Owner);
            var addResult1 = await CardPileCmd.Add(boring, PileType.Deck);
            var blockTheDoor = Owner.RunState.CreateCard<BlockTheDoor>(Owner);
            var addResult2 = await CardPileCmd.Add(blockTheDoor, PileType.Deck);
            CardCmd.PreviewCardPileAdd(new[] { addResult1, addResult2 }, 2f);
            // 循环页面，音乐继续播放
            SetEventState(
                L10NLookup("RAINBOW_TELEPHONE_POLE.pages.INITIAL.description"),
                GenerateInitialOptions()
            );
        }

        private void StopMusic()
        {
            if (_musicHandle.HasValue)
            {
                NDebugAudioManager.Instance.Stop(_musicHandle.Value);
                _musicHandle = null;
            }
        }
    }

    // 补丁：将事件添加到第一层（密林）和第二层（底层）
    [HarmonyPatch(typeof(Overgrowth), nameof(Overgrowth.AllEvents), MethodType.Getter)]
    public static class Overgrowth_AllEvents_Patch
    {
        static void Postfix(ref IEnumerable<EventModel> __result)
        {
            if (!KaguyaContentFilter.ShouldShowKaguyaContent())
                return;
            __result = __result.Concat(new EventModel[]
            {
                ModelDb.Event<RainbowTelephonePole>(),
                ModelDb.Event<TaketoriMonogatariEvent>(),
                ModelDb.Event<MoonNews>()
            });
        }
    }

    [HarmonyPatch(typeof(Underdocks), nameof(Underdocks.AllEvents), MethodType.Getter)]
    public static class Underdocks_AllEvents_Patch
    {
        static void Postfix(ref IEnumerable<EventModel> __result)
        {
            if (!KaguyaContentFilter.ShouldShowKaguyaContent())
                return;
            __result = __result.Concat(new EventModel[]
            {
                ModelDb.Event<RainbowTelephonePole>(),
                ModelDb.Event<TaketoriMonogatariEvent>(),
                ModelDb.Event<MoonNews>()
            });
        }
    }
}
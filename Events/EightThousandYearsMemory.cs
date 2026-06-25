using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace Kaguya.Events
{
    public sealed class EightThousandYearsMemory : EventModel
    {
        private const int MaxTransformCount = 8;
        private const int HpLoss = 6;

        private int _currentTransform;
        private int? _musicHandle;
        private bool _isFinalStage;

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new HpLossVar(HpLoss)
        };

        public override bool IsAllowed(IRunState runState) => true;

        public override void OnRoomEnter()
        {
            base.OnRoomEnter();
            if (!_musicHandle.HasValue)
                _musicHandle = NDebugAudioManager.Instance.Play("FullMoon.mp3");
        }

        protected override IReadOnlyList<EventOption> GenerateInitialOptions()
        {
            _currentTransform = 0;
            _isFinalStage = false;
            return new List<EventOption>
            {
                new EventOption(this, StartTransforming, InitialOptionKey("ACCEPT")),
                new EventOption(this, Reject, InitialOptionKey("REJECT"),
                    HoverTipFactory.FromCardWithCardHoverTips<Normality>())
            };
        }

        // ---------- 音效与背景图 ----------
        private void PlaySound(string soundName)
        {
            if (!LocalContext.IsMe(Owner)) return;
            NDebugAudioManager.Instance.Play(soundName);
        }

        private void SetPortraitImage(string imagePath)
        {
            if (!LocalContext.IsMe(Owner)) return;
            var tex = PreloadManager.Cache.GetTexture2D(imagePath);
            if (tex != null) NEventRoom.Instance?.SetPortrait(tex);
        }

        private void UpdatePortraitByStage(int stage)
        {
            int num = stage + 1;
            SetPortraitImage($"res://images/events/e{num}.png");
        }

        private void StopMusicAndFinish(LocString text)
        {
            if (_musicHandle.HasValue)
            {
                NDebugAudioManager.Instance.Stop(_musicHandle.Value);
                _musicHandle = null;
            }
            SetEventFinished(text);
        }

        private async Task StartTransforming()
        {
            // 播放 e1.mp3
            PlaySound("e1.mp3");
            UpdatePortraitByStage(0);
            await ShowTransformPage();
        }

        private async Task ShowTransformPage()
        {
            string descKey;
            if (_isFinalStage)
            {
                descKey = "EIGHT_THOUSAND_YEARS_MEMORY.pages.TRANSFORM_DONE.description";
            }
            else
            {
                int pageNumber = _currentTransform + 1;
                descKey = $"EIGHT_THOUSAND_YEARS_MEMORY.pages.TRANSFORM_{pageNumber}.description";
            }
            LocString pageDesc = L10NLookup(descKey);

            var options = new List<EventOption>();

            if (_isFinalStage)
            {
                // 最终阶段仅有“停止”选项
                options.Add(new EventOption(this, StopTransforming,
                    "EIGHT_THOUSAND_YEARS_MEMORY.pages.TRANSFORM.options.STOP"));
            }
            else
            {
                // 变化阶段：始终有“变化一张牌”
                options.Add(new EventOption(this, DoTransform,
                    "EIGHT_THOUSAND_YEARS_MEMORY.pages.TRANSFORM.options.TRANSFORM"));

                // 从第4次变化页面开始（_currentTransform >= 3）才显示“停止”
                if (_currentTransform >= 3)
                {
                    options.Add(new EventOption(this, StopTransforming,
                        "EIGHT_THOUSAND_YEARS_MEMORY.pages.TRANSFORM.options.STOP"));
                }
            }

            SetEventState(pageDesc, options);
        }

        private async Task DoTransform()
        {
            // 扣除生命上限
            await CreatureCmd.LoseMaxHp(new ThrowingPlayerChoiceContext(), Owner.Creature, HpLoss, false);

            // 选择一张牌进行变化
            var prefs = new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 1, 1)
            {
                Cancelable = false,
                RequireManualConfirmation = true
            };
            var selected = (await CardSelectCmd.FromDeckForTransformation(Owner, prefs)).ToList();
            if (selected.Count == 1)
                await CardCmd.TransformToRandom(selected[0], Owner.RunState.Rng.Niche);

            // 计数器递增
            _currentTransform++;

            // 播放音效：第 k 次变化播放 e(k+1).mp3（即 e2~e9）
            int soundIndex = _currentTransform + 1;
            PlaySound($"e{soundIndex}.mp3");

            // 换背景图
            UpdatePortraitByStage(_currentTransform);

            if (_currentTransform < MaxTransformCount)
            {
                await ShowTransformPage();
            }
            else
            {
                // 第八次完成，进入最终阶段
                _isFinalStage = true;
                await ShowTransformPage();
            }
        }

        private async Task StopTransforming()
        {
            if (_isFinalStage)
            {
                // 最终阶段：给予神化
                var apotheosisCard = Owner.RunState.CreateCard<Apotheosis>(Owner);
                if (apotheosisCard != null)
                {
                    var addResult = await CardPileCmd.Add(apotheosisCard, PileType.Deck);
                    CardCmd.PreviewCardPileAdd(new[] { addResult }, 2f);
                }
                StopMusicAndFinish(L10NLookup("EIGHT_THOUSAND_YEARS_MEMORY.pages.TRANSFORM_END.description"));
            }
            else
            {
                StopMusicAndFinish(L10NLookup("EIGHT_THOUSAND_YEARS_MEMORY.pages.TRANSFORM_STOPPED.description"));
            }
            await Task.CompletedTask;
        }

        private async Task Reject()
        {
            await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), Owner.Creature, HpLoss,
                ValueProp.Unblockable | ValueProp.Unpowered, null, null);

            var normality = Owner.RunState.CreateCard<Normality>(Owner);
            var addResult = await CardPileCmd.Add(normality, PileType.Deck);
            CardCmd.PreviewCardPileAdd(new[] { addResult }, 2f);
            await Cmd.Wait(0.5f);

            StopMusicAndFinish(L10NLookup("EIGHT_THOUSAND_YEARS_MEMORY.pages.REJECT.description"));
        }
    }

    [HarmonyPatch(typeof(Glory), nameof(Glory.AllEvents), MethodType.Getter)]
    public static class Glory_AllEvents_Patch_EightThousandYears
    {
        static void Postfix(ref IEnumerable<EventModel> __result)
        {
            if (!KaguyaContentFilter.ShouldShowKaguyaContent())
                return;
            __result = __result.Concat(new[] { ModelDb.Event<EightThousandYearsMemory>() });
        }
    }
}

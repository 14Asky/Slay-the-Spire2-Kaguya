using Godot;
using Kaguya;
using HarmonyLib;
using Kaguya.Cards;
using Kaguya.Events;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace Kaguya.Events
{
    public sealed class VirtualYouTuberHaruku : EventModel
    {
        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new HealVar(33)
        };

        protected override IReadOnlyList<EventOption> GenerateInitialOptions()
        {
            return new[]
            {
                new EventOption(this, HotSpringTherapy, InitialOptionKey("HOT_SPRING")),
                new EventOption(this, PsychologicalCounseling, InitialOptionKey("COUNSELING"),
                    HoverTipFactory.FromCardWithCardHoverTips<Boring>()), // ÃÌº”ø›‘Ôø®≈∆–¸∏°Ã· æ
                new EventOption(this, GentleListening, InitialOptionKey("LISTENING"))
            };
        }

        private async Task HotSpringTherapy()
        {
            int maxHp = Owner.Creature.MaxHp;
            int healAmount = maxHp / 3;
            await CreatureCmd.Heal(Owner.Creature, healAmount, true);
            SetEventFinished(L10NLookup("VIRTUAL_YOUTUBER_HARUKU.pages.HOT_SPRING.description"));
        }

        private async Task PsychologicalCounseling()
        {
            var prefs = new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 2);
            var cards = await CardSelectCmd.FromDeckForRemoval(prefs: prefs, player: Owner);
            foreach (var card in cards)
            {
                await CardPileCmd.RemoveFromDeck(card);
            }
            // ÃÌº”“ª’≈ø›‘Ôø®≈∆≤¢‘§¿¿
            var boring = Owner.RunState.CreateCard<Boring>(Owner);
            var addResult = await CardPileCmd.Add(boring, PileType.Deck);
            CardCmd.PreviewCardPileAdd(new[] { addResult }, 2f);
            SetEventFinished(L10NLookup("VIRTUAL_YOUTUBER_HARUKU.pages.COUNSELING.description"));
        }

        private Task GentleListening()
        {
            int upgradeCount = 2;
            var upgradableCards = PileType.Deck.GetPile(Owner).Cards
                .Where(c => c.IsUpgradable)
                .ToList()
                .StableShuffle(Owner.RunState.Rng.Niche)
                .Take(upgradeCount);
            foreach (var card in upgradableCards)
            {
                CardCmd.Upgrade(card);
            }
            SetEventFinished(L10NLookup("VIRTUAL_YOUTUBER_HARUKU.pages.LISTENING.description"));
            return Task.CompletedTask;
        }
    }
}

[HarmonyPatch(typeof(Hive), nameof(Hive.AllEvents), MethodType.Getter)]
public static class Hive_AllEvents_Patch
{
    static void Postfix(ref IEnumerable<EventModel> __result)
    {
            if (!KaguyaContentFilter.ShouldShowKaguyaContent())
                return;
        __result = __result.Concat(new EventModel[]
        {
            ModelDb.Event<VirtualYouTuberHaruku>(),
            ModelDb.Event<VirtualYouTuberTereLily>()
        });
    }
}

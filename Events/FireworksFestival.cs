using Godot;
using HarmonyLib;
using Kaguya.Potions;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace Kaguya.Events
{
    public sealed class FireworksFestival : EventModel
    {
        private const int GoldCost = 50;
        private const int HealAmount = 10;

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new GoldVar(GoldCost),
            new HealVar(HealAmount)
        };

        public override bool IsAllowed(IRunState runState) => true;

        protected override IReadOnlyList<EventOption> GenerateInitialOptions()
        {
            return new List<EventOption>
            {
                new EventOption(this, AttendFestival, InitialOptionKey("ATTEND")),
                new EventOption(this, Decline, InitialOptionKey("DECLINE"))
            };
        }

        private async Task AttendFestival()
        {
            if (Owner.Gold < GoldCost)
            {
                SetEventFinished(L10NLookup("FIREWORKS_FESTIVAL.pages.NOT_ENOUGH_GOLD.description"));
                return;
            }

            await PlayerCmd.LoseGold(GoldCost, Owner);

            SetEventState(
                L10NLookup("FIREWORKS_FESTIVAL.pages.REWARDS.description"),
                new List<EventOption>
                {
                    new EventOption(this, RemoveCardReward, "FIREWORKS_FESTIVAL.pages.REWARDS.options.REMOVE_CARD", HoverTipFactory.FromPotion<AppleCandy>()),
                    new EventOption(this, UpgradeCardReward, "FIREWORKS_FESTIVAL.pages.REWARDS.options.UPGRADE_CARD", HoverTipFactory.FromPotion<Pancake>()),
                    new EventOption(this, HealReward, "FIREWORKS_FESTIVAL.pages.REWARDS.options.HEAL", HoverTipFactory.FromPotion<Smoothie>())
                }
            );
        }

        private async Task RemoveCardReward()
        {
            var prefs = new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1);
            var selected = await CardSelectCmd.FromDeckForRemoval(Owner, prefs);
            var card = selected.FirstOrDefault();
            if (card != null)
                await CardPileCmd.RemoveFromDeck(card);

            var appleCandy = ModelDb.Potion<AppleCandy>().ToMutable();
            await RewardsCmd.OfferCustom(Owner, new List<Reward>
            {
                new PotionReward(appleCandy, Owner)
            });
            SetEventFinished(L10NLookup("FIREWORKS_FESTIVAL.pages.REMOVE_CARD.description"));
        }

        private async Task UpgradeCardReward()
        {
            var prefs = new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, 1);
            var selected = await CardSelectCmd.FromDeckForUpgrade(Owner, prefs);
            var card = selected.FirstOrDefault();
            if (card != null)
                CardCmd.Upgrade(card);

            var pancake = ModelDb.Potion<Pancake>().ToMutable();
            await RewardsCmd.OfferCustom(Owner, new List<Reward>
            {
                new PotionReward(pancake, Owner)
            });
            SetEventFinished(L10NLookup("FIREWORKS_FESTIVAL.pages.UPGRADE_CARD.description"));
        }

        private async Task HealReward()
        {
            await CreatureCmd.Heal(Owner.Creature, HealAmount);

            var smoothie = ModelDb.Potion<Smoothie>().ToMutable();
            await RewardsCmd.OfferCustom(Owner, new List<Reward>
            {
                new PotionReward(smoothie, Owner)
            });
            SetEventFinished(L10NLookup("FIREWORKS_FESTIVAL.pages.HEAL.description"));
        }

        private Task Decline()
        {
            SetEventFinished(L10NLookup("FIREWORKS_FESTIVAL.pages.DECLINE.description"));
            return Task.CompletedTask;
        }
    }

    // 将事件添加到 Glory 章节的事件池
    [HarmonyPatch(typeof(Glory), nameof(Glory.AllEvents), MethodType.Getter)]
    public static class Glory_AllEvents_Patch
    {
        static void Postfix(ref IEnumerable<EventModel> __result)
        {
            if (!KaguyaContentFilter.ShouldShowKaguyaContent())
                return;
            __result = __result.Concat(new[] { ModelDb.Event<FireworksFestival>() });
        }
    }
}

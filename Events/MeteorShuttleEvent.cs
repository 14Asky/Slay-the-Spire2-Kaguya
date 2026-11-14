using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace Kaguya.Events
{
    public sealed class MeteorShuttleEvent : EventModel
    {
        private const int HpLossNormal = 20;
        private const int RemovalCount = 2;
        public override bool IsShared => true;

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new HpLossVar(HpLossNormal),
            new CardsVar(RemovalCount)
        };

        public override bool IsAllowed(IRunState runState) => runState.Players.Count <= 1;

        protected override IReadOnlyList<EventOption> GenerateInitialOptions()
        {
            return new List<EventOption>
            {
                new EventOption(this, Resist, InitialOptionKey("RESIST")),
                new EventOption(this, Submit, InitialOptionKey("SUBMIT"))
            };
        }

        // 选项A：试图反抗 - 失去一半生命上限，然后跳转到第一层（Act 1）
        private async Task Resist()
        {
            int currentMaxHp = Owner.Creature.MaxHp;
            int halfMaxHp = (int)Math.Ceiling(currentMaxHp / 2.0);
            await CreatureCmd.LoseMaxHp(new ThrowingPlayerChoiceContext(), Owner.Creature, halfMaxHp, cardSource: null);

            SetEventFinished(L10NLookup("METEOR_SHUTTLE_EVENT.pages.RESIST.description"));

            await Cmd.Wait(0.3f);
            NMapScreen.Instance.SetTravelEnabled(enabled: true);
            await RunManager.Instance.EnterAct(0);
        }

        // 选项B：不作反抗 - 失去20点生命，从牌组中删除1张牌
        private async Task Submit()
        {
            // 失去20点生命（不可格挡、无视护甲）
            await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), Owner.Creature, HpLossNormal,
                ValueProp.Unblockable | ValueProp.Unpowered, null, null);

            // 从牌组中选择一张牌并删除
            var prefs = new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 2, 2)
            {
                Cancelable = false,
                RequireManualConfirmation = true
            };
            var selectedCards = await CardSelectCmd.FromDeckForRemoval(Owner, prefs);
            var cardToRemove = selectedCards.FirstOrDefault();
            if (cardToRemove != null)
            {
                await CardPileCmd.RemoveFromDeck(cardToRemove);
            }

            SetEventFinished(L10NLookup("METEOR_SHUTTLE_EVENT.pages.SUBMIT.description"));
        }
    }
}

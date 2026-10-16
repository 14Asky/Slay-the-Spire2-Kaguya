using BaseLib.Abstracts;
using BaseLib.Utils;
using Kaguya.HinaMods;
using Kaguya.HinaMods.Cards;
using Kaguya.HinaMods.Character;
using Kaguya.RelicPools;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.TestSupport;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.Relics
{
    [Pool(typeof(KaguyaRelicPool))]
    public sealed class SunHeaddress : CustomRelicModel
    {
        private const int MaxCardsToPlay = 168;

        public override RelicRarity Rarity => RelicRarity.Ancient;

        protected override IEnumerable<DynamicVar> CanonicalVars => new[]
        {
            new EnergyVar(2)
        };

        protected override IEnumerable<IHoverTip> ExtraHoverTips => new[]
        {
            HoverTipFactory.ForEnergy(this)
        };

        public override string PackedIconPath => "res://images/relics/sun_headdress.png";
        protected override string PackedIconOutlinePath => "res://images/relics/sun_headdress_outline.png";
        protected override string BigIconPath => "res://images/relics/sun_headdress_big.png";

        public override decimal ModifyMaxEnergy(Player player, decimal amount)
        {
            if (player != Owner) return amount;
            return amount + DynamicVars.Energy.BaseValue;
        }

        // 自动打牌阶段（每回合触发，接管全部回合）
        public override async Task AfterAutoPrePlayPhaseEnteredLate(PlayerChoiceContext choiceContext, Player player)
        {
            if (player != Owner) return;
            ICombatState combatState = player.Creature.CombatState;
            Flash();

            bool isKaguya = Owner.Character is HinaCharacter;

            int cardsPlayed = 0;
            using (CardSelectCmd.PushSelector(new SunCardSelector()))
            {
                while (cardsPlayed < MaxCardsToPlay)
                {
                    if (CombatManager.Instance.IsOverOrEnding) break;
                    var hand = PileType.Hand.GetPile(Owner).Cards.ToList();
                    if (hand.Count == 0) break;

                    bool played = false;

                    if (isKaguya)
                    {
                        int kaguyaPlayed = await TryPlayKaguyaLogic(choiceContext, hand, combatState);
                        cardsPlayed += kaguyaPlayed;
                        played = kaguyaPlayed > 0;
                    }
                    else
                    {
                        int defaultPlayed = await TryPlayDefaultLogic(choiceContext, hand, combatState);
                        cardsPlayed += defaultPlayed;
                        played = defaultPlayed > 0;
                    }

                    if (!played) break;
                }
            }

            if (cardsPlayed > 0)
            {
                bool isFull = cardsPlayed >= MaxCardsToPlay;
                LocString line = isFull
                    ? new LocString("relics", "SUN_HEADDRESS.warning")
                    : new LocString("relics", "SUN_HEADDRESS.approval");
                TalkCmd.Play(line, Owner.Creature, VfxColor.Purple);
            }
        }

        // ====================== 辉夜出牌逻辑 ======================

        private async Task<int> TryPlayKaguyaLogic(
            PlayerChoiceContext choiceContext, List<CardModel> hand,
            ICombatState combatState)
        {
            // P1: SUPPORT 链 —— 打出 SUPPORT 牌，紧接着打同类型非 SUPPORT 牌
            var supportCard = hand.FirstOrDefault(c => HasCustomTag(c, CustomCardTags.SUPPORT) && c.CanPlay());
            if (supportCard != null)
            {
                await PlayCard(choiceContext, supportCard, combatState);
                int played = 1;

                if (CombatManager.Instance.IsOverOrEnding) return played;

                var freshHand = PileType.Hand.GetPile(Owner).Cards.ToList();
                var followCard = freshHand.FirstOrDefault(c =>
                    !HasCustomTag(c, CustomCardTags.SUPPORT) &&
                    c.Type == supportCard.Type &&
                    c.CanPlay());

                if (followCard != null)
                {
                    await PlayCard(choiceContext, followCard, combatState);
                    played++;
                    return played;
                }
                return played;
            }

            // P2 + P3: SINGER 优先 + 基础优先级
            var playable = hand.Where(c => c.CanPlay()).ToList();
            if (playable.Count == 0) return 0;

            CardModel? bestCard = null;
            bestCard ??= playable.FirstOrDefault(c => HasCustomTag(c, CustomCardTags.ECHO) && c.Type == CardType.Power && c.EnergyCost.GetAmountToSpend() <= 2);
            bestCard ??= playable.FirstOrDefault(c => HasCustomTag(c, CustomCardTags.ECHO) && c.Type == CardType.Skill);
            bestCard ??= playable.FirstOrDefault(c => HasCustomTag(c, CustomCardTags.ECHO) && c.Type == CardType.Attack);
            bestCard ??= playable.FirstOrDefault(c => c.Type == CardType.Power && c.EnergyCost.GetAmountToSpend() <= 2);
            bestCard ??= playable.FirstOrDefault(c => c.Type == CardType.Skill);
            bestCard ??= playable.FirstOrDefault(c => c.Type == CardType.Attack);

            if (bestCard != null)
            {
                await PlayCard(choiceContext, bestCard, combatState);
                return 1;
            }

            return 0;
        }

        // ====================== 原版出牌逻辑 ======================

        private async Task<int> TryPlayDefaultLogic(
            PlayerChoiceContext choiceContext, List<CardModel> hand,
            ICombatState combatState)
        {
            // 1. 伙伴配对
            var partnerCard = hand.FirstOrDefault(c => c.Tags.Contains((CardTag)1004) && c.CanPlay());
            if (partnerCard != null)
            {
                var matchingNonPartner = hand.FirstOrDefault(c =>
                    !c.Tags.Contains((CardTag)1004) &&
                    c.Type == partnerCard.Type &&
                    c.CanPlay());
                if (matchingNonPartner != null)
                {
                    await PlayCard(choiceContext, matchingNonPartner, combatState);
                    int played = 1;

                    if (!CombatManager.Instance.IsOverOrEnding)
                    {
                        var freshPartner = PileType.Hand.GetPile(Owner).Cards
                            .FirstOrDefault(c => c == partnerCard || (c.Tags.Contains((CardTag)1004) && c.CanPlay()));
                        if (freshPartner != null)
                        {
                            await PlayCard(choiceContext, freshPartner, combatState);
                            played++;
                            return played;
                        }
                    }
                    return played;
                }
            }

            // 2. 打攻击牌
            var attackCard = hand.FirstOrDefault(c => c.Type == CardType.Attack && c.CanPlay());
            if (attackCard != null)
            {
                await PlayCard(choiceContext, attackCard, combatState);
                return 1;
            }

            // 3. 打其他可打牌
            var anyCard = hand.FirstOrDefault(c => c.CanPlay());
            if (anyCard != null)
            {
                await PlayCard(choiceContext, anyCard, combatState);
                return 1;
            }

            return 0;
        }

        // ====================== 通用辅助方法 ======================

        private static bool HasCustomTag(CardModel card, string tag)
        {
            if (card is HinaModsCard hinaCard)
                return hinaCard.CustomTags?.Contains(tag) == true;
            return false;
        }

        private async Task PlayCard(PlayerChoiceContext choiceContext, CardModel card, ICombatState combatState)
        {
            if (!card.CanPlay()) return;
            if (CombatManager.Instance.IsOverOrEnding) return;
            Creature target = GetTarget(card, combatState);
            await card.SpendResources();
            await CardCmd.AutoPlay(choiceContext, card, target, AutoPlayType.Default, skipXCapture: true);
        }

        private Creature GetTarget(CardModel card, ICombatState combatState)
        {
            Rng combatTargets = Owner.RunState.Rng.CombatTargets;
            return card.TargetType switch
            {
                TargetType.AnyEnemy => combatState.HittableEnemies.FirstOrDefault(),
                TargetType.AnyAlly => combatTargets.NextItem(combatState.Allies.Where(c => c != null && c.IsAlive && c.IsPlayer && c != Owner.Creature)),
                TargetType.AnyPlayer => Owner.Creature,
                _ => null,
            };
        }
    }

    public class SunCardSelector : ICardSelector
    {
        public Task<IEnumerable<CardModel>> GetSelectedCards(IEnumerable<CardModel> options, int minSelect, int maxSelect)
        {
            return Task.FromResult(options.Take(maxSelect));
        }

        public CardRewardSelection GetSelectedCardReward(IReadOnlyList<CardCreationResult> options, IReadOnlyList<CardRewardAlternative> alternatives)
        {
            return default(CardRewardSelection);
        }

        public Task<Creature> SelectTarget(PlayerChoiceContext context, TargetType targetType, IEnumerable<Creature> validTargets, CardModel card)
        {
            return Task.FromResult(validTargets.FirstOrDefault());
        }
    }
}

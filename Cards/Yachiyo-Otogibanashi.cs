using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace Kaguya.Cards
{
    public sealed class YachiyoOtogibanashi : CardModel
    {
        public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

        protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new CardsVar(3) };
        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new[] { HoverTipFactory.FromCard<Otogibanashi>(base.IsUpgraded) };

        public YachiyoOtogibanashi() : base(2, CardType.Skill, CardRarity.Token, TargetType.Self) { }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            // 1. 抽牌直到手牌上限
            int currentHandCount = PileType.Hand.GetPile(Owner).Cards.Count;
            int drawCount = 10 - currentHandCount;
            if (drawCount > 0)
            {
                await CardPileCmd.Draw(choiceContext, drawCount, Owner);
            }

            // 2. 每抽3张牌触发效果
            int triggerCount = drawCount / 2;
            if (triggerCount > 0)
            {
                // 扣除生命上限（每触发一次3点）
                await CreatureCmd.LoseMaxHp(choiceContext, Owner.Creature, 1 * triggerCount, cardSource: this);

                // 从手牌中选择最多 triggerCount 张牌增加重放次数
                var handCards = PileType.Hand.GetPile(Owner).Cards.ToList();
                if (handCards.Count > 0)
                {
                    var prefs = new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 0, triggerCount);
                    var selected = await CardSelectCmd.FromHand(choiceContext, Owner, prefs, null, this);
                    var chosen = selected.ToList();

                    foreach (var card in chosen)
                    {
                        card.BaseReplayCount += 1;
                        CardCmd.Preview(card);
                    }
                }
            }

            // 3. 将一张 Remember 加入抽牌堆（升级版则加入升级后的版本）
            var OtogibanashiCard = CombatState.CreateCard<Otogibanashi>(Owner);
            if (OtogibanashiCard != null)
            {
                if (IsUpgraded)
                {
                    CardCmd.Upgrade(OtogibanashiCard);
                }
                await CardPileCmd.AddGeneratedCardToCombat(OtogibanashiCard, PileType.Draw, Owner);
            }
        }

        protected override void OnUpgrade()
        {
            // 升级效果已在 OnPlay 中处理
        }
    }
}

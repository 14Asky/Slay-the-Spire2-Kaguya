using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.Cards
{
    public sealed class ILoveMyself : CardModel
    {
        public override IEnumerable<CardKeyword> CanonicalKeywords => new[]
        {
            CardKeyword.Exhaust,
            CardKeyword.Retain
        };

        protected override IEnumerable<DynamicVar> CanonicalVars => new[]
        {
            new DamageVar(4, ValueProp.Move)
        };

        public ILoveMyself() : base(2, CardType.Attack, CardRarity.Ancient, TargetType.Self) { }
        public override IEnumerable<CardTag> Tags => new[] { (CardTag)1001 };

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            // 收集所有牌堆中的诅咒牌（手牌、抽牌堆、弃牌堆）
            var handCurses = PileType.Hand.GetPile(Owner).Cards
                .Where(c => c.Type == CardType.Curse)
                .ToList();
            var drawCurses = PileType.Draw.GetPile(Owner).Cards
                .Where(c => c.Type == CardType.Curse)
                .ToList();
            var discardCurses = PileType.Discard.GetPile(Owner).Cards
                .Where(c => c.Type == CardType.Curse)
                .ToList();

            var allCurses = handCurses.Concat(drawCurses).Concat(discardCurses).ToList();

            // 消耗所有诅咒牌
            foreach (var curse in allCurses)
            {
                await CardCmd.Exhaust(choiceContext, curse);
            }

            // 获取消耗堆中所有牌的数量
            var exhaustPile = PileType.Exhaust.GetPile(Owner);
            int exhaustCount = exhaustPile.Cards.Count;
            int damage = (int)DynamicVars["Damage"].BaseValue;

            if (exhaustCount > 0)
            {
                // 使用多段攻击，确保每段独立享受力量加成
                await DamageCmd.Attack(damage)
                    .WithHitCount(exhaustCount)
                    .FromCard(this, cardPlay)
                    .TargetingRandomOpponents(CombatState)
                    .WithHitFx("vfx/vfx_attack_blunt", null, "blunt_attack.mp3")
                    .Execute(choiceContext);
            }
        }

        protected override void OnUpgrade()
        {
            RemoveKeyword(CardKeyword.Exhaust);
        }
    }
}
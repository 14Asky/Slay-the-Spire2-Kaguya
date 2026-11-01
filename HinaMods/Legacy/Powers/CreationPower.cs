using BaseLib.Abstracts;
using Godot;
using Kaguya.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.Powers
{
    public sealed class LegacyCreationPower : CustomPowerModel
    {
        private class LegacyData
        {
            public CardType? LastPlayedCardType;
        }

        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Counter;
        public override PowerInstanceType InstanceType => PowerInstanceType.None;
        public override bool AllowNegative => false;

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new EnergyVar(2)
        };

        public override string CustomPackedIconPath => "res://images/powers/creation_power.png";
        public override string CustomBigIconPath => "res://images/powers/creation_power.png";

        protected override object InitInternalData() => new LegacyData();

        public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            if (cardPlay.Card.Owner != Owner.Player) return;
            GetInternalData<LegacyData>().LastPlayedCardType = cardPlay.Card.Type;
            await Task.CompletedTask;
        }

        public override async Task AfterApplied(Creature applier, CardModel cardSource)
        {
            await Task.CompletedTask;
        }

        public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature applier, CardModel cardSource)
        {
            if (power.GetType() != GetType()) return;
            if (Amount >= 10)
            {
                var lastType = GetInternalData<LegacyData>().LastPlayedCardType;
                List<CardModel> Candidates;

                // 只有当上一张牌是攻击、技能或能力时，才使用对应的类型池
                if (lastType.HasValue &&
                    (lastType.Value == CardType.Attack || lastType.Value == CardType.Skill || lastType.Value == CardType.Power))
                {
                    Candidates = GetSongCandidatesByType(lastType.Value);
                }
                else
                {
                    // 否则（含从未出牌或打出状态/诅咒等）混合全部9首歌曲
                    Candidates = GetSongCandidatesByType(CardType.Attack)
                        .Concat(GetSongCandidatesByType(CardType.Skill))
                        .Concat(GetSongCandidatesByType(CardType.Power))
                        .ToList();
                }

                var rng = Owner.Player.RunState.Rng.CombatCardGeneration;
                var selected = Candidates.Distinct().TakeRandom(1, rng).ToList();

                if (selected.Count > 0)
                {
                    bool shouldUpgrade = Owner.GetPower<LegacyLivePerformancePower>() != null;
                    foreach (var template in selected)
                    {
                        var card = Owner.Player.Creature.CombatState.CreateCard(template, Owner.Player);
                        if (shouldUpgrade)
                        {
                            CardCmd.Upgrade(card);
                        }
                        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner.Player);
                    }
                }

                int energyGain = (int)DynamicVars["Energy"].BaseValue;
                await PlayerCmd.GainEnergy(energyGain, Owner.Player);
                await PowerCmd.Remove(this);
            }
        }

        private List<CardModel> GetSongCandidatesByType(CardType type)
        {
            var list = new List<CardModel>();
            switch (type)
            {
                case CardType.Attack:
                    list.Add(ModelDb.Card<ILoveMyself>());
                    list.Add(ModelDb.Card<ExOtogibanashi>());
                    list.Add(ModelDb.Card<StarlitSea>());
                    break;
                case CardType.Skill:
                    list.Add(ModelDb.Card<WorldPrincess>());
                    list.Add(ModelDb.Card<Remember>());
                    list.Add(ModelDb.Card<Melt>());
                    break;
                case CardType.Power:
                    list.Add(ModelDb.Card<HappySynthesizer>());
                    list.Add(ModelDb.Card<TransientSymphony>());
                    list.Add(ModelDb.Card<Reply>());
                    break;
            }
            return list;
        }
    }
}
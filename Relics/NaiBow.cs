using BaseLib.Abstracts;
using BaseLib.Utils;
using Kaguya.RelicPools;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.Relics
{
    [Pool(typeof(KaguyaRelicPool))]
    public sealed class NaiBow : CustomRelicModel
    {
        private int _attacksPlayedThisTurn;
        private bool _hasStunnedThisCombat; // 标记本场战斗是否已触发过击晕
        private const int RequiredAttacks = 6;
        private const int DamagePerProc = 10;

        public override RelicRarity Rarity => RelicRarity.Ancient;
        public override bool ShowCounter => CombatManager.Instance.IsInProgress;
        public override int DisplayAmount => _attacksPlayedThisTurn % RequiredAttacks;

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new DamageVar(DamagePerProc, ValueProp.Unpowered | ValueProp.Unblockable)
        };

        public override string PackedIconPath => "res://images/relics/nai_bow.png";
        protected override string PackedIconOutlinePath => "res://images/relics/nai_bow_outline.png";
        protected override string BigIconPath => "res://images/relics/nai_bow_big.png";

        public override Task BeforeCombatStart()
        {
            _attacksPlayedThisTurn = 0;
            _hasStunnedThisCombat = false;
            UpdateDisplay();
            return Task.CompletedTask;
        }

        public override Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
        {
            if (side == Owner.Creature.Side)
            {
                _attacksPlayedThisTurn = 0;
                UpdateDisplay();
            }
            return Task.CompletedTask;
        }

        public override Task AfterCombatEnd(CombatRoom _)
        {
            _attacksPlayedThisTurn = 0;
            _hasStunnedThisCombat = false;
            UpdateDisplay();
            return Task.CompletedTask;
        }

        public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
        {
            if (cardPlay.Card.Owner != Owner) return;
            if (!CombatManager.Instance.IsInProgress) return;

            if (cardPlay.Card.Type == CardType.Attack)
            {
                _attacksPlayedThisTurn++;
                UpdateDisplay();

                if (_attacksPlayedThisTurn % RequiredAttacks == 0)
                {
                    var enemies = Owner.Creature.CombatState.HittableEnemies;
                    if (enemies.Count > 0)
                    {
                        var target = Owner.RunState.Rng.CombatTargets.NextItem(enemies);
                        if (target != null)
                        {
                            Flash();
                            // 造成伤害
                            await CreatureCmd.Damage(context, target, DamagePerProc, ValueProp.Unpowered | ValueProp.Unblockable | ValueProp.SkipHurtAnim, Owner.Creature, null, null);

                            // 击晕效果：每场战斗仅一次
                            if (!_hasStunnedThisCombat)
                            {
                                _hasStunnedThisCombat = true;
                                await CreatureCmd.Stun(target);
                            }
                        }
                    }
                }
            }
        }

        private void UpdateDisplay()
        {
            bool isNextActivating = (_attacksPlayedThisTurn % RequiredAttacks) == (RequiredAttacks - 1);
            Status = isNextActivating ? RelicStatus.Active : RelicStatus.Normal;
            InvokeDisplayAmountChanged();
        }
    }
}
using BaseLib.Abstracts;
using BaseLib.Utils;
using Kaguya.Powers;
using Kaguya.RelicPools;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.Relics
{
    [Pool(typeof(KaguyaRelicPool))]
    public sealed class ElectronicKeyboard : CustomRelicModel
    {
        private const int CreationAmount = 1;

        /// <summary>本场战斗「已打出过哪些卡牌类型」的位掩码（bit = (int)CardType）。</summary>
        private int _playedTypesMask;

        public override RelicRarity Rarity => RelicRarity.Uncommon;

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new PowerVar<CreationPower>(CreationAmount)
        };

        protected override IEnumerable<IHoverTip> ExtraHoverTips => new[]
        {
            HoverTipFactory.FromPower<CreationPower>()
        };

        public override string PackedIconPath => "res://images/relics/dianziqing.png";
        protected override string PackedIconOutlinePath => "res://images/relics/dianziqing.png";
        protected override string BigIconPath => "res://images/relics/dianziqing.png";

        /// <summary>
        /// 本场战斗的进度必须走 [SavedProperty]，否则联机重连 / 战斗中重新加入时进度会丢，
        /// 已经打过的类型会再触发一次（多给创作）→ 各端状态分叉。
        /// HashSet 不被 BaseLib 的存档类型支持，这里用 int 位掩码。
        /// </summary>
        [SavedProperty]
        private int PlayedTypesMask
        {
            get => _playedTypesMask;
            set
            {
                AssertMutable();
                _playedTypesMask = value;
            }
        }

        /// <summary>标记该类型本场已打出；返回 true 表示是第一次（需要触发效果）。</summary>
        private bool MarkPlayed(CardType type)
        {
            int bit = 1 << (int)type;
            if ((_playedTypesMask & bit) != 0)
                return false;

            PlayedTypesMask = _playedTypesMask | bit;
            return true;
        }

        public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            if (cardPlay.Card.Owner != Owner) return;

            var type = cardPlay.Card.Type;
            if (MarkPlayed(type))
            {
                Flash();
                await PowerCmd.Apply<CreationPower>(
                    choiceContext,
                    Owner.Creature,
                    CreationAmount,
                    Owner.Creature,
                    null);
            }
        }

        public override Task AfterCombatEnd(CombatRoom _)
        {
            PlayedTypesMask = 0;
            return Task.CompletedTask;
        }
    }
}
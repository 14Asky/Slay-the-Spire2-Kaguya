using Godot;
using Kaguya.Cards;  
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace Kaguya
{
    public sealed class PartingGift : RelicModel
    {
        // 先古遗物（通过事件获取）
        public override RelicRarity Rarity => RelicRarity.Ancient;

        // 拾起时有效果
        public override bool HasUponPickupEffect => true;

        // 悬浮提示：合并两张卡牌的提示集合
        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            HoverTipFactory.FromCardWithCardHoverTips<TransientSymphony>()
                .Concat(HoverTipFactory.FromCardWithCardHoverTips<Reply>());

        // 动态变量：用于本地化文本（血量损失、卡牌名称）
        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new HpLossVar(10),                                          // 损失 10 生命上限
            new StringVar("TransientSymphony", ModelDb.Card<TransientSymphony>().Title),
            new StringVar("Reply", ModelDb.Card<Reply>().Title)
        };

        public override async Task AfterObtained()
        {
            // 1. 减少 10 点生命上限
            await CreatureCmd.LoseMaxHp(
                new ThrowingPlayerChoiceContext(),   // 无需玩家选择的上下文
                Owner.Creature,
                DynamicVars.HpLoss.BaseValue,
                cardSource: null
            );

            // 2. 创建两张卡牌并加入牌组
            var results = new List<CardPileAddResult>();

            var card1 = Owner.RunState.CreateCard<TransientSymphony>(Owner);
            if (card1 != null)
                results.Add(await CardPileCmd.Add(card1, PileType.Deck));

            var card2 = Owner.RunState.CreateCard<Reply>(Owner);
            if (card2 != null)
                results.Add(await CardPileCmd.Add(card2, PileType.Deck));

            // 3. 播放卡牌加入牌组的预览动画（时长 1 秒）
            if (results.Count > 0)
                CardCmd.PreviewCardPileAdd(results, 1f);
        }
    }
}

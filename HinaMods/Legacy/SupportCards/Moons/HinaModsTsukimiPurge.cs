//using BaseLib.Utils;
//using Godot;
//using Kaguya.HinaMods.Legacy.Powers;
//using MegaCrit.Sts2.Core.Combat;
//using MegaCrit.Sts2.Core.Commands;
//using MegaCrit.Sts2.Core.Entities.Cards;
//using MegaCrit.Sts2.Core.Entities.Creatures;
//using MegaCrit.Sts2.Core.Entities.Players;
//using MegaCrit.Sts2.Core.GameActions.Multiplayer;
//using MegaCrit.Sts2.Core.HoverTips;
//using MegaCrit.Sts2.Core.Localization.DynamicVars;
//using MegaCrit.Sts2.Core.Models;
//using MegaCrit.Sts2.Core.Models.CardPools;
//using MegaCrit.Sts2.Core.Models.Powers;
//using Kaguya.HinaMods;
//using Kaguya.HinaMods.Legacy.Cards;
//using Kaguya.HinaMods.Character;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;

//namespace Kaguya.HinaMods.Legacy.Cards.Rare;

//public sealed class LegacyHinaModsTsukimiPurge : LegacyMoonsCard
//{
//    // ====================== 标准悬浮提示 ======================
//    protected override IEnumerable<IHoverTip> ExtraHoverTips
//    {
//        get
//        {
//            foreach (var tip in base.ExtraHoverTips)
//                yield return tip;
//            yield return HoverTipFactory.FromPower<LegacyTsukimiTimePower>();
//        }
//    }

//    // 卡牌关键词
//    public override List<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

//    // 自定义标签
//    public override HashSet<string> CustomTags => new HashSet<string>() { LegacyCustomCardTags.MOONYEARS };

//    // 构造：1费 技能牌 远古卡 自身目标（完全不变）
//    public LegacyHinaModsTsukimiPurge() : base(1, CardType.Skill, CardRarity.Ancient, TargetType.Self) { }

//    // 播放条件：月见层数≥1000（完全不变）
//    protected override bool IsPlayable
//    {
//        get
//        {
//            LegacyTsukimiTimePower power = Owner.Creature.GetPower<LegacyTsukimiTimePower>();
//            return power != null && power.Amount >= 1000;
//        }
//    }

//    // 金色发光（完全不变）
//    protected override bool ShouldGlowGoldInternal => true;

//    // ====================== 核心玩法逻辑（100%原样保留） ======================
//    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
//    {
//        // 1. 消耗 1000 层月见
//        LegacyTsukimiTimePower tsukimiPower = Owner.Creature.GetPower<LegacyTsukimiTimePower>();
//        if (tsukimiPower != null)
//        {
//            await PowerCmd.ModifyAmount(choiceContext, tsukimiPower, -1000, Owner.Creature, this);
//        }

//        // 2. 获取所有卡牌堆
//        CardPile draw = PileType.Draw.GetPile(Owner);
//        CardPile hand = PileType.Hand.GetPile(Owner);
//        CardPile discard = PileType.Discard.GetPile(Owner);
//        CardPile exhaust = PileType.Exhaust.GetPile(Owner);

//        // 3. 筛选全牌堆诅咒牌
//        List<CardModel> curseCards = draw.Cards
//            .Concat(hand.Cards)
//            .Concat(discard.Cards)
//            .Concat(exhaust.Cards)
//            .Where(card => card.Type == CardType.Curse)
//            .ToList();

//        // 4. 消耗所有诅咒
//        int count = curseCards.Count;
//        foreach (CardModel curse in curseCards)
//        {
//            await CardCmd.Exhaust(choiceContext, curse);
//        }

//        // 5. 获得对应能量
//        if (count > 0)
//        {
//            await PlayerCmd.GainEnergy(count, Owner);
//        }
//    }

//    // 升级效果：费用-1（完全不变）
//    protected override void OnUpgrade()
//    {
//        base.EnergyCost.UpgradeBy(-1);
//    }
//}
//using BaseLib.Utils;
//using Godot;
//using Kaguya.HinaMods.Legacy.Powers;
//using MegaCrit.Sts2.Core.Commands;
//using MegaCrit.Sts2.Core.Entities.Cards;
//using MegaCrit.Sts2.Core.Entities.Players;
//using MegaCrit.Sts2.Core.Factories;
//using MegaCrit.Sts2.Core.GameActions.Multiplayer;
//using MegaCrit.Sts2.Core.HoverTips;
//using MegaCrit.Sts2.Core.Models;
//using MegaCrit.Sts2.Core.Nodes.CommonUi;
//using Kaguya.HinaMods;
//using Kaguya.HinaMods.Legacy.Cards;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;

//namespace Kaguya.HinaMods.Legacy.Cards.Rare;

//public sealed class LegacyMoonviewCall : LegacyMoonsCard
//{
//    // 悬浮提示
//    protected override IEnumerable<IHoverTip> ExtraHoverTips
//    {
//        get
//        {
//            foreach (var tip in base.ExtraHoverTips)
//                yield return tip;
//            yield return HoverTipFactory.FromPower<LegacyTsukimiTimePower>();
//        }
//    }

//    // 卡牌关键词：使用后消耗
//    public override List<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

//    // 自定义标签
//    public override HashSet<string> CustomTags => new HashSet<string>() { LegacyCustomCardTags.MOONYEARS };

//    // 构造函数：1费 技能 远古稀有度 指向自身
//    public LegacyMoonviewCall() : base(1, CardType.Skill, CardRarity.Ancient, TargetType.Self) { }

//    // 可播放
//    protected override bool IsPlayable => true;

//    // 金色发光
//    protected override bool ShouldGlowGoldInternal => true;

//    // 核心逻辑（绕过Ancient过滤，官方API联机同步）
//    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
//    {
//        Player player = Owner;
//        if (player == null) return;

//        // 1. 获取仅含Ancient的MoonsCard卡池（不被过滤）
//        List<CardModel> moonsCardPool = GetMoonsAncientCardPool();
//        if (!moonsCardPool.Any()) return;

//        // 2. 抽卡数量：基础1张，升级2张
//        int drawCount = IsUpgraded ? 2 : 1;

//        // 3. 官方随机+创建（绕过FilterForCombat，保留Ancient）
//        for (int i = 0; i < drawCount; i++)
//        {
//            // 官方原生随机：使用CombatCardGeneration RNG，确保联机同步
//            CardModel randomCard = player.RunState.Rng.CombatCardGeneration.NextItem(moonsCardPool);

//            // 官方创建卡牌：战斗内必须用CombatState.CreateCard，确保状态同步
//            CardModel newCard = player.Creature.CombatState.CreateCard(randomCard, player);

//            // 官方加入手牌：使用AddGeneratedCardToCombat，触发正确的战斗事件
//            await CardPileCmd.AddGeneratedCardToCombat(
//                newCard,
//                PileType.Hand,
//                player,
//                CardPilePosition.Top
//            );
//        }
//    }

//    // 升级效果：费用-1
//    protected override void OnUpgrade()
//    {
//        EnergyCost.UpgradeBy(-1);
//    }

//    /// <summary>
//    /// 获取仅含Ancient稀有度的MoonsCard卡池
//    /// 解决CardFactory自动过滤Ancient的问题
//    /// </summary>
//    private List<CardModel> GetMoonsAncientCardPool()
//    {
//        return ModelDb.AllCards
//            .OfType<LegacyMoonsCard>()
//            .Where(c => c.Rarity == CardRarity.Ancient) // 强制筛选Ancient
//            .Cast<CardModel>()
//            .ToList();
//    }
//}
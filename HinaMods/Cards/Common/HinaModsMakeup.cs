// ⚠ 本文件整份为注释（无有效代码），对应卡牌未实装。
//using BaseLib.Utils;
//using Godot;
//using MegaCrit.Sts2.Core.CardSelection;
//using MegaCrit.Sts2.Core.Commands;
//using MegaCrit.Sts2.Core.Entities.Cards;
//using MegaCrit.Sts2.Core.Entities.Players;
//using MegaCrit.Sts2.Core.Factories;
//using MegaCrit.Sts2.Core.GameActions.Multiplayer;
//using MegaCrit.Sts2.Core.Localization.DynamicVars;
//using MegaCrit.Sts2.Core.Models;
//using MegaCrit.Sts2.Core.Models.CardPools;
//using Kaguya.HinaMods;
//using Kaguya.HinaMods.Character;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;

//namespace Kaguya.HinaMods.Cards;

//"MOENEGIMOD-HINA_MODS_MAKEUP.title": "化妆",
//"MOENEGIMOD-HINA_MODS_MAKEUP.description": "选择1张手牌，将其变化。",
//// 完全复刻SingerReshape手动选择模板 | 卡牌：化妆
//public sealed class HinaModsMakeup : HinaModsCard
//{
//    // 动态变量：选择1张手牌（完全沿用模板格式）
//    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
//    {
//        new IntVar("TransformCount", 1)
//    };

//    // 构造函数：1费 技能 白卡 目标自身
//    public HinaModsMakeup()
//        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
//    { }

//    // 核心标签：消耗（Exhaust）
//    public override List<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
//    // 完全复刻 手动选择手牌变形逻辑（无任何修改）
//    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
//    {
//        Player player = Owner;
//        if (player == null)
//            return;

//        // 读取选择数量（固定1张）
//        int selectCount = (int)DynamicVars["TransformCount"].BaseValue;

//        // 手动选择器：完全和参考代码一致
//        CardSelectorPrefs prefs = new CardSelectorPrefs(
//            CardSelectorPrefs.TransformSelectionPrompt,
//            selectCount
//        );

//        // 从手牌手动选牌
//        IEnumerable<CardModel> selectedCards = await CardSelectCmd.FromHand(
//            choiceContext,
//            player,
//            prefs,
//            null,
//            this
//        );

//        if (!selectedCards.Any())
//            return;

//        // 获取角色全卡池（通用版，不限制歌者）
//        List<CardModel> characterCardPool = GetCharacterCards(player);
//        if (!characterCardPool.Any())
//            return;

//        // 遍历选中的牌，执行变形
//        foreach (CardModel oldCard in selectedCards)
//        {
//            CardModel newCard = CardFactory.GetDistinctForCombat(
//                player,
//                characterCardPool,
//                1,
//                player.RunState.Rng.CombatCardGeneration
//            ).FirstOrDefault();

//            if (newCard == null)
//                continue;

//            // 移除旧牌，添加新牌到手牌
//            await CardPileCmd.RemoveFromCombat(oldCard);
//            await CardPileCmd.AddGeneratedCardToCombat(newCard, PileType.Hand, addedByPlayer: true);

//            // 新牌本回合免费使用
//            newCard.SetToFreeThisTurn();
//        }
//    }

//    // 获取角色全卡池（移除歌者限制，完全通用）
//    private List<CardModel> GetCharacterCards(Player player)
//    {
//        HinaModsCardPool characterPool = player.Character.CardPool as HinaModsCardPool;
//        if (characterPool == null)
//            return new List<CardModel>();

//        return characterPool.GetUnlockedCards(
//            player.UnlockState,
//            player.RunState.CardMultiplayerConstraint
//        ).ToList();
//    }

//    // 升级：保留
//    protected override void OnUpgrade()
//    {
//        AddKeyword(CardKeyword.Retain);
//    }
//}
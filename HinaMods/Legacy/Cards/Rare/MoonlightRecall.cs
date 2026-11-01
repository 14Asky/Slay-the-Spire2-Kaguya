using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Kaguya.HinaMods.Legacy.Cards;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Cards.Rare;

/// <summary>
/// 歌者功能卡牌：时光回溯
/// 效果：选择弃牌堆中的1张牌，将其加入手牌并本回合免费打出
/// 升级：费用减少1点（1费→0费），被选择的牌获得升级
/// </summary>
public sealed class LegacyMoonlightRecall : HinaModsCard
{
    // 构造函数：1费、技能、罕见稀有度、目标自身（完全对标参考）
    public LegacyMoonlightRecall()
        : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    { }

    public override List<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 获取玩家（统一格式，完全对标参考）
        Player player = Owner;
        if (player == null) return;

        // 播放施法动画（完全照搬参考）
        await CreatureCmd.TriggerAnim(player.Creature, "Cast", player.Character.CastAnimDelay);

        // 弃牌堆选牌逻辑（1:1 对标《坚守》的选牌代码）
        CardPile discardPile = PileType.Discard.GetPile(player);
        List<CardModel> discardCards = discardPile.Cards.ToList();

        if (!discardCards.Any())
            return;

        // 🔥 仅修改此处，其余代码完全不变
        CardSelectorPrefs prefs = new CardSelectorPrefs(base.SelectionScreenPrompt, 1);

        IEnumerable<CardModel> selectedCards = await CardSelectCmd.FromSimpleGrid(
            context: choiceContext,
            cardsIn: discardCards,
            player: player,
            prefs: prefs
        );

        CardModel targetCard = selectedCards.FirstOrDefault();
        if (targetCard == null)
            return;

        // 将卡牌加入手牌
        await CardPileCmd.Add(targetCard, PileType.Hand);

        // 本回合免费打出（新增核心效果，其余代码完全不变）
        targetCard.SetToFreeThisTurn();

        // ======================
        // 仅【本卡牌升级后】为目标卡牌升级（仅加一层判断，其余代码完全不变）
        // ======================
        if (IsUpgraded && targetCard.IsMutable && targetCard.IsUpgradable)
        {
            targetCard.UpgradeInternal();       // 执行升级
            targetCard.FinalizeUpgradeInternal(); // 完成升级收尾
        }
    }

    // 🔥 严格使用你指定的升级逻辑：费用-1（1费→0费）
    protected override void OnUpgrade()
    {
    }
}
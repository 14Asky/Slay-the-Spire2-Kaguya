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
using Kaguya.HinaMods.Cards;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards.Rare;

/// <summary>
/// 习惯寂寞（AccustomedToLoneliness）
/// 本地化键：KAGUYA-MOONLIGHT_RECALL.title / .description
/// 1费 · 技能牌 · 稀有 · 目标：自身 · 关键词：Exhaust
/// 卡面：将[gold]弃牌堆[/gold]中的1张牌[gold]升级[/gold]并加入[gold]手牌[/gold]，其本回合可被免费打出。
/// 升级：费用 -1
/// </summary>
public sealed class AccustomedToLoneliness : HinaModsCard
{
    // 构造函数：1费、技能、罕见稀有度、目标自身（完全对标参考）
    public AccustomedToLoneliness()
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

        // 「获得+免费」标记：这类牌打出时不消耗月夜
        HinaModsCard.MarkFreePlay(targetCard);

        // ======================
        // 仅【本卡牌升级后】为目标卡牌升级（仅加一层判断，其余代码完全不变）
        // ======================
        if (IsUpgraded && targetCard.IsMutable && targetCard.IsUpgradable)
        {
            targetCard.UpgradeInternal();       // 执行升级
            targetCard.FinalizeUpgradeInternal(); // 完成升级收尾
        }
    }

    /// <summary>升级：费用 -1（1费→0费）。</summary>
    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
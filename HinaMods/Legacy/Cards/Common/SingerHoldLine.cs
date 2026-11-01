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
using Kaguya.HinaMods;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Cards;

/// <summary>
/// 歌者防御卡牌：坚守
/// 效果：获得5点格挡，选择弃牌堆中的1张牌加入手牌
/// 升级：获得8点格挡，选择弃牌堆中的1张牌加入手牌
/// </summary>
public sealed class LegacySingerHoldLine : HinaModsCard
{
    // 构造函数：1费、技能、普通稀有度、目标自身（完全对标参考）
    public LegacySingerHoldLine()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    { }

    // 歌者专属自定义标签（完全保留）
    public override HashSet<string> CustomTags => new HashSet<string>() { LegacyCustomCardTags.SINGER };

    // 防御标签（照搬参考格式）

    // 🔥 完全对标 LegacyHinaModsClearGame：动态变量 BlockVar（基础5点格挡）
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(5m, ValueProp.Move)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 获取玩家（统一格式）
        Player player = Owner;
        if (player == null) return;

        // 播放施法动画
        await CreatureCmd.TriggerAnim(player.Creature, "Cast", player.Character.CastAnimDelay);

        // 🔥 完全对标参考：官方标准格挡调用
        await CommonActions.CardBlock(this, cardPlay);

        // 弃牌堆选牌加入手牌（原有逻辑保留）
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

        await CardPileCmd.Add(targetCard, PileType.Hand);
    }

    // 🔥 完全对标 LegacyHinaModsClearGame：动态变量升级格式
    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
    }
}

using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 毕业演出（GraduationPerformance）
/// 本地化键：KAGUYA-GRADUATION_PERFORMANCE.title / .description
/// 1费 · 技能牌 · 稀有 · 目标：自身 · 关键词：Exhaust
/// 卡面：将你[gold]抽牌堆[/gold]中的[purple]歌声[/purple]牌全部打出；
///       然后抽取等量+1的牌。
/// 升级：无
/// </summary>
public class GraduationPerformance() : HinaModsCard(1,
    CardType.Skill, CardRarity.Rare,
    TargetType.Self)
{
    public override List<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            HoverTipFactory.FromPower<MoonlightPower>()
        };
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1) 快照抽牌堆中的全部【歌声】牌
        List<CardModel> echoCards = PileType.Draw.GetPile(Owner).Cards
            .OfType<HinaModsCard>()
            .Where(c => c.HasEcho)
            .Cast<CardModel>()
            .ToList();

        if (echoCards.Count == 0)
            return;

        // 2) 安全批量打出：整批先入 Play 堆再逐张默认参数 AutoPlay，全程不跳视觉，
        //    避免歌声牌在 Draw↔Play↔Exhaust 之间搬家时 UI 节点脱节（空白卡牌）。
        await HinaModsCard.PlayCardsSafely(choiceContext, echoCards);

        // 3) 抽取与打出的歌声牌等量的牌（升级后 +1）
        int drawCount = echoCards.Count + (IsUpgraded ? 1 : 0);
        if (drawCount > 0)
        {
            await CardPileCmd.Draw(choiceContext, drawCount, Owner);
        }
    }

    // 升级：多抽 1 张牌（由 IsUpgraded 分支实现，无需改数值）
    protected override void OnUpgrade()
    {
    }
}

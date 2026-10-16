using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods;
using Kaguya.HinaMods.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaModsCode.cards.Rare;

// 卡牌类名同步修改
/// <summary>
/// 三色流光（TricolorStreamer）
/// 本地化键：KAGUYA-FINAL_PERFORMANCE.title / .description
/// 2费 · 技能牌 · 稀有 · 目标：单个敌人 · 关键词：歌声
/// 卡面：打出你[gold]消耗堆[/gold]中所有[purple]歌声[/purple]牌。
/// 代码实测：CalculationBase=0
/// 升级：费用 -1
/// </summary>
public sealed class TricolorStreamer : HinaModsCard
{
    // 动态变量（对标KnifeTrap标准写法）
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CalculationBaseVar(0m),
        new CalculationExtraVar(0m)
    ];
    // 按需求：只保留【歌声】关键词，不带【消耗】
    public override List<CardKeyword> CanonicalKeywords => [KaguyaKeywords.Echo];

    public override HashSet<string> CustomTags => new HashSet<string>() { CustomCardTags.ECHO };
    // 构造函数：2费 技能牌 稀有 单体目标
    public TricolorStreamer()
        : base(2, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy)
    { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1) 快照消耗堆里所有歌声牌。
        //    用 HasEcho（不是查标签）：中途才获得歌声的牌（如旋转打击）也能被计入。
        List<CardModel> singerCardsInExhaust = PileType.Exhaust
            .GetPile(Owner)
            .Cards
            .OfType<HinaModsCard>()
            .Where(c => c.HasEcho)
            .Cast<CardModel>()
            .ToList();

        if (singerCardsInExhaust.Count == 0)
            return;

        // 2) 交给基类的安全批量打出。
        //    ⚠ 这里绝不能再混用 skipCardPileVisuals：歌声牌的结局堆恒为 Exhaust，
        //    跳过视觉会让牌在 Exhaust↔Play 之间反复搬家而 UI 节点不跟随，
        //    进而在手牌区留下「有占位、点不亮、拖不动」的空白卡（单人和联机都会出现）。
        await HinaModsCard.PlayCardsSafely(choiceContext, singerCardsInExhaust, cardPlay.Target);
    }

    // 升级：费用 -1（2 → 1）
    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
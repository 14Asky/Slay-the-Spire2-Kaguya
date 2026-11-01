using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Powers;
using Kaguya.HinaMods.SupportCards.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Kaguya.HinaMods;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 担忧（Worry）
/// 本地化键：KAGUYA-PARRY_PARRY.title / .description
/// 1费 · 技能牌 · 罕见 · 目标：自身 · 关键词：消耗（升级后移除） · 标签：支援牌
/// 卡面：将2张[blue]照顾[/blue]加入[gold]手牌[/gold]。
/// 升级：去除【消耗】。
/// </summary>
public sealed class Worry : HinaModsCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    /// <summary>基础带【消耗】；升级时用 RemoveKeyword 去掉（见 OnUpgrade）。</summary>
    public override List<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    /// <summary>支援牌标签（本牌会获得支援牌）。</summary>
    public override HashSet<string> CustomTags => new HashSet<string>() { CustomCardTags.SUPPORTCARD };

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
           HoverTipFactory.FromCard<Care>()
        };
    }

    public Worry()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 固定加入 2 张「照顾」
        for (int i = 0; i < 2; i++)
        {
            CardModel supportBlock = CardScope.CreateCard<Care>(Owner);
            await CardPileCmd.AddGeneratedCardToCombat(supportBlock, PileType.Hand, Owner);
        }
    }

    /// <summary>升级：去除【消耗】（与官方同类卡一致，用 RemoveKeyword 而非覆盖 CanonicalKeywords）。</summary>
    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }
}

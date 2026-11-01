using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Legacy.Powers;
using Kaguya.HinaMods.Legacy.SupportCards.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Kaguya.HinaMods;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Cards;

public sealed class LegacyMoonlightTripleStrike : HinaModsCard
{
    // 无任何数值变量
    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    // 消耗关键词
    public override List<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
           HoverTipFactory.FromCard<LegacySupportStrike>()
        };
    }

    // 构造：1费 技能牌 白卡 自身目标
    public LegacyMoonlightTripleStrike()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 动态数量：普通2张，升级3张
        int count = IsUpgraded ? 3 : 2;
        for (int i = 0; i < count; i++)
        {
            CardModel card = CardScope.CreateCard<LegacySupportStrike>(Owner);
            await CardPileCmd.Add(card, PileType.Hand);
        }
    }

    // 无属性升级，留空
    protected override void OnUpgrade()
    {
    }
}
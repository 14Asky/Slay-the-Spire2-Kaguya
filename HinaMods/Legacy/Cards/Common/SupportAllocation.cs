using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Legacy.Powers;
using Kaguya.HinaMods.Legacy.SupportCards.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
// 新增：悬浮提示必需的命名空间
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Cards;

/// <summary>
/// 支援调配
/// 1费 技能牌 | 目标自身
/// 获得1张【支援打击】。
/// 升级：获得的支援牌自动升级。
/// </summary>
public sealed class LegacySupportAllocation : HinaModsCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    public override List<CardKeyword> CanonicalKeywords => [
         CardKeyword.Exhaust
    ];

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
           HoverTipFactory.FromCard<LegacyHeavyStrikeCard>()
        };
    }

    public LegacySupportAllocation()
        : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 生成 支援打击
        CardModel strike = CardScope.CreateCard<LegacyHeavyStrikeCard>(Owner);

        // 核心：如果卡牌已升级，生成的支援打击同步升级
        if (IsUpgraded)
        {
            CardCmd.Upgrade(strike);
        }

        // 将卡牌加入手牌
        await CardPileCmd.Add(strike, PileType.Hand);
    }

    protected override void OnUpgrade()
    {
        // 本卡无属性升级，仅触发衍生卡升级，无需修改
    }
}
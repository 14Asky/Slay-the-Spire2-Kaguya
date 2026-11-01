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
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 登场（Appear）
/// 本地化键：KAGUYA-MOONLIGHT_TRIPLE_STRIKE.title / .description
/// 1费 · 技能牌 · 普通 · 目标：自身 · 标签：支援牌（无关键词）
/// 卡面：获得1张[blue]疾驰[/blue]。
/// 升级：获得的疾驰是【升级后】的。
/// </summary>
public sealed class Appear : HinaModsCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    /// <summary>支援牌标签（本牌会获得支援牌）。</summary>
    public override HashSet<string> CustomTags => new HashSet<string>() { CustomCardTags.SUPPORTCARD };

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
           HoverTipFactory.FromCard<SupportDash>(),
           HoverTipFactory.FromCard<SupportStrike>()
        };
    }

    public Appear()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1 张疾驰
        CardModel dash = CardScope.CreateCard<SupportDash>(Owner);
        await CardPileCmd.AddGeneratedCardToCombat(dash, PileType.Hand, Owner);

        // 1 张支援打击
        CardModel strike = CardScope.CreateCard<SupportStrike>(Owner);
        await CardPileCmd.AddGeneratedCardToCombat(strike, PileType.Hand, Owner);
    }

    /// <summary>升级：费用 -1。</summary>
    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}

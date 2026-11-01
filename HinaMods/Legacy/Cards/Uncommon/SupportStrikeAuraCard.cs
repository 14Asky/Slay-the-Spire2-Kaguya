using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Legacy.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Kaguya.HinaMods;
using System.Collections.Generic;
using System.Threading.Tasks;

// 统一命名空间
namespace Kaguya.HinaMods.Legacy.Cards;

/// <summary>
/// 支援打击光环：获得【支援打击光环】BUFF。消耗。
/// </summary>
public sealed class LegacySupportStrikeAuraCard : HinaModsCard
{
    // 卡牌配置：1费 技能牌 罕见卡 目标自身
    public LegacySupportStrikeAuraCard()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    { }

    public override HashSet<string> CustomTags => new HashSet<string>() { LegacyCustomCardTags.SUPPORTCARD };

    public override List<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
             HoverTipFactory.FromPower<LegacySupportStrikeAuraPower>()
        };
    }

    // 核心打出逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<LegacySupportStrikeAuraPower>(
            choiceContext,
            Owner.Creature,
            1,
            Owner.Creature,
            this);
    }

    // 升级：移除消耗
    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }
}
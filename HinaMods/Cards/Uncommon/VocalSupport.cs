using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 声援（VocalSupport）
/// 本地化键：KAGUYA-SINGER_SUPPORT_GUARD.title / .description
/// 1费 · 技能牌 · 罕见 · 目标：自身 · 关键词：歌声
/// 卡面：获得6点格挡。本回合内，你每获得1张牌就将其升级。
/// 升级：获得【保留】
/// </summary>
public sealed class VocalSupport : HinaModsCard
{
    /// <summary>歌声关键词（打出后进消耗堆，下回合抽牌前洗回抽牌堆）。</summary>
    public override HashSet<string> CustomTags => new HashSet<string>() { CustomCardTags.ECHO };

    /// <summary>无【消耗】；升级后获得【保留】。</summary>
    public override List<CardKeyword> CanonicalKeywords =>
        IsUpgraded ? [CardKeyword.Retain] : [];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(6m, ValueProp.Move)
    ];

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            HoverTipFactory.FromPower<CheerPower>()
        };
    }

    public VocalSupport() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        await CommonActions.CardBlock(this, cardPlay);

        // 挂本回合临时能力，由它负责升级本回合获得的每一张牌
        await PowerCmd.Apply<CheerPower>(
            choiceContext,
            Owner.Creature,
            1,
            Owner.Creature,
            this);
    }

    // 升级：获得【保留】（CanonicalKeywords 按 IsUpgraded 动态返回）
    protected override void OnUpgrade()
    {
    }
}

using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Cards;

/// <summary>
/// 固守 | 2费技能牌
/// 获得17点格挡，升级后21点 | 保留关键词 | 歌者标签
/// </summary>
public sealed class LegacySingerBulwark : HinaModsCard
{
    // 歌者自定义专属标签（强制保留）
    public override HashSet<string> CustomTags => new HashSet<string>() { LegacyCustomCardTags.SINGER };

    // 官方关键词：保留(Retain) + 防御标签
    public override List<CardKeyword> CanonicalKeywords => [
          CardKeyword.Retain
    ];

    // 动态变量：基础14点格挡（官方标准配置）
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(14m, ValueProp.Move)
    ];

    // 构造函数：2费 技能 普通卡 目标自身
    public LegacySingerBulwark()
        : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    // 打出逻辑：获得格挡（官方原生方法，无BUG）
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardBlock(this, cardPlay);
    }

    // 升级效果：17格挡 → 21格挡（+4）
    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(4m);
    }
}

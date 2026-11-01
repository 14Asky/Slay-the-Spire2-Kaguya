using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Kaguya.HinaMods;
using Kaguya.HinaMods.Legacy.Cards;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Cards.Uncommon;

// 完全参照你的模板编写 | 0费歌者基础抽牌卡
public sealed class LegacySingerDraw : HinaModsCard
{
    // 你要求的歌者专属标签
    public override HashSet<string> CustomTags => new HashSet<string>() { LegacyCustomCardTags.SINGER };

    // 动态变量：仅抽牌数量（基础1张）
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new CardsVar(1)
    };

    // 构造函数：0费 技能 普通卡 目标自身
    public LegacySingerDraw()
        : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    { }

    // 核心打出逻辑：完全参照你的模板，使用CardPileCmd.Draw抽牌
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 抽牌（和你月夜抽牌卡的抽牌方式完全一致）
        await CardPileCmd.Draw(choiceContext, base.DynamicVars.Cards.BaseValue, base.Owner);
    }

    // 升级效果：抽牌数 1→2
    protected override void OnUpgrade()
    {
        base.DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
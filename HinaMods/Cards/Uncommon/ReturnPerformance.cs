using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Kaguya.HinaMods;
using Kaguya.HinaMods.Cards;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards.Uncommon;

// 完全参照你的模板编写 | 0费歌者基础抽牌卡
/// <summary>
/// 回归演出（ReturnPerformance）
/// 本地化键：KAGUYA-SINGER_DRAW.title / .description
/// 0费 · 技能牌 · 罕见 · 目标：自身 · 关键词：歌声
/// 卡面：抽1张牌。
/// 代码实测：Cards=1
/// 升级：Cards +1
/// </summary>
public sealed class ReturnPerformance : HinaModsCard
{
    // 你要求的歌者专属标签
    public override HashSet<string> CustomTags => new HashSet<string>() { CustomCardTags.ECHO };

    // 动态变量：仅抽牌数量（基础1张）
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new CardsVar(1)
    };

    // 构造函数：0费 技能 普通卡 目标自身
    public ReturnPerformance()
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
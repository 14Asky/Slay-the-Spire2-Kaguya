using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Kaguya.HinaMods;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 星河入梦（StarRiverDream）
/// 本地化键：KAGUYA-SUPPORT_ASSEMBLE.title / .description
/// 0费 · 技能牌 · 罕见 · 目标：自身 · 关键词：消耗 · 标签：支援牌
/// 卡面：将1张[blue]演奏[/blue]加入[gold]抽牌堆[/gold]，且费用减少{CostReduce:diff()}。
/// 代码实测：CostReduce=1
/// 升级：CostReduce +1（1 → 2）
/// </summary>
public sealed class StarRiverDream : HinaModsCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("CostReduce", 1m)
    ];

    public StarRiverDream()
        : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    { }

    /// <summary>消耗。</summary>
    public override List<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    /// <summary>支援牌标签（本牌会获得支援牌）。</summary>
    public override HashSet<string> CustomTags => new HashSet<string>() { CustomCardTags.SUPPORTCARD };

    /// <summary>提示：展示会被加入抽牌堆的「演奏」详情（抽牌堆里的牌玩家看不到，必须靠 tip）。</summary>
    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            HoverTipFactory.FromCard<Performance>()
        };
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player player = Owner;
        if (player == null)
            return;

        // 生成 1 张「演奏」（演奏是支援牌 Token）
        CardModel vigor = CardScope.CreateCard<Performance>(player);

        // 该「演奏」费用 -N（基础 1 / 升级 2）
        int reduce = (int)DynamicVars["CostReduce"].BaseValue;
        vigor.EnergyCost.AddUntilPlayed(-reduce);

        // 参考官方「捕捉灵魂」：加牌进抽牌堆必须用 PreviewCardPileAdd 包一层，否则没有过渡动画
        CardCmd.PreviewCardPileAdd(
            await CardPileCmd.AddGeneratedCardToCombat(
                vigor,
                PileType.Draw,
                player,
                CardPilePosition.Random));

        player.PlayerCombatState?.RecalculateCardValues();
    }

    /// <summary>升级：费用减少 1 → 2。</summary>
    protected override void OnUpgrade()
    {
        DynamicVars["CostReduce"].UpgradeValueBy(1m);
    }
}

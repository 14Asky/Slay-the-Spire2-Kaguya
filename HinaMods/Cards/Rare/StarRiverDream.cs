using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Kaguya.HinaMods;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 星河入梦（StarRiverDream）
/// 本地化键：KAGUYA-SUPPORT_ASSEMBLE.title / .description
/// 1费 · 技能牌 · 罕见 · 目标：自身 · 关键词：消耗 · 标签：支援牌
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
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    { }

    /// <summary>消耗。</summary>
    public override List<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    /// <summary>支援牌标签（本牌会获得支援牌）。</summary>
    public override HashSet<string> CustomTags => new HashSet<string>() { CustomCardTags.SUPPORTCARD };

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

        await CardPileCmd.AddGeneratedCardToCombat(
            vigor,
            PileType.Draw,
            player,
            CardPilePosition.Random);

        player.PlayerCombatState?.RecalculateCardValues();
    }

    /// <summary>升级：费用减少 1 → 2。</summary>
    protected override void OnUpgrade()
    {
        DynamicVars["CostReduce"].UpgradeValueBy(1m);
    }
}

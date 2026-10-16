using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Kaguya.HinaMods;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 看电影（WatchAMovie）
/// 本地化键：KAGUYA-SINGER_BULWARK.title / .description
/// 3费 · 技能牌 · 罕见 · 目标：自身 · 关键词：Retain、歌声
/// 卡面：获得12点格挡。
///       抽2张牌（优先抽取[purple]歌声[/purple]牌）。
///       此牌每打出一次费用-1。
/// 代码实测：Block=12；Cards=2
/// 升级：Block +3
/// </summary>
public sealed class WatchAMovie : HinaModsCard
{
    /// <summary>歌声标签。</summary>
    public override HashSet<string> CustomTags => new HashSet<string>() { CustomCardTags.ECHO };

    /// <summary>保留 + 歌声。</summary>
    public override List<CardKeyword> CanonicalKeywords => [
          CardKeyword.Retain,
          KaguyaKeywords.Echo
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(12m, ValueProp.Move),
        new CardsVar(2)
    ];

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            HoverTipFactory.FromPower<MoonlightPower>()
        };
    }

    public WatchAMovie()
        : base(3, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        // 1) 获得格挡
        await CommonActions.CardBlock(this, cardPlay);

        // 2) 抽2张牌，优先抽歌声牌
        await DrawWithSongPriority(choiceContext, (int)DynamicVars.Cards.BaseValue);

        // 3) 此牌每打出一次费用-1（战斗内累积，最低 0）
        if (EnergyCost.GetWithModifiers(CostModifiers.None) > 0m)
        {
            EnergyCost.UpgradeBy(-1);
        }
    }

    /// <summary>
    /// 抽牌，但把抽牌堆里的【歌声】牌排到最前面优先抽到。
    /// 做法：先把歌声牌移到抽牌堆顶部，再按数量正常抽。
    /// </summary>
    private async Task DrawWithSongPriority(PlayerChoiceContext choiceContext, int count)
    {
        if (count <= 0)
            return;

        Player player = Owner;
        if (player == null)
            return;

        CardPile drawPile = PileType.Draw.GetPile(player);

        // 把抽牌堆里的歌声牌提到顶部（保持原有相对顺序）
        List<CardModel> songs = drawPile.Cards
            .OfType<HinaModsCard>()
            .Where(c => c.HasEcho)
            .Cast<CardModel>()
            .ToList();

        for (int i = 0; i < songs.Count && i < count; i++)
        {
            await CardPileCmd.Add(songs[i], PileType.Draw, CardPilePosition.Top);
        }

        await CardPileCmd.Draw(choiceContext, count, player);
    }

    /// <summary>升级：格挡 12 → 15。</summary>
    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
    }
}

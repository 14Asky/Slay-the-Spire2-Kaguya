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
    /// 抽牌，但优先抽【歌声】牌，且总抽牌数恒定为 count：
    /// · 抽牌堆里有 ≥ count 张歌声牌 → 从中【随机】抽 count 张；
    /// · 歌声牌不足 count 张       → 歌声牌全部抽到，剩余空位用普通牌补足；
    /// · 没有歌声牌               → 完全等同于普通抽牌。
    /// <para>
    /// 实现与引擎「回合开始优先抽【固有 Innate】牌」一致：先用
    /// <see cref="CardPile.MoveToTopInternal"/> 把选中的歌声牌静默提到堆顶，
    /// 再走标准抽牌命令，剩余空位自然由堆顶的普通牌补上。
    /// </para>
    /// <para>
    /// ⚠ 请勿用 CardPileCmd.Add(card, PileType.Draw, CardPilePosition.Top) 来重排 ——
    /// 该 API 面向「跨牌堆搬运」，即使目标就是当前所在的牌堆，也会照走一遍换堆流程并生成
    /// 一次多余 tween（卡牌在抽牌堆里弹进弹出），导致抽牌动画错乱。
    /// MoveToTopInternal 只改内部顺序、不发出任何换堆事件，因此不会产生多余动画。
    /// </para>
    /// </summary>
    private async Task DrawWithSongPriority(PlayerChoiceContext choiceContext, int count)
    {
        if (count <= 0)
            return;

        Player player = Owner;
        if (player == null)
            return;

        CardPile drawPile = PileType.Draw.GetPile(player);

        // 抽牌堆里的歌声牌
        List<CardModel> songs = drawPile.Cards
            .OfType<HinaModsCard>()
            .Where(c => c.HasEcho)
            .Cast<CardModel>()
            .ToList();

        // 本次最多优先 count 张：歌声牌多于 count 张时随机挑，不足则全部优先
        int take = songs.Count < count ? songs.Count : count;
        if (take > 0)
        {
            if (songs.Count > take)
            {
                // 歌声牌多于本次抽牌数 → 随机挑出 take 张（其余留在原处，不会被提前抽到）
                player.RunState.Rng.CombatCardSelection.Shuffle(songs);
            }

            // 倒序提到堆顶：MoveToTopInternal 每次都 Insert(0)，倒着来才能保持挑选顺序
            for (int i = take - 1; i >= 0; i--)
            {
                drawPile.MoveToTopInternal(songs[i]);
            }
        }

        // 正常抽 count 张：先抽到刚提到堆顶的歌声牌，剩余空位由普通牌补足
        await CardPileCmd.Draw(choiceContext, count, player);
    }

    /// <summary>升级：格挡 12 → 15。</summary>
    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
    }
}

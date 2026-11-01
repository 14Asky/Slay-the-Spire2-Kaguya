using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Kaguya.HinaMods;
using Kaguya.HinaMods.Cards;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards.Uncommon;

/// <summary>
/// 小菜一碟（PieceOfCake）
/// 本地化键：KAGUYA-HINA_MODS_FORTUNE_EXPEND.title / .description
/// 3费 · 技能牌 · 罕见 · 目标：自身 · 标签：支援牌
/// 卡面：获得2点[gold]格挡[/gold]，并随机将2张[blue]支援[/blue]技能牌加入[gold]抽牌堆[/gold]。
///       本场战斗中每当你打出一张[blue]支援[/blue]技能牌，本牌获得的[gold]格挡[/gold]+2。
/// 代码实测：Block=2；每张支援技能牌 +2 格挡
/// 升级：格挡 2 → 3；每张支援技能牌 +2 → +3
/// </summary>
public sealed class PieceOfCake : HinaModsCard
{
    /// <summary>基础格挡（升级 +3）。</summary>
    private decimal _baseBlock = 5m;

    /// <summary>每打出一张支援技能牌的格挡增量（升级不变）。</summary>
    private decimal _blockPerSupport = 2m;

    /// <summary>本场战斗中因打出支援技能牌累积的额外格挡。</summary>
    private decimal _blockBonus;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(5m, ValueProp.Move)];

    /// <summary>支援牌标签（本牌会获得支援牌）。</summary>
    public override HashSet<string> CustomTags => new HashSet<string>() { CustomCardTags.SUPPORTCARD };

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        // 本牌效果与【月夜】无关（旧版残留的月夜提示是错的），只解释「支援」标签。
        return new IHoverTip[]
        {
            new HoverTip(
                new LocString("card_keywords", "SUPPORT.title"),
                new LocString("card_keywords", "SUPPORT.description"))
        };
    }

    public PieceOfCake()
        : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    private decimal CurrentBlock => _baseBlock + _blockBonus;

    /// <summary>把成长后的格挡同步到卡面。</summary>
    private void RefreshCardValues()
    {
        DynamicVars.Block.BaseValue = CurrentBlock;
        Owner?.PlayerCombatState?.RecalculateCardValues();
    }

    /// <summary>本场战斗中每打出一张支援技能牌，本牌格挡 +2（升级后 +3）。</summary>
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await base.AfterCardPlayed(choiceContext, cardPlay);

        if (cardPlay.Card == this || cardPlay.Player != Owner)
        {
            return;
        }
        if (cardPlay.Card is not HinaModsCard played)
        {
            return;
        }
        if (played.Type != CardType.Skill || played.CustomTags?.Contains(CustomCardTags.SUPPORT) != true)
        {
            return;
        }

        _blockBonus += _blockPerSupport;
        RefreshCardValues();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, cardPlay);

        // 随机将 2 张支援技能牌加入抽牌堆
        List<CardModel> pool = ModelDb.AllCards
            .OfType<HinaModsCard>()
            .Where(c => c.Type == CardType.Skill
                        && c.CustomTags?.Contains(CustomCardTags.SUPPORT) == true)
            .Where(c => HinaModsCard.AllowedInRun(c, Owner))
            .Cast<CardModel>()
            .ToList();
        if (pool.Count == 0)
        {
            return;
        }

        List<CardModel> cards = CardFactory.GetDistinctForCombat(
            Owner,
            pool,
            2,
            Owner.RunState.Rng.CombatCardGeneration).ToList();

        // 参考官方「捕捉灵魂」：批量加牌进抽牌堆同样用 PreviewCardPileAdd 包一层，否则没有过渡动画
        CardCmd.PreviewCardPileAdd(
            await CardPileCmd.AddGeneratedCardsToCombat(cards, PileType.Draw, Owner));
    }

    /// <summary>升级：基础格挡 5 → 8（每张支援技能牌的 +2 不变）。</summary>
    protected override void OnUpgrade()
    {
        _baseBlock += 3m;
        DynamicVars.Block.BaseValue = CurrentBlock;
    }
}

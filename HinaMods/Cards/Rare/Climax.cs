using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using Kaguya.HinaMods.Cards;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards.Rare;

/// <summary>
/// 决战时刻（Climax）
/// 本地化键：KAGUYA-HINA_MODS_MOONLIGHT.title / .description
/// 1费 · 技能牌 · 稀有 · 目标：自身
/// 卡面：本回合你每打出一张牌，随机将一张[blue]支援[/blue]牌加入[gold]手牌[/gold]。
/// 升级：无
/// </summary>
public sealed class Climax : HinaModsCard
{

    /// <summary>支援牌标签（本牌会获得支援牌）。</summary>
    public override HashSet<string> CustomTags => new HashSet<string>() { CustomCardTags.SUPPORTCARD };

    /// <summary>本牌已打出、效果生效中。</summary>
    private bool _active;

    /// <summary>本回合已结束（效果失效）。</summary>
    private bool _expired;

    /// <summary>消耗；升级后额外获得【保留】。</summary>
    public override List<CardKeyword> CanonicalKeywords =>
        IsUpgraded ? [CardKeyword.Exhaust, CardKeyword.Retain] : [CardKeyword.Exhaust];

    public Climax()
        : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        _active = true;
    }

    /// <summary>本回合每打出一张牌（含本牌），随机将一张支援牌加入手牌。</summary>
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await base.AfterCardPlayed(choiceContext, cardPlay);

        // 不包含本牌自己
        if (cardPlay.Card == this)
            return;

        if (!_active || _expired || Owner == null || cardPlay.Player != Owner)
        {
            return;
        }

        List<CardModel> pool = ModelDb.AllCards
            .OfType<HinaModsCard>()
            .Where(c => c.CustomTags?.Contains(CustomCardTags.SUPPORT) == true)
            .Cast<CardModel>()
            .ToList();
        if (pool.Count == 0)
        {
            return;
        }

        List<CardModel> cards = CardFactory.GetDistinctForCombat(
            Owner,
            pool,
            1,
            Owner.RunState.Rng.CombatCardGeneration).ToList();

        foreach (CardModel card in cards)
        {
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner, CardPilePosition.Top);
        }
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterPlayerTurnStart(choiceContext, player);
        if (player == Owner)
        {
            _expired = false;
        }
    }

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        await base.BeforeSideTurnEnd(choiceContext, side, participants);
        if (side == CombatSide.Player)
        {
            _expired = true;
        }
    }

    // 升级：获得【保留】（CanonicalKeywords 按 IsUpgraded 动态返回，不再降费）
    protected override void OnUpgrade()
    {
    }
}

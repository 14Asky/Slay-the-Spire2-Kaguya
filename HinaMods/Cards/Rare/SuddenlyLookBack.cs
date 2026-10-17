using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 蓦然回首（SuddenlyLookBack）
/// 本地化键：KAGUYA-SINGER_MOON_FINALE.title / .description
/// 3费 · 技能牌 · 稀有 · 目标：自身
/// 卡面：将你的[gold]手牌[/gold]和[gold]弃牌堆[/gold]洗入[gold]抽牌堆[/gold]，然后抽满你的[gold]手牌[/gold]，
///       并且自动打出[gold]手牌[/gold]中的所有攻击牌。
/// 升级：费用 -1
/// </summary>
public sealed class SuddenlyLookBack : HinaModsCard
{
    /// <summary>基础带【消耗】；升级后额外获得【保留】（见 OnUpgrade）。</summary>
    public override List<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public SuddenlyLookBack()
        : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    { }

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        // 1) 消耗你所有的【诅咒牌】与【状态牌】
        //    取牌口径与官方「散射炮 FlakCannon」一致：全战斗区（含手牌/抽牌堆/弃牌堆），
        //    已进消耗堆的除外。
        List<CardModel> junk = Owner.PlayerCombatState.AllCards
            .Where(c => (c.Type == CardType.Curse || c.Type == CardType.Status)
                        && c.Pile != null
                        && c.Pile.Type != PileType.Exhaust)
            .ToList();

        int exhausted = 0;
        foreach (CardModel card in junk)
        {
            await CardCmd.Exhaust(choiceContext, card);
            exhausted++;
        }

        if (exhausted <= 0)
            return;

        // 2) 从抽牌堆【随机】打出与消耗数量相同的牌
        List<CardModel> candidates = PileType.Draw.GetPile(Owner).Cards.ToList();
        if (candidates.Count == 0)
            return;

        Owner.RunState.Rng.CombatCardSelection.Shuffle(candidates);

        int playCount = exhausted < candidates.Count ? exhausted : candidates.Count;
        await HinaModsCard.PlayCardsSafely(choiceContext, candidates.Take(playCount).ToList());
    }

    /// <summary>升级：获得【保留】。</summary>
    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}

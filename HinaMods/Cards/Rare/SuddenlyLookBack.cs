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
    public SuddenlyLookBack()
        : base(3, CardType.Skill, CardRarity.Rare, TargetType.Self)
    { }

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        // 1. 手牌 + 弃牌堆 全部洗入抽牌堆
        List<CardModel> handCards = PileType.Hand.GetPile(Owner).Cards.ToList();
        foreach (CardModel card in handCards)
        {
            await CardPileCmd.Add(card, PileType.Draw);
        }

        List<CardModel> discardCards = PileType.Discard.GetPile(Owner).Cards.ToList();
        foreach (CardModel card in discardCards)
        {
            await CardPileCmd.Add(card, PileType.Draw);
        }

        await CardPileCmd.Shuffle(choiceContext, Owner);

        // 2. 抽满手牌
        int need = CardPile.MaxCardsInHand - PileType.Hand.GetPile(Owner).Cards.Count;
        if (need > 0)
        {
            await CardPileCmd.Draw(choiceContext, need, Owner);
        }

        // 3. 安全自动打出【手牌】中的所有攻击牌
        //    （整批先入 Play 堆再逐张默认参数 AutoPlay，避免攻击牌在手牌/Play 堆间
        //      搬家时 UI 节点脱节，残留「有占位、点不亮、拖不动」的空白卡）
        List<CardModel> attacks = PileType.Hand.GetPile(Owner).Cards
            .Where(c => c.Type == CardType.Attack)
            .ToList();

        await HinaModsCard.PlayCardsSafely(choiceContext, attacks);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}

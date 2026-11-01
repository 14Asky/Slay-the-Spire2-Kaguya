using Godot;
using Kaguya.HinaMods.Legacy.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Kaguya.HinaMods;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Cards;

public sealed class LegacyEncore : HinaModsCard
{
    //public override HashSet<string> CustomTags => new HashSet<string>() { LegacyCustomCardTags.SINGER };
    // 官方原生写法：单元素数组（替代反编译的伪类型）
    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new IntVar("Replay", 1)
    };

    // 官方原生写法：单元素数组（完美等效）
    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
             HoverTipFactory.Static(StaticHoverTip.ReplayStatic)
        };
    }

    // 3费 → 升级2费
    public LegacyEncore() : base(3, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        // 获取所有卡牌堆
        CardPile draw = PileType.Draw.GetPile(Owner);
        CardPile hand = PileType.Hand.GetPile(Owner);
        CardPile discard = PileType.Discard.GetPile(Owner);
        CardPile exhaust = PileType.Exhaust.GetPile(Owner);

        // 筛选所有歌者卡牌
        var allSingerCards = draw.Cards
            .Concat(hand.Cards)
            .Concat(discard.Cards)
            .Concat(exhaust.Cards)
            .OfType<HinaModsCard>()
            .Where(c => c.CustomTags.Contains(LegacyCustomCardTags.SINGER))
            .ToList();

        // 给所有歌者牌 +1重放
        foreach (var card in allSingerCards)
        {
            card.BaseReplayCount += 1;
            CardCmd.Preview(card);
        }
    }

    // 升级：费用-1
    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1);
    }
}
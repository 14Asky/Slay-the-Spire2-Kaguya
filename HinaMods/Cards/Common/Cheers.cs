using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using Kaguya.HinaMods;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 喝彩（Cheers）
/// 本地化键：KAGUYA-SINGER_SELF_DEBUFF.title / .description
/// 1费 · 技能牌 · 普通 · 目标：自身 · 关键词：歌声 · 标签：支援牌
/// 卡面：获得4层[gold]活力[/gold]。随机将一张[blue]支援[/blue]技能牌洗入[gold]抽牌堆[/gold]。
/// 代码实测：VigorPower=4
/// 升级：活力 +2（4 → 6）
/// </summary>
public sealed class Cheers : HinaModsCard
{
    /// <summary>歌声关键词（打出后进消耗堆，下回合抽牌前洗回抽牌堆）
    /// + 支援牌标签（本牌会获得支援牌）。</summary>
    public override HashSet<string> CustomTags =>
        new HashSet<string>() { CustomCardTags.ECHO, CustomCardTags.SUPPORTCARD };

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<VigorPower>(4m)
    ];

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            HoverTipFactory.FromPower<VigorPower>()
        };
    }

    public Cheers()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        // 无条件获得活力
        await PowerCmd.Apply<VigorPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars[nameof(VigorPower)].BaseValue,
            Owner.Creature,
            this);

        // 随机一张【支援 + 技能】牌洗入抽牌堆
        List<CardModel> pool = ModelDb.CardPool<TokenCardPool>()
            .AllCardIds
            .Select(id => ModelDb.GetById<CardModel>(id))
            .OfType<HinaModsCard>()
            .Where(c => c.CustomTags?.Contains(CustomCardTags.SUPPORT) == true
                        && c.Type == CardType.Skill)
            .Cast<CardModel>()
            .ToList();

        if (pool.Count == 0)
            return;

        CardModel card = CardFactory.GetDistinctForCombat(
            Owner,
            pool,
            1,
            Owner.RunState.Rng.CombatCardGeneration).FirstOrDefault();

        if (card == null)
            return;

        // 参考官方「捕捉灵魂」：加牌进抽牌堆必须用 PreviewCardPileAdd 包一层，否则没有过渡动画
        CardCmd.PreviewCardPileAdd(
            await CardPileCmd.AddGeneratedCardToCombat(
                card, PileType.Draw, Owner, CardPilePosition.Random));
    }

    /// <summary>升级：活力 4 → 6（+2）。</summary>
    protected override void OnUpgrade()
    {
        DynamicVars[nameof(VigorPower)].UpgradeValueBy(2m);
    }
}

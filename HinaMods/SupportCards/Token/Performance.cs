using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 演奏（Performance）
/// 本地化键：KAGUYA-VIGOR_BURST.title / .description
/// 3费 · 技能牌 · 令牌 · 目标：自身
/// 卡面：获得{Block:diff()}点[gold]格挡[/gold]。将随机一张[purple]歌声[/purple]牌加入[gold]手牌[/gold]，
///       并使该[purple]歌声[/purple]牌本场战斗费用-1。
/// 代码实测：Block=12
/// 升级：Block +4（12 → 16）
/// </summary>
[Pool(typeof(TokenCardPool))]
public sealed class Performance : HinaModsCard
{
    // 支援 + 支援牌标签（支援标签使基类自动附加虚无/消耗关键词）
    public override HashSet<string> CustomTags =>
        new HashSet<string>() { CustomCardTags.SUPPORT, CustomCardTags.SUPPORTCARD };

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(12m, ValueProp.Move)
    ];

    public Performance()
        : base(3, CardType.Skill, CardRarity.Token, TargetType.Self)
    { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player player = Owner;
        if (player == null)
            return;

        // 获得格挡
        await CreatureCmd.GainBlock(
            player.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, cardPlay);

        List<CardModel> songPool = ModelDb.AllCards
            .OfType<HinaModsCard>()
            .Where(c => c.HasEcho)
            .Cast<CardModel>()
            .ToList();

        if (songPool.Count == 0)
            return;

        CardModel card = CardFactory.GetDistinctForCombat(
            player,
            songPool,
            1,
            player.RunState.Rng.CombatCardGeneration).FirstOrDefault();

        if (card == null)
            return;

        // 该歌声牌本场战斗费用 -1
        card.EnergyCost.AddThisCombat(-1);

        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player, CardPilePosition.Top);

        player.PlayerCombatState?.RecalculateCardValues();
    }

    /// <summary>升级：格挡 12 → 16。</summary>
    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(4m);
    }

    // ====================== 官方标准代币生成方法 ======================
    public static async Task<CardModel> CreateInHand(Player owner, ICombatState combatState)
    {
        return (await CreateInHand(owner, 1, combatState)).FirstOrDefault();
    }

    public static async Task<IEnumerable<CardModel>> CreateInHand(Player owner, int count, ICombatState combatState)
    {
        if (count == 0 || CombatManager.Instance.IsOverOrEnding)
            return System.Array.Empty<CardModel>();

        List<CardModel> supportCards = new List<CardModel>();
        for (int i = 0; i < count; i++)
        {
            supportCards.Add(combatState.CreateCard<Performance>(owner));
        }

        await CardPileCmd.AddGeneratedCardsToCombat(supportCards, PileType.Hand, owner);
        return supportCards;
    }
}

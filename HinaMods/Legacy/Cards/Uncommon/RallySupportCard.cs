using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using Kaguya.HinaMods;
using Kaguya.HinaMods.Legacy.Cards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Cards.Uncommon;

public sealed class LegacyRallySupportCard : HinaModsCard
{
    // 官方标准：X费卡牌
    protected override bool HasEnergyCostX => true;

    // 官方标准构造函数
    public LegacyRallySupportCard()
        : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }
    public override HashSet<string> CustomTags => new HashSet<string>() { LegacyCustomCardTags.SUPPORTCARD };
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player player = Owner;
        if (player == null) return;

        // 官方标准：获取X费用数值
        int xValue = ResolveEnergyXValue();
        // 官方标准：升级后数值+1（复刻Tempest）
        if (IsUpgraded)
            xValue++;

        // 从官方TokenCardPool获取支援代币卡
        List<CardModel> pool = GetOfficialSupportTokenPool();
        if (pool.Count == 0) return;

        // 随机获取卡牌
        List<CardModel> cards = CardFactory.GetDistinctForCombat(
            player,
            pool,
            xValue,
            player.RunState.Rng.CombatCardGeneration
        ).ToList();

        // 🔥 修复：官方标准加入手牌 参数规范
        foreach (var card in cards)
        {
            await CardPileCmd.AddGeneratedCardToCombat(
                card,
                PileType.Hand,
                player,
                CardPilePosition.Top);
        }
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
    }

    // 从官方TokenCardPool筛选 支援标签代币卡
    private List<CardModel> GetOfficialSupportTokenPool()
    {
        TokenCardPool tokenPool = ModelDb.CardPool<TokenCardPool>();

        return tokenPool.AllCardIds
            .Select(id => ModelDb.GetById<CardModel>(id))
            .OfType<HinaModsCard>()
            .Where(c => c.CustomTags?.Contains(LegacyCustomCardTags.SUPPORT) == true)
            .Cast<CardModel>()
            .ToList();
    }
}
using BaseLib.Abstracts;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using Kaguya.HinaMods;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Cards;

public sealed class LegacySupportBurst : HinaModsCard
{
    private const int _discardCount = 2;

    public LegacySupportBurst()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    { }
    public override HashSet<string> CustomTags => new HashSet<string>() { LegacyCustomCardTags.SUPPORTCARD };
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        // 丢弃2张手牌
        await CardCmd.Discard(
            choiceContext,
            await CardSelectCmd.FromHandForDiscard(
                choiceContext,
                Owner,
                new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, _discardCount),
                null,
                this
            )
        );

        // 固定获得 2 张，不再随升级变化
        int count = 2;

        List<CardModel> supportPool = GetOfficialSupportTokenPool();
        if (supportPool.Any())
        {
            var randomCards = CardFactory.GetDistinctForCombat(
                Owner,
                supportPool,
                count,
                Owner.RunState.Rng.CombatCardGeneration
            );

            foreach (var card in randomCards)
            {
                // 升级后支援牌自动升级
                if (IsUpgraded)
                {
                    CardCmd.Upgrade(card);
                }

                // 🔥 修复：官方标准参数（联机稳定）
                await CardPileCmd.AddGeneratedCardToCombat(
                    card,
                    PileType.Hand,
                    Owner,
                    CardPilePosition.Top);
            }
        }
    }

    protected override void OnUpgrade()
    {
    }

    // 支援代币池
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
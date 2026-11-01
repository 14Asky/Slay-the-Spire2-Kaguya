using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using Kaguya.HinaMods;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 跃击（LeapingStrike）
/// 本地化键：KAGUYA-LEAP_STRIKE.title / .description
/// 1费 · 攻击牌 · 罕见 · 目标：单个敌人
/// 卡面：造成6点伤害。
///       将随机1张[blue]支援[/blue]攻击牌加入[gold]手牌[/gold]并打出。
/// 代码实测：Damage=6；CardCount=1
/// 升级：Damage +3（6→9）；获得的支援攻击牌为升级版（张数仍 1）
/// </summary>
public class LeapingStrike() : HinaModsCard(1,
    CardType.Attack,
    CardRarity.Uncommon,
    TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(6m, ValueProp.Move),
        new DynamicVar("CardCount", 1m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null) return;

        // 基础单体伤害
        await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);

        // 随机支援攻击牌 → 加入手牌并打出
        int count = (int)DynamicVars["CardCount"].BaseValue;
        List<CardModel> pool = GetSupportAttackPool();
        if (pool.Count == 0) return;

        var cards = CardFactory.GetDistinctForCombat(
            Owner,
            pool,
            count,
            Owner.RunState.Rng.CombatCardGeneration
        ).ToList();

        foreach (var card in cards)
        {
            // 升级后：获得的是升级版支援攻击牌
            if (IsUpgraded && card.IsMutable && card.IsUpgradable && !card.IsUpgraded)
            {
                card.UpgradeInternal();
                card.FinalizeUpgradeInternal();
            }

            await CardPileCmd.AddGeneratedCardToCombat(
                card,
                PileType.Hand,
                Owner,
                CardPilePosition.Top);

            await CardCmd.AutoPlay(choiceContext, card, null);
        }
    }

    // 升级：伤害 6 → 9（张数仍 1，但获得的是升级版）
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }

    // 支援 + 攻击 类型的代币池
    private List<CardModel> GetSupportAttackPool()
    {
        TokenCardPool tokenPool = ModelDb.CardPool<TokenCardPool>();

        return tokenPool.AllCardIds
            .Select(id => ModelDb.GetById<CardModel>(id))
            .OfType<HinaModsCard>()
            .Where(c => c.CustomTags?.Contains(CustomCardTags.SUPPORT) == true
                && c.Type == CardType.Attack)
            .Cast<CardModel>()
            .ToList();
    }
}

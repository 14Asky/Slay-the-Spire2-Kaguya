using Kaguya.HinaMods;
using Kaguya.HinaMods.Cards;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Relics;

/// <summary>
/// 八千代的折扇（YachiyoFanRelic）
/// 本地化键：KAGUYA-YACHIYO_FAN_RELIC.title / .description
/// 卡面：每回合当你的能量变为0时，随机获得1张升级后的0费[blue]支援[/blue]牌。
/// 代码实测：每回合限一次；判定 = 花费能量后当前能量为 0；
///           随机池 = 全部 0 费支援牌；入手前先升级。
/// </summary>
public sealed class YachiyoFanRelic : HinaRelics
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    public override string PackedIconPath => "res://images/hinamods/relics/yachiyo_fan_relic.png";
    protected override string PackedIconOutlinePath => "res://images/hinamods/relics/yachiyo_fan_relic.png";
    protected override string BigIconPath => "res://images/hinamods/relics/yachiyo_fan_relic.png";

    private bool _hasTriggeredThisTurn;

    public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(Owner.Creature))
            return Task.CompletedTask;

        _hasTriggeredThisTurn = false;
        return Task.CompletedTask;
    }

    /// <summary>花费能量后检查：当前能量为 0 且本回合未触发过 → 给 1 张升级后的 0 费支援牌。</summary>
    public override async Task AfterEnergySpent(CardModel card, int amount)
    {
        await base.AfterEnergySpent(card, amount);

        if (_hasTriggeredThisTurn) return;
        // ★ 必须【真正花费了能量】才继续：引擎的 SpendEnergy 里 amount == 0 时
        //   不会扣能量，但 Hook.AfterEnergySpent 仍会被调用（0 费牌打出时 amount = 0）。
        //   若不拦这一条，能量本来就为 0 时打出任意 0 费牌都会误触发「能量变为 0」。
        if (amount <= 0) return;
        if (Owner?.PlayerCombatState == null) return;
        // 多人安全：只处理【自己】打出卡牌花费的能量（card 为 null 时不触发）
        if (card?.Owner != Owner) return;
        if (Owner.PlayerCombatState.Energy != 0) return;

        Player player = Owner;
        if (player.Creature == null || !player.Creature.IsAlive) return;

        List<CardModel> pool = ModelDb.AllCards
            .OfType<HinaModsCard>()
            .Where(c => c.CustomTags?.Contains(CustomCardTags.SUPPORT) == true
                        && c.EnergyCost.Canonical == 0
                        && !c.EnergyCost.CostsX)
            .Cast<CardModel>()
            .ToList();

        if (pool.Count == 0) return;

        List<CardModel> cards = CardFactory.GetDistinctForCombat(
            player, pool, 1, player.RunState.Rng.CombatCardGeneration).ToList();

        if (cards.Count == 0) return;

        _hasTriggeredThisTurn = true;
        Flash();

        foreach (CardModel gift in cards)
        {
            // 升级（战斗中可变克隆，幂等）
            if (gift.IsMutable && gift.IsUpgradable && !gift.IsUpgraded)
            {
                gift.UpgradeInternal();
                gift.FinalizeUpgradeInternal();
            }

            await CardPileCmd.AddGeneratedCardToCombat(gift, PileType.Hand, player, CardPilePosition.Top);
        }
    }
}

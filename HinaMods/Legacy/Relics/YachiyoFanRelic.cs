using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Legacy.Powers;
using Kaguya.HinaMods.Legacy.SupportCards.Common;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.Rooms;
using Kaguya.HinaMods.Legacy.Cards;
using Kaguya.HinaMods.Legacy.Relics;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Relics;

/// <summary>
/// 八千代的折扇
/// 专属遗物
/// 每回合开始时，每有一名带有攻击意图的敌人，将一张【招架】加入你的手牌
/// </summary>
public sealed class LegacyYachiyoFanRelic : HinaRelics
{
    // ====================== 基础配置 ======================
    public override RelicRarity Rarity => RelicRarity.Rare;
    public override bool ShowCounter => false;

    // 图标路径
    public override string PackedIconPath => "res://images/hinamods/relics/yachiyo_fan_relic.png";
    protected override string PackedIconOutlinePath => "res://images/hinamods/relics/yachiyo_fan_relic.png";
    protected override string BigIconPath => "res://images/hinamods/relics/yachiyo_fan_relic.png";


    protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
    {
        HoverTipFactory.FromCard<LegacySupportHeavyBlock>()
    };

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        await base.AfterSideTurnStart(side, participants, combatState);

        // 仅自身回合生效
        if (side != Owner?.Creature.Side || combatState == null)
            return;

        // 🔥 修改：统计【带有攻击意图的敌人数量】
        int attackEnemyCount = CountAttackIntentEnemies(combatState);
        if (attackEnemyCount <= 0)
            return;

        // 🔥 修改：根据敌人数量，生成对应张数的招架
        await CreateParryCards(combatState, attackEnemyCount);

        // 遗物闪光特效
        Flash();
    }

    // ====================== 🔥 修改：统计攻击意图敌人数量（替代原bool判断） ======================
    private int CountAttackIntentEnemies(ICombatState combatState)
    {
        int count = 0;
        if (combatState.Enemies == null || !combatState.Enemies.Any())
            return count;

        foreach (Creature enemy in combatState.Enemies)
        {
            if (enemy.IsDead || !enemy.IsMonster)
                continue;

            var intents = enemy.Monster.NextMove?.Intents;
            if (intents == null)
                continue;

            // 只要该敌人有攻击意图，计数+1
            foreach (var intent in intents)
            {
                if (intent.IntentType == IntentType.Attack)
                {
                    count++;
                    break; // 一个敌人只计数一次
                }
            }
        }
        return count;
    }

    // ====================== 🔥 修改：批量生成招架卡牌 ======================
    private async Task CreateParryCards(ICombatState combatState, int count)
    {
        if (Owner == null || count <= 0)
            return;

        List<CardModel> parryCards = new List<CardModel>();
        // 根据数量创建多张招架
        for (int i = 0; i < count; i++)
        {
            CardModel parryCard = combatState.CreateCard<LegacySupportHeavyBlock>(Owner);
            parryCards.Add(parryCard);
        }

        // 批量加入手牌
        await CardPileCmd.AddGeneratedCardsToCombat(parryCards, PileType.Hand, Owner);
    }

    // ====================== 固定空实现（无修改） ======================
    public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(Owner.Creature))
            return Task.CompletedTask;
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await base.AfterCardPlayed(choiceContext, cardPlay);
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        await base.AfterSideTurnEnd(choiceContext, side, participants);
    }

    public override Task AfterCombatEnd(CombatRoom _)
    {
        return Task.CompletedTask;
    }
}
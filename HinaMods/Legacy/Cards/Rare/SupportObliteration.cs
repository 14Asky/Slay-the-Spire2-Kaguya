using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using Kaguya.HinaMods;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Cards;

public sealed class LegacySupportObliteration : HinaModsCard
{
    // ====================== 🔥 核心修复：删除 static，改为私有实例字段（仅本张牌自己用） ======================
    private int _combatCostReduction = 0;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(66m, ValueProp.Move)
    };

    public LegacySupportObliteration()
        : base(25, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    { }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        await base.AfterCardPlayed(context, cardPlay);

        // 只响应自己玩家的支援牌
        if (cardPlay.Card.Owner != Owner)
            return;

        if (cardPlay.Card is not HinaModsCard hinaCard || !hinaCard.CustomTags.Contains(LegacyCustomCardTags.SUPPORT))
            return;

        // 🔥 修复：用实例字段累加（仅本张牌）
        _combatCostReduction++;

        // 刷新当前这张牌的费用
        RefreshCost();

        // 刷新游戏内费用显示
        Owner?.PlayerCombatState?.RecalculateCardValues();
    }

    // 刷新费用（单场战斗有效）
    public void RefreshCost()
    {
        int cost = IsUpgraded ? 20 : 25;
        // 🔥 修复：用实例字段计算费用
        int newCost = Mathf.Max(cost - _combatCostReduction, 0);
        EnergyCost.SetCustomBaseCost(newCost);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(CombatState)
            .Execute(choiceContext);

        foreach (Creature enemy in CombatState.HittableEnemies)
        {
            if (enemy.IsMonster && enemy.IsSecondaryEnemy)
            {
                await CreatureCmd.Kill(enemy);
            }
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-5);
        DynamicVars.Damage.UpgradeValueBy(22m);
        RefreshCost();
    }

    // ====================== 🔥 新增：战斗结束强制重置（彻底清空本回合减费） ======================
    public override async Task AfterCombatEnd(CombatRoom room)
    {
        await base.AfterCombatEnd(room);
        // 战斗结束，重置本张牌的减费计数器
        _combatCostReduction = 0;
        RefreshCost();
    }
}
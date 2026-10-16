using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using Kaguya.HinaMods;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 涅灭（Annihilation）
/// 本地化键：KAGUYA-SUPPORT_OBLITERATION.title / .description
/// 0费 · 攻击牌 · 稀有 · 目标：全体敌人 · 关键词：Retain
/// 卡面：对全体目标造成6点伤害。
///       本场战斗中每打出一次，费用增加1且伤害翻倍。
/// 代码实测：Damage=6
/// 升级：无
/// </summary>
public sealed class Annihilation : HinaModsCard
{
    private const decimal BaseDamageNormal = 6m;
    private const decimal BaseDamageUpgraded = 8m;
    private const decimal DamageGrowth = 2m;

    /// <summary>此牌已打出的次数 —— 决定当前伤害（基础值 × 2^n）。</summary>
    private int _plays;

    /// <summary>当前费用基数 —— 每打出一次 +1（本场战斗中只增不减）。</summary>
    private int _cost;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(BaseDamageNormal, ValueProp.Move)
    };

    public Annihilation()
        : base(0, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    { }

    public override List<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];

    private decimal BaseDamage => IsUpgraded ? BaseDamageUpgraded : BaseDamageNormal;

    /// <summary>打出 n 次之后的伤害（每次翻倍，向下取整）。</summary>
    private decimal DamageAfterPlays(int plays)
    {
        decimal value = BaseDamage;
        for (int i = 0; i < plays; i++)
        {
            value *= DamageGrowth;
        }
        return decimal.Floor(value);
    }

    /// <summary>把当前费用与伤害同步到卡面。</summary>
    private void RefreshCardValues()
    {
        EnergyCost.SetCustomBaseCost(_cost);
        DynamicVars.Damage.BaseValue = DamageAfterPlays(_plays);
        Owner?.PlayerCombatState?.RecalculateCardValues();
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await base.AfterCardPlayed(choiceContext, cardPlay);
        if (cardPlay.Card != this)
        {
            return;
        }

        _plays++;
        _cost++;
        RefreshCardValues();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(CombatState)
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        // 基础伤害 6 → 8，随后按新的基础值重算（此时 IsUpgraded 已为 true）
        RefreshCardValues();
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        await base.AfterCombatEnd(room);
        // 战斗结束：只清计数，不再触碰 EnergyCost / DynamicVars（此时卡已不可变）
        _plays = 0;
        _cost = 0;
    }
}

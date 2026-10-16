using Kaguya.HinaMods.Powers;
using Kaguya.HinaMods.SupportCards.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Kaguya.HinaMods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 备战（Preparation）
/// 本地化键：KAGUYA-CONFLUENCE.title / .description
/// 1费 · 攻击牌 · 稀有 · 目标：单个敌人 · 关键词：Retain
/// 卡面：造成6点伤害。
///       你每打出一张[blue]支援[/blue]攻击牌，此牌造成伤害+3。
/// 代码实测：Damage=6；SupportBonus=3
/// 升级：SupportBonus +1；Damage +2
/// </summary>
public sealed class Preparation : HinaModsCard
{
    /// <summary>每打出一张【支援攻击牌】增加的伤害（升级后由 OnUpgrade 提升）。</summary>
    private const decimal DamagePerSupportAttack = 3m;

    public override List<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];

    protected override DynamicVar[] CanonicalVars => new[]
    {
        new DamageVar(6m, ValueProp.Move),
        new DynamicVar("SupportBonus", DamagePerSupportAttack)
    };

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            HoverTipFactory.FromCard<SupportStrike>()
        };
    }

    // 构造函数：1费 攻击 稀有 任意敌人 + 保留
    public Preparation()
        : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    /// <summary>
    /// 每打出一张【支援攻击牌】就固定增加伤害（不再按该支援牌的伤害数值累加）。
    /// 只在本牌位于手牌中时生效 —— 打出后本牌离开手牌，不再继续累计。
    /// </summary>
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await base.AfterCardPlayed(choiceContext, cardPlay);

        if (Owner == null || Owner.Creature == null || Owner.Creature.CombatState == null)
            return;

        // 仅手牌中生效
        if (!PileType.Hand.GetPile(Owner).Cards.Contains(this))
            return;

        // 仅本玩家打出的牌
        if (cardPlay.Card == null || cardPlay.Player != Owner)
            return;

        // 自动打出的牌不触发，避免与余音/三色流光等批量自动打出形成循环
        if (cardPlay.IsAutoPlay)
            return;

        // 仅【支援 + 攻击类型】的牌生效
        if (cardPlay.Card is not HinaModsCard supportCard
            || supportCard.CustomTags?.Contains(CustomCardTags.SUPPORT) != true
            || supportCard.Type != CardType.Attack)
            return;

        DynamicVars.Damage.BaseValue += DynamicVars["SupportBonus"].BaseValue;
    }

    /// <summary>升级：基础伤害 6 → 8，且每张支援攻击牌的加成 3 → 4。</summary>
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars["SupportBonus"].UpgradeValueBy(1m);
    }
}

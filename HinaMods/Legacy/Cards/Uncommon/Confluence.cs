using Kaguya.HinaMods.Legacy.Powers;
using Kaguya.HinaMods.Legacy.SupportCards.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Kaguya.HinaMods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Cards;

public sealed class LegacyConfluence : HinaModsCard
{
    public override List<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];
    // 官方标准动态变量（对齐 LegacyDragonRide 格式）
    protected override DynamicVar[] CanonicalVars => new[]
    {
        new DamageVar(1m, ValueProp.Move)
    };
    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            HoverTipFactory.FromCard<LegacySupportStrike>()
        };
    }
    // 金光特效：可打出时发光
    protected override bool ShouldGlowGoldInternal => IsPlayable;

    // 打出限制：伤害≥20 + 在手牌中
    protected override bool IsPlayable =>
        base.IsPlayable
        && PileType.Hand.GetPile(Owner).Cards.Contains(this)
        && DynamicVars.Damage.BaseValue >= 20;

    // 构造函数：2费 攻击 罕见 任意敌人 + 保留
    public LegacyConfluence()
        : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {}

    // 官方标准攻击逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    // 核心：监听支援打击，累加伤害
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await base.AfterCardPlayed(choiceContext, cardPlay);

        // 仅手牌中 + 打出支援打击时生效
        if (!PileType.Hand.GetPile(Owner).Cards.Contains(this)
            || cardPlay.Card is not LegacySupportStrike supportCard)
            return;

        // 兼容获取支援打击伤害
        decimal damage = 0m;
        if (supportCard.DynamicVars.ContainsKey("CalculatedDamage"))
            damage = supportCard.DynamicVars.CalculatedDamage.Calculate(null);
        else if (supportCard.DynamicVars.ContainsKey("Damage"))
            damage = supportCard.DynamicVars.Damage.BaseValue;

        // 官方伤害修正钩子
        damage = Hook.ModifyDamage(Owner.RunState, Owner.Creature.CombatState, null,
            Owner.Creature, damage, ValueProp.Move, supportCard, null, ModifyDamageHookType.All,
            CardPreviewMode.None, out IEnumerable<AbstractModel> _);

        // 累加伤害到汇流
        DynamicVars.Damage.BaseValue += damage;
    }

    // 升级：费用-1（对齐你要求的写法）
    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
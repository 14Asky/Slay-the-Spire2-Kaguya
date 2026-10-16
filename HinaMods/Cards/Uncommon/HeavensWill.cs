
using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Kaguya.HinaMods;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 天意（HeavensWill）
/// 本地化键：KAGUYA-MOONLIGHT_SLASH.title / .description
/// 1费 · 攻击牌 · 罕见 · 目标：单个敌人
/// 卡面：造成9点伤害，抽1张牌。
///       若这张牌是通过抽牌到手牌，则自动打出该牌并获得3层[gold]月夜[/gold]。
/// 代码实测：Damage=9；Cards=1
/// 升级：Damage +3
/// </summary>
public sealed class HeavensWill : HinaModsCard
{
    /// <summary>通过抽牌到手时额外获得的月夜层数。</summary>
    private const decimal DrawnMoonlight = 3m;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(9m, ValueProp.Move),
        new CardsVar(1)
    ];

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            HoverTipFactory.FromPower<MoonlightPower>()
        };
    }

    public HeavensWill()
        : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    { }

    /// <summary>通过「效果抽牌」进入手牌时：自动打出本牌并获得 3 层月夜。</summary>
    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        await base.AfterCardDrawn(choiceContext, card, fromHandDraw);

        // fromHandDraw = true 是回合开始的抽牌阶段，只有「效果抽牌」才触发
        if (card != this || fromHandDraw || Owner == null || Owner.Creature.IsDead)
            return;

        // 自动打出本牌（指定目标牌由引擎随机取可命中敌人）
        await CardPileCmd.Add(this, PileType.Play);
        await CardCmd.AutoPlay(choiceContext, this, null);

        await PowerCmd.Apply<MoonlightPower>(
            choiceContext,
            Owner.Creature,
            DrawnMoonlight,
            Owner.Creature,
            this);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);      // 9 → 12
    }
}

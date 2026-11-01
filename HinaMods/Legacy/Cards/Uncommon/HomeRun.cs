using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Legacy.Powers;
using Kaguya.HinaMods.Legacy.SupportCards.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Kaguya.HinaMods.Legacy.Cards;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Cards.Uncommon;

public sealed class LegacyHomeRun : HinaModsCard
{
    private const decimal BaseDamage = 10m;
    private const decimal BaseIncreasePerSupport = 2m;

    // 构造函数：2费 攻击 普通 全体敌人
    public LegacyHomeRun()
        : base(2, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
    {
    }

    public override HashSet<string> CustomTags => new HashSet<string>() { LegacyCustomCardTags.SUPPORTCARD };

   

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CalculationBaseVar(BaseDamage),
        new ExtraDamageVar(0m),
        new IntVar("IncreasePerSupport", BaseIncreasePerSupport),
        new CalculatedDamageVar(ValueProp.Move)
            .WithMultiplier((CardModel card, Creature _) => 1)
    ];


    // 核心：全体攻击
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);
    }

    // 当打出支援攻击牌时，增加伤害
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await base.AfterCardPlayed(choiceContext, cardPlay);

        // 1. 必须是自己的牌
        if (cardPlay.Card.Owner != Owner)
            return;
        // 2. 必须是模组卡牌（才能使用 CustomTags）
        if (cardPlay.Card is not HinaModsCard supportCard)
            return;
        // 3. 必须是攻击牌
        if (supportCard.Type != CardType.Attack)
            return;
        // 4. 严格匹配自定义支援标签
        if (supportCard.CustomTags?.Contains(LegacyCustomCardTags.SUPPORT) != true)
            return;

        // 满足条件，增加伤害
        decimal add = DynamicVars["IncreasePerSupport"].BaseValue;
        DynamicVars.CalculationBase.BaseValue += add;
    }

    // 升级：基础伤害+3
    protected override void OnUpgrade()
    {
        DynamicVars.CalculationBase.UpgradeValueBy(3m);
    }
}

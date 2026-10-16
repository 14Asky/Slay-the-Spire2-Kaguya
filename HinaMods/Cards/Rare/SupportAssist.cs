// ⚠ 本文件整份为注释（无有效代码），对应卡牌未实装。
//using BaseLib.Utils;
//using Godot;
//using MegaCrit.Sts2.Core.Commands;
//using MegaCrit.Sts2.Core.Entities.Cards;
//using MegaCrit.Sts2.Core.Entities.Creatures;
//using MegaCrit.Sts2.Core.GameActions.Multiplayer;
//using MegaCrit.Sts2.Core.Localization.DynamicVars;
//using MegaCrit.Sts2.Core.Models;
//using MegaCrit.Sts2.Core.ValueProps;
//using Kaguya.HinaMods;
//using Kaguya.HinaMods.Cards;
//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;

//namespace Kaguya.HinaMods.Cards.Uncommon;

//public sealed class SupportAssist : HinaModsCard
//{
//    private const decimal BaseDamage = 1m;
//    private const decimal IncreasePerSupport = 2m;

//    protected override IEnumerable<DynamicVar> CanonicalVars =>
//    [
//        new CalculationBaseVar(BaseDamage),
//        new ExtraDamageVar(0m),
//        new IntVar("IncreasePerSupport", IncreasePerSupport),
//        new CalculatedDamageVar(ValueProp.Move)
//            .WithMultiplier((CardModel card, Creature _) => 1)
//    ];

//    public SupportAssist()
//        : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
//    { }

//    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
//    {
//        ArgumentNullException.ThrowIfNull(cardPlay.Target);
//        await DamageCmd.Attack(DynamicVars.CalculatedDamage)
//            .FromCard(this, cardPlay)
//            .Targeting(cardPlay.Target)
//            .WithHitFx("vfx/vfx_attack_slash")
//            .Execute(choiceContext);
//    }

//    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
//    {
//        await base.AfterCardPlayed(choiceContext, cardPlay);
//        if (cardPlay.Card is not HinaModsCard hinaCard || !hinaCard.CustomTags.Contains(CustomCardTags.SUPPORT))
//            return;

//        decimal add = DynamicVars["IncreasePerSupport"].BaseValue;
//        DynamicVars.CalculationBase.BaseValue += add;
//    }

    //    protected override void OnUpgrade()
    //    {
    //        EnergyCost.UpgradeBy(-1);
    //    }
//}
using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Legacy.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Kaguya.HinaMods;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Cards;

public sealed class LegacyHinaModsMoonLeverage : HinaModsCard
{
    private const string _strengthLossKey = "StrengthLoss";

    public override List<CardKeyword> CanonicalKeywords => [
        CardKeyword.Retain
    ];
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("StrengthLoss", 4m)
    };

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            HoverTipFactory.FromPower<LegacyFortunePower>()
        };
    }

    protected override bool IsPlayable
    {
        get
        {
            if (!base.IsPlayable)
                return false;

            LegacyFortunePower fortunePower = base.Owner.Creature.GetPower<LegacyFortunePower>();
            return fortunePower != null && fortunePower.Amount >= 1;
        }
    }

    protected override bool ShouldGlowGoldInternal => IsPlayable;

    public LegacyHinaModsMoonLeverage()
        : base(1, CardType.Skill, CardRarity.Basic, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        LegacyFortunePower fortunePower = base.Owner.Creature.GetPower<LegacyFortunePower>();

        if (cardPlay.IsFirstInSeries)
        {
            if (fortunePower != null && fortunePower.Amount >= 1)
            {
                // 🔥 修复：添加 choiceContext 作为第一个参数
                await PowerCmd.ModifyAmount(
                    choiceContext, // 新增
                    fortunePower,
                    -1m,
                    base.Owner.Creature,
                    this
                );
            }
        }

        // 🔥 修复：添加 choiceContext 作为第一个参数
        await PowerCmd.Apply<LegacyTempStrengthDown>(
            choiceContext, // 新增
            cardPlay.Target,
            base.DynamicVars["StrengthLoss"].BaseValue,
            base.Owner.Creature,
            this
        );
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars["StrengthLoss"].UpgradeValueBy(2m);
    }
}
using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Legacy.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Cards;

/// <summary>
/// 月华团队祝福
/// 0费 技能牌 | 全体盟友目标 | 多人联机专属
/// 消耗3层月夜，为全体盟友施加1层力量和1层敏捷。消耗。
/// 升级：力量变为2层，敏捷变为2层。
/// </summary>
public sealed class LegacyMoonlightTeamBless : HinaModsCard
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;
    public override List<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<LegacyFortunePower>(3m),
        new PowerVar<StrengthPower>(1m),
        new PowerVar<DexterityPower>(1m)
    ];

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
           HoverTipFactory.FromPower<LegacyFortunePower>(),
           HoverTipFactory.FromPower<StrengthPower>(),
           HoverTipFactory.FromPower<DexterityPower>()
        };
    }

    protected override bool IsPlayable
    {
        get
        {
            if (!base.IsPlayable)
                return false;

            decimal requiredMoon = base.DynamicVars["LegacyFortunePower"].BaseValue;
            LegacyFortunePower fortunePower = base.Owner.Creature.GetPower<LegacyFortunePower>();

            return fortunePower != null && fortunePower.Amount >= requiredMoon;
        }
    }

    protected override bool ShouldGlowGoldInternal => IsPlayable;

    public LegacyMoonlightTeamBless()
        : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.AllAllies)
    { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        decimal cost = base.DynamicVars["LegacyFortunePower"].BaseValue;
        LegacyFortunePower fortunePower = base.Owner.Creature.GetPower<LegacyFortunePower>();

        if (cardPlay.IsFirstInSeries)
        {
            if (fortunePower != null && fortunePower.Amount >= cost)
            {
                // 修复：添加 choiceContext 参数
                await PowerCmd.ModifyAmount(
                    choiceContext,
                    fortunePower,
                    -cost,
                    base.Owner.Creature,
                    this);
            }
        }

        IEnumerable<Creature> teammates = from c in CombatState.GetTeammatesOf(Owner.Creature)
                                          where c != null && c.IsAlive && c.IsPlayer
                                          select c;

        foreach (Creature playerCreature in teammates)
        {
            // 修复：添加 choiceContext 参数
            await PowerCmd.Apply<StrengthPower>(
                choiceContext,
                playerCreature,
                base.DynamicVars["StrengthPower"].BaseValue,
                base.Owner.Creature,
                this
            );

            // 修复：添加 choiceContext 参数
            await PowerCmd.Apply<DexterityPower>(
                choiceContext,
                playerCreature,
                base.DynamicVars["DexterityPower"].BaseValue,
                base.Owner.Creature,
                this
            );
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["StrengthPower"].UpgradeValueBy(1m);
        DynamicVars["DexterityPower"].UpgradeValueBy(1m);
    }
}
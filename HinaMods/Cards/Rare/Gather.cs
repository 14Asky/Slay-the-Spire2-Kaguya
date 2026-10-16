using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Powers;
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

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 集结（Gather）
/// 本地化键：KAGUYA-MOONLIGHT_TEAM_BLESS.title / .description
/// 0费 · 技能牌 · 罕见 · 目标：AllAllies · 关键词：Exhaust
/// 卡面：[gold]月夜3[/gold]：所有玩家获得1层[gold]力量[/gold]和1层[gold]敏捷[/gold]。
/// 代码实测：MoonCost=3；StrengthPower=1；DexterityPower=1
/// 升级：力量 +1；敏捷 +1
/// </summary>
public sealed class Gather : HinaModsCard
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;
    public override List<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<MoonlightPower>(MoonCostVarName, 3m),
        new PowerVar<StrengthPower>(1m),
        new PowerVar<DexterityPower>(1m)
    ];

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
           HoverTipFactory.FromPower<MoonlightPower>(),
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

            // 「获得+免费」的牌不消耗月夜，可直接打出
            if (SkipsMoonlightCost)
                return true;

            decimal requiredMoon = base.DynamicVars[MoonCostVarName].BaseValue;
            MoonlightPower fortunePower = base.Owner.Creature.GetPower<MoonlightPower>();

            return fortunePower != null && fortunePower.Amount >= requiredMoon;
        }
    }

    protected override bool ShouldGlowGoldInternal => IsPlayable;

    public Gather()
        : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.AllAllies)
    { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        decimal cost = base.DynamicVars[MoonCostVarName].BaseValue;

        // 月夜N：非首次打出，或层数不足，则整个效果都不触发（豁免时 cost=0 直接放行）
        if (!cardPlay.IsFirstInSeries)
            return;
        if (!await TryConsumeMoonlight(choiceContext, cost, this))
            return;

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
using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 游戏人间（PlayfulLife）
/// 本地化键：KAGUYA-PLAYFUL_LIFE.title / .description
/// 0费 · 技能牌 · 罕见 · 目标：自身
/// 卡面：本回合每获得1层[gold]月夜[/gold]，获得{PlayfulLifePower:diff()}层[gold]格挡[/gold]。
///       获得当前回合数的[gold]月夜[/gold]。
/// 升级：格挡 1 → 2（获得的月夜数量不变）
/// </summary>
public sealed class PlayfulLife : HinaModsCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<PlayfulLifePower>(1m)
    ];

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            HoverTipFactory.FromPower<PlayfulLifePower>(),
            HoverTipFactory.FromPower<MoonlightPower>()
        };
    }

    // 构造函数：0费 技能 罕见 目标自身
    public PlayfulLife()
        : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 顺序很重要：先挂「本回合获得月夜 → 格挡」，再获得月夜，
        // 这样这一笔月夜也能吃到格挡（反过来的话会少一次）。
        await PowerCmd.Apply<PlayfulLifePower>(
            choiceContext,
            Owner.Creature,
            DynamicVars[nameof(PlayfulLifePower)].BaseValue,
            Owner.Creature,
            this);

        // 获得等同于当前回合数的月夜
        await PowerCmd.Apply<MoonlightPower>(
            choiceContext,
            Owner.Creature,
            CombatState.RoundNumber,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars[nameof(PlayfulLifePower)].UpgradeValueBy(1m);
    }
}

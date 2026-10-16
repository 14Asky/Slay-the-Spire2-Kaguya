using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Kaguya.HinaMods;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 天人形态（CelestialForm）
/// 本地化键：KAGUYA-TSUKIMI_FORM.title / .description
/// 3费 · 能力牌 · 稀有 · 目标：自身
/// 卡面：每当你获得[gold]月夜[/gold]时，额外获得该次获得量50%的[gold]月夜[/gold]。
/// 代码实测：CelestialFormPower=50
/// 升级：CelestialFormPower +50
/// </summary>
public sealed class CelestialForm : HinaModsCard
{
    // 动态变量：额外月夜的百分比（基础 50，升级 100）
    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new PowerVar<CelestialFormPower>(50m)
    };

    // 3费 能力牌 稀有
    public CelestialForm()
        : base(3, CardType.Power, CardRarity.Rare, TargetType.Self)
    { }

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            HoverTipFactory.FromPower<CelestialFormPower>(),
            HoverTipFactory.FromPower<MoonlightPower>()
        };
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player player = Owner;
        if (player == null) return;

        // 施法动画
        await CreatureCmd.TriggerAnim(player.Creature, "Cast", player.Character.CastAnimDelay);

        await PowerCmd.Apply<CelestialFormPower>(
            choiceContext,
            player.Creature,
            DynamicVars[nameof(CelestialFormPower)].BaseValue,
            player.Creature,
            this);
    }

    // 升级：比例 50% → 100%
    protected override void OnUpgrade()
    {
        DynamicVars[nameof(CelestialFormPower)].UpgradeValueBy(50m);
    }
}

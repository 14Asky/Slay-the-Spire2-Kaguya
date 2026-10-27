using BaseLib.Utils;
using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 满月（FullMoon）
/// 本地化键：KAGUYA-FULL_MOON.title / .description
/// 1费 · 技能牌 · 罕见 · 目标：自身 · 无关键词
/// 卡面：[gold]月夜[/gold]{MoonCost:diff()}：使所有能量低于{EnergyTo:diff()}点的[gold]友方[/gold]回复到{EnergyTo:diff()}点能量。
/// 代码实测：MoonCost=5；EnergyTo=3
/// 升级：月夜 5 → 3
/// 说明：只补差额（当前能量已 ≥3 的友方不受影响），不会把多出来的能量削掉。
/// </summary>
public sealed class FullMoon : HinaModsCard
{
    private const string EnergyToVar = "EnergyTo";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<MoonlightPower>(MoonCostVarName, 5m),
        new DynamicVar(EnergyToVar, 3m)
    ];

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            HoverTipFactory.FromPower<MoonlightPower>()
        };
    }

    /// <summary>多人联机专用：单人模式下不会出现在卡池里。</summary>
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public FullMoon()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    { }

    /// <summary>
    /// 月夜不足时打不出去（与「集结 / 蓦然回首 / 旗开得胜」同一套门槛写法）。
    /// ⚠ 门槛用 <see cref="HinaModsCard.MoonRequirement"/>（免费置 0 之前的原值）判断：
    ///   「获得+免费 / 药水免费」只免掉**消耗**，不会把「月夜5」这个门槛一并免掉，
    ///   否则这张牌就能在 0 月夜时被免费打出去。
    /// </summary>
    protected override bool IsPlayable
    {
        get
        {
            if (!base.IsPlayable)
                return false;

            decimal required = MoonRequirement;

            // 月夜为 0 层且本牌不消耗月夜（例如本来就是月夜0）时才允许没有 Power 的情况
            if (required <= 0m)
                return true;

            MoonlightPower? moon = Owner?.Creature?.GetPower<MoonlightPower>();
            return moon != null && moon.Amount >= required;
        }
    }

    protected override bool ShouldGlowGoldInternal => IsPlayable;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 免费打出（获得+免费 / 药水免费）时只免消耗；「月夜5」的门槛已在 IsPlayable 里保证过
        decimal need = SkipsMoonlightCost ? 0m : DynamicVars[MoonCostVarName].BaseValue;
        if (!await TryConsumeMoonlight(choiceContext, need, this))
            return;

        int targetEnergy = (int)DynamicVars[EnergyToVar].BaseValue;
        var combat = CombatState;
        if (combat == null)
            return;

        // 「所有友方」= 联机里同一侧的所有玩家；单人时只有自己一个
        foreach (Player player in combat.Players)
        {
            if (player?.PlayerCombatState == null)
                continue;

            // 只处理「能量低于目标值」的友方：已经达到或超过 3 点的完全不受影响
            int current = player.PlayerCombatState.Energy;
            if (current >= targetEnergy)
                continue;

            await PlayerCmd.GainEnergy(targetEnergy - current, player);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars[MoonCostVarName].UpgradeValueBy(-2m);   // 5 → 3
    }
}

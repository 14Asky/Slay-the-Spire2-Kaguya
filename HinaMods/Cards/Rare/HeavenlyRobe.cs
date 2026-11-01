using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 天之羽衣（HeavenlyRobe）
/// 本地化键：KAGUYA-TSUKIMI_ROBE.title / .description
/// 3费 · 能力牌 · 稀有 · 目标：自身
/// 卡面：在你的回合结束时，你每有1层[gold]月夜[/gold]，获得1点格挡。
/// 代码实测：HeavenlyRobePower=1
/// 升级：费用 -1
/// </summary>
public sealed class HeavenlyRobe : HinaModsCard
{
    // 动态变量：每层月夜获得的格挡数
    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new PowerVar<HeavenlyRobePower>(1m)
    };

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            HoverTipFactory.FromPower<HeavenlyRobePower>(),
            HoverTipFactory.FromPower<MoonlightPower>()
        };
    }

    public HeavenlyRobe()
        : base(3, CardType.Power, CardRarity.Rare, TargetType.Self)
    { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player player = Owner;
        if (player == null) return;

        // 施法动画
        await CreatureCmd.TriggerAnim(player.Creature, "Cast", player.Character.CastAnimDelay);

        await PowerCmd.Apply<HeavenlyRobePower>(
            choiceContext,
            player.Creature,
            DynamicVars[nameof(HeavenlyRobePower)].BaseValue,
            player.Creature,
            this
        );
    }

    // 升级：费用 -1
    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}

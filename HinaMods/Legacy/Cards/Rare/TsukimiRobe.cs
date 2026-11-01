using Kaguya.HinaMods.Legacy.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Kaguya.HinaMods.Legacy.Powers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Cards;

/// <summary>
/// 月见长袍
/// 3费 能力牌 | 自身目标
/// 获得1层月见长袍。
/// 升级：获得2层月见长袍。
/// </summary>
public sealed class LegacyTsukimiRobe : HinaModsCard
{
    // 动态变量：基础倍率1，对标群蛇形态规范
    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new PowerVar<LegacyTsukimiRobeBuff>(1m)
    };
    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            HoverTipFactory.FromPower<LegacyTsukimiRobeBuff>(),
            HoverTipFactory.FromPower<LegacyFortunePower>()
        };
    }

    public LegacyTsukimiRobe()
        : base(3, CardType.Power, CardRarity.Rare, TargetType.Self)
    { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player player = Owner;
        if (player == null) return;

        // 施法动画
        await CreatureCmd.TriggerAnim(player.Creature, "Cast", player.Character.CastAnimDelay);

        // 🔥 修复：补全官方强制参数 choiceContext
        await PowerCmd.Apply<LegacyTsukimiRobeBuff>(
            choiceContext,
            player.Creature,
            DynamicVars[nameof(LegacyTsukimiRobeBuff)].BaseValue,
            player.Creature,
            this
        );
    }

    protected override void OnUpgrade()
    {
        DynamicVars[nameof(LegacyTsukimiRobeBuff)].UpgradeValueBy(1m);
    }
}
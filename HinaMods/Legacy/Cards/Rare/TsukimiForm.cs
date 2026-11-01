using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Legacy.Powers;
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

namespace Kaguya.HinaMods.Legacy.Cards;

/// <summary>
/// 月见形态
/// 3费 能力牌 | 自身目标
/// 获得2层月见形态。
/// 升级：获得3层月见形态。
/// </summary>
public sealed class LegacyTsukimiForm : HinaModsCard
{
    // 动态变量：对标群蛇形态，控制Power层数（回合结束月夜值）
    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new PowerVar<LegacyTsukimiFormPower>(2m)
    };
    // 3费 能力牌 稀有
    public LegacyTsukimiForm()
        : base(3, CardType.Power, CardRarity.Rare, TargetType.Self)
    { }

    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            HoverTipFactory.FromPower<LegacyTsukimiFormPower>(),
            HoverTipFactory.FromPower<LegacyFortunePower>()
        };
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player player = Owner;
        if (player == null) return;

        // 施法动画
        await CreatureCmd.TriggerAnim(player.Creature, "Cast", player.Character.CastAnimDelay);

        // 🔥 修复：补全官方必需的 choiceContext 参数
        await PowerCmd.Apply<LegacyTsukimiFormPower>(
            choiceContext,
            player.Creature,
            DynamicVars[nameof(LegacyTsukimiFormPower)].BaseValue,
            player.Creature,
            this);
    }

    // 升级：Power层数+1
    protected override void OnUpgrade()
    {
        DynamicVars[nameof(LegacyTsukimiFormPower)].UpgradeValueBy(1m);
    }
}
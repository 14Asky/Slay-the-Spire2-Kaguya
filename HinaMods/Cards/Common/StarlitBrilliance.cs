using Kaguya.HinaMods.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using Kaguya.HinaMods.Powers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 星夜璀璨（StarlitBrilliance）
/// 本地化键：KAGUYA-TSUKIMI_DRAW.title / .description
/// 2费 · 能力牌 · 罕见 · 目标：自身
/// 卡面：每当你抽到一张牌时，获得1层[gold]月夜[/gold]。
/// 代码实测：StarlitBrilliancePower=1
/// 升级：费用 -1
/// </summary>
public sealed class StarlitBrilliance : HinaModsCard
{
    

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new PowerVar<StarlitBrilliancePower>(1m);
        }
    }

    public StarlitBrilliance()
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }
    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            HoverTipFactory.FromPower<MoonlightPower>(),
            HoverTipFactory.FromPower<StarlitBrilliancePower>()
        };
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 修复：添加 choiceContext 参数
        await PowerCmd.Apply<StarlitBrilliancePower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["StarlitBrilliancePower"].IntValue,
            Owner.Creature,
            this);
    }

    /// <summary>升级：每 3 张牌获得的月夜 1 → 2。</summary>
    protected override void OnUpgrade()
    {
        DynamicVars[nameof(StarlitBrilliancePower)].UpgradeValueBy(1m);
    }
}
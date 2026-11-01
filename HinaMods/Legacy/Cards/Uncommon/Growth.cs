using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Legacy.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Cards;

public sealed class LegacyGrowth : HinaModsCard
{
    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
             HoverTipFactory.FromPower<LegacyFortunePower>(),
             HoverTipFactory.FromPower<LegacyGrowthPower>(),
        };
    }

    // 能力牌 1费 稀有 目标自身
    public LegacyGrowth() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    // 打出：获得1层【成长】BUFF（无论是否升级都只给1层）
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 🔥 修复：补全官方强制参数 choiceContext
        await PowerCmd.Apply<LegacyGrowthPower>(
            choiceContext,
            Owner.Creature,
            1,
            Owner.Creature,
            this);
    }

    // 升级：费用-1（1费→0费）
    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
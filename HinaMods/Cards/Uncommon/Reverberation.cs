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
using Kaguya.HinaMods.Cards;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards.Uncommon;

/// <summary>
/// 余音（Reverberation）
/// 本地化键：KAGUYA-SINGER_CASCADE.title / .description
/// 3费 · 能力牌 · 罕见 · 目标：自身
/// 卡面：在你的回合结束时，自动打出你本回合打出的最后一张牌。
/// 升级：费用 -1
/// </summary>
public sealed class Reverberation : HinaModsCard
{
    // 按需求：余音不再拥有【歌声】关键词（已移除 ECHO 标签）

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<ReverberationPower>(1m)
    ];

    // 悬浮提示：显示余音Power
    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            HoverTipFactory.FromPower<ReverberationPower>(),
        };
    }

    // 官方标准构造函数：2费 能力 罕见 目标自身
    public Reverberation()
        : base(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    // 核心打出逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player player = Owner;
        if (player == null)
            return;

        // 获得1层余音
        await PowerCmd.Apply<ReverberationPower>(
            choiceContext,
            player.Creature,
            1,
            player.Creature,
            this);
    }

    // 升级逻辑：费用-1（2→1）
    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        EnergyCost.UpgradeBy(-1);
    }
}

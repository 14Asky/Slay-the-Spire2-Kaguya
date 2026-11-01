using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Legacy.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using Kaguya.HinaMods;
using Kaguya.HinaMods.Legacy.Cards;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Cards.Uncommon;

/// <summary>
/// 歌声连奏
/// 3费 技能 稀有
/// 获得1层【余音】
/// 余音：每回合结束时，自动打出你本回合中最后一张歌声牌
/// 升级：费用-1（3→2）
/// </summary>
public sealed class LegacySingerCascade : HinaModsCard
{
    // 自定义标签
    public override HashSet<string> CustomTags => new HashSet<string>() { LegacyCustomCardTags.SINGERCARD };

    // 悬浮提示：显示余音Power（完全对齐Growth写法）
    protected override IEnumerable<IHoverTip> GetCustomHoverTips()
    {
        return new IHoverTip[]
        {
            HoverTipFactory.FromPower<LegacySingerCascadePower>(),
        };
    }

    // 官方标准构造函数：3费 技能 稀有 目标自身
    public LegacySingerCascade()
        : base(3, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    // 核心打出逻辑（完全对齐Growth写法）
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player player = Owner;
        if (player == null)
            return;

        // 获得1层余音（和Growth获得1层成长完全一致）
        await PowerCmd.Apply<LegacySingerCascadePower>(
            choiceContext,
            player.Creature,
            1,
            player.Creature,
            this);
    }

    // 升级逻辑：费用-1（3→2，完全对齐Growth写法）
    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        EnergyCost.UpgradeBy(-1);
    }
}
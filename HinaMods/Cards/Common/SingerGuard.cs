using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Kaguya.HinaMods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Cards;

/// <summary>
/// 基础格挡卡牌 | 歌者标签
/// 1费 获得7点格挡，作为第一张打出时额外获得4点格挡
/// 升级后：基础格挡10点
/// </summary>
public sealed class SingerGuard : HinaModsCard
{
    // 歌者自定义标签（保留）
    public override HashSet<string> CustomTags => new HashSet<string>() { CustomCardTags.SINGER };

    // 官方防御标签（保留）

    // 动态变量：基础7点格挡（标准官方写法）
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(8m, ValueProp.Move)
    ];

    // 构造函数（不变）
    public SingerGuard()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    // 🔥 完全复刻 MoonlightSlash 的官方判断逻辑：本回合第一张牌
    private bool IsFirstCardThisTurn
    {
        get
        {
            // 统计：本回合、自己打出的卡牌总数（和月光斩代码完全一致）
            int playedCount = CombatManager.Instance.History.CardPlaysFinished
                .Count(e => e.HappenedThisTurn(CombatState) && e.CardPlay.Card.Owner == Owner);

            // 已出牌数 = 0 → 真正的第一张牌
            return playedCount == 0;
        }
    }

    // 核心：使用你指定的 CreatureCmd.GainBlock API
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay);

        // 基础格挡
        await CreatureCmd.GainBlock(
            Owner.Creature,
            DynamicVars.Block.BaseValue,
            ValueProp.Move,
            cardPlay
        );

        // 🔥 修复：仅本回合第一张打出时触发额外格挡
        if (IsFirstCardThisTurn)
        {
            await CreatureCmd.GainBlock(
                Owner.Creature,
                4m,
                ValueProp.Move,
                cardPlay
            );
        }
    }

    // 升级逻辑：8 → 12 格挡（保留不变）
    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2m);
    }
}

using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context; // PlayerChoiceContext所在命名空间
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Threading.Tasks;
using Kaguya.HinaMods.Powers;

namespace Kaguya.HinaMods.Powers;

/// <summary>
/// 天之羽衣（HeavenlyRobePower）
/// 本地化键：KAGUYA-TSUKIMI_ROBE_BUFF.title / .description
/// 卡面：回合结束时，每有1层月夜，获得1点格挡。
/// </summary>
public sealed class HeavenlyRobePower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    // ✅ 正确的实例化类型（修复旧版IsInstanced废弃问题）
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    // 图标配置
    public override string CustomPackedIconPath => "res://images/hinamods/Powers/tsukimi_robe_buff.png";
    public override string CustomBigIconPath => "res://images/hinamods/Powers/tsukimi_robe_buff.png";
    public override int DisplayAmount => (int)Amount;

    // 格挡悬浮提示
    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.Static(StaticHoverTip.Block);
        }
    }

    // 🔥 最终修复：100%匹配最新AbstractModel API签名
    public override async Task AfterSideTurnEnd(PlayerChoiceContext ctx, CombatSide side, IEnumerable<Creature> participants)
    {
        // ✅ 正确调用基类方法
        await base.AfterSideTurnEnd(ctx, side, participants);

        // 原有逻辑完全保留
        if (Owner == null || side != Owner.Side) return;

        // 获取月夜层数
        MoonlightPower moonPower = Owner.GetPower<MoonlightPower>();
        int moonStacks = moonPower?.Amount ?? 0;
        if (moonStacks <= 0) return;

        // 核心：月夜层数 × 自身倍率（1/2，由卡牌升级控制）
        int blockAmount = moonStacks * (int)Amount;
        await CreatureCmd.GainBlock(Owner, blockAmount, ValueProp.Unpowered, null, fast: true);
    }
}
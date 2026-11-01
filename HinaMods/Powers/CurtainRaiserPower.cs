using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Kaguya.HinaMods;
using Kaguya.HinaMods.Cards;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Powers;

/// <summary>
/// 开幕（CurtainRaiserPower）
/// 本地化键：KAGUYA-SUPPORT_RHYTHM_POWER.title / .description
/// 卡面：每打出若干张[blue]支援[/blue]牌，抽1张牌（基础4张，【开幕】升级后3张）。
/// </summary>
public sealed class CurtainRaiserPower : CustomPowerModel
{
    // ====================== 官方同款基础配置（完全统一） ======================
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter; // 可叠加
    public override bool AllowNegative => false;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced; // 官方必备：多层独立数据

    // 图标、层数显示（保持不变）
    public override string CustomPackedIconPath => "res://images/hinamods/Powers/support_rhythm_power.png";
    public override string CustomBigIconPath => "res://images/hinamods/Powers/support_rhythm_power.png";

    // ====================== 参考官方：内部独立数据 ======================
    private class Data
    {
        public int supportCardsPlayed; // 已打出支援牌数
        public int triggerCount;      // 已触发次数
    }

    /// <summary>抽 1 张牌所需的支援牌张数。由【开幕】打出时写入：基础 4 / 升级后 3。</summary>
    public int Threshold { get; set; } = 4;

    // 显示剩余需要打出的支援牌数量（官方标准显示）
    public override int DisplayAmount => Threshold - GetInternalData<Data>().supportCardsPlayed % Threshold;

    // 初始化独立数据（每张BUFF数据独立）
    protected override object InitInternalData() => new Data();

    // ====================== 核心逻辑：每4张支援牌抽1牌 ======================
    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        await base.AfterCardPlayed(context, cardPlay);

        // 官方严格判断：仅持有者生效
        if (cardPlay.Card.Owner.Creature != Owner)
            return;

        // 仅支援牌生效
        if (cardPlay.Card is not HinaModsCard modCard || !modCard.CustomTags.Contains(CustomCardTags.SUPPORT))
            return;

        // 获取独立数据
        Data data = GetInternalData<Data>();
        data.supportCardsPlayed++;

        // 计算触发次数（官方原版逻辑）
        int triggers = data.supportCardsPlayed / Threshold - data.triggerCount;
        if (triggers > 0)
        {
            Flash();
            // 每达到阈值抽 1 张
            await CardPileCmd.Draw(context, triggers, Owner.Player);
            data.triggerCount += triggers;
        }

        // 刷新UI数字显示
        InvokeDisplayAmountChanged();
    }
}
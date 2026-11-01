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
using Kaguya.HinaMods.Legacy.Cards;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Powers;

public sealed class LegacySupportRhythmPower : CustomPowerModel
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
    private class LegacyData
    {
        public int supportCardsPlayed; // 已打出支援牌数
        public int triggerCount;      // 已触发次数
    }

    // 🔥 核心修改：每 3 张支援牌触发一次
    private const int TriggerThreshold = 3;

    // 显示剩余需要打出的支援牌数量（官方标准显示）
    public override int DisplayAmount => TriggerThreshold - GetInternalData<LegacyData>().supportCardsPlayed % TriggerThreshold;

    // 初始化独立数据（每张BUFF数据独立）
    protected override object InitInternalData() => new LegacyData();

    // ====================== 核心逻辑：每3张支援牌抽1牌 ======================
    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        await base.AfterCardPlayed(context, cardPlay);

        // 官方严格判断：仅持有者生效
        if (cardPlay.Card.Owner.Creature != Owner)
            return;

        // 仅支援牌生效
        if (cardPlay.Card is not HinaModsCard modCard || !modCard.CustomTags.Contains(LegacyCustomCardTags.SUPPORT))
            return;

        // 获取独立数据
        LegacyData data = GetInternalData<LegacyData>();
        data.supportCardsPlayed++;

        // 计算触发次数（官方原版逻辑）
        int triggers = data.supportCardsPlayed / TriggerThreshold - data.triggerCount;
        if (triggers > 0)
        {
            Flash();
            // 🔥 核心修改：抽牌 = 层数 × 触发次数（叠加生效，官方逻辑）
            await CardPileCmd.Draw(context, (int)Amount * triggers, Owner.Player);
            data.triggerCount += triggers;
        }

        // 刷新UI数字显示
        InvokeDisplayAmountChanged();
    }
}
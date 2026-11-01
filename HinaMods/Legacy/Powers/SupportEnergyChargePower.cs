using BaseLib.Abstracts;
using Godot;
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

public sealed class LegacySupportOrbitPower : CustomPowerModel
{
    // ====================== 官方原版配置 完全一致 ======================
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter; // 可叠加
    public override bool AllowNegative => false;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    // 图标配置（你的原样保留）
    public override string CustomPackedIconPath => "res://images/hinamods/Powers/support_orbit_power.png";
    public override string CustomBigIconPath => "res://images/hinamods/Powers/support_orbit_power.png";

    // 内部数据（官方结构，每张Buff独立计数）
    private class LegacyData
    {
        public int supportCardsPlayed;
        public int triggerCount;
    }

    // 每打出 5 张支援牌触发一次
    private const int TriggerThreshold = 5;

    // 🔥 官方原版显示逻辑：剩余需要打出的牌数
    public override int DisplayAmount => TriggerThreshold - GetInternalData<LegacyData>().supportCardsPlayed % TriggerThreshold;

    // 🔥 官方原版：初始化独立数据
    protected override object InitInternalData() => new LegacyData();

    // ====================== 核心修复：对标官方 AfterCardPlayed ======================
    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        await base.AfterCardPlayed(context, cardPlay);

        // 🔥 官方严格判断：仅持有者生效
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
            // 🔥 终极修复：能量 = 层数 × 触发次数（官方原版逻辑！叠加核心）
            await PlayerCmd.GainEnergy((int)Amount * triggers, Owner.Player);
            data.triggerCount += triggers;
        }

        // 刷新数字显示
        InvokeDisplayAmountChanged();
    }
}
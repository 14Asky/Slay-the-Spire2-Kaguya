using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace Kaguya.HinaMods.Powers;

/// <summary>
/// 交换武器（WeaponSwapPower）
/// 本地化键：KAGUYA-SUPPORT_AURA_POWER.title / .description
/// 效果：本场战斗中，你因打出支援牌获得的正面buff由「临时」变为「永久」
///       （临时力量 → 永久力量，临时敏捷 → 永久敏捷）。
/// 说明：本能力本身不产生任何 buff，只作为开关被 <c>HinaModsCard.GrantSupportBonus</c> 检测：
///       检测到它存在时，直接给 StrengthPower / DexterityPower，不再挂临时力量/敏捷能力。
/// </summary>
public sealed class WeaponSwapPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    // 纯被动开关，不需要显示层数
    public override int DisplayAmount => 0;

    public override string CustomPackedIconPath => "res://images/hinamods/Powers/support_aura.png";
    public override string CustomBigIconPath => "res://images/hinamods/Powers/support_aura.png";
}

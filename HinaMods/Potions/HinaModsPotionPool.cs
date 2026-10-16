namespace Kaguya.HinaMods.Potions;

using BaseLib.Abstracts;
using Kaguya.HinaMods.Extensions;

/// <summary>
/// 辉夜侧药水池：定义药水描述里用的能量图标路径（24x24）。
/// 基础设施类，无本地化键。
/// </summary>
public class HinaModsPotionPool : CustomPotionPoolModel
{
    // 描述中使用的能量图标。大小为24x24。
    public override string BigEnergyIconPath => "Charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "Charui/text_energy.png".ImagePath();

}
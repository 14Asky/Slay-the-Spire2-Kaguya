using BaseLib.Abstracts;
using Godot;
using Kaguya.HinaMods.Character;
using System;

namespace Kaguya.HinaMods.Character;

/// <summary>
/// 辉夜侧遗物池：能量色与轮廓色取自 HinaCharacter 的角色主题色。
/// 基础设施类，无本地化键。
/// </summary>
public partial class HinaModsRelicPool : CustomRelicPoolModel
{
    public override string EnergyColorName => HinaCharacter.CharacterId;

    public override Color LabOutlineColor => HinaCharacter.Color;
}
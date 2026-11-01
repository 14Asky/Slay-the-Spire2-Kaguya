// 严格匹配你的命名空间
using Kaguya.HinaMods.Legacy.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using Kaguya.HinaMods.Legacy.Cards;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
// 🔥 引用 BaseLib 的 CustomPowerModel
using BaseLib.Abstracts;
using System.Threading.Tasks;

namespace Kaguya.HinaMods.Legacy.Powers;

// 🔥 核心：直接继承官方TemporaryStrengthPower（全逻辑复用）
// 实现 ICustomPower 才能让 BaseLib 的 PackedIconPath patch 采用自定义图标；
// 基类 TemporaryStrengthPower 不是 CustomPowerModel，本身没有 CustomPackedIconPath。
public sealed class LegacyTempStrengthDown : TemporaryStrengthPower, ICustomPower
{
    public override AbstractModel OriginModel => ModelDb.Card<LegacyHinaModsMoonLeverage>();

    protected override bool IsPositive => false;

    // 卖萌附带的「临时力量下降」图标（原版没有，单独画的）
    public string? CustomPackedIconPath => "res://images/hinamods/Powers/legacy_temp_strength_down.png";
    public string? CustomBigIconPath => "res://images/hinamods/Powers/legacy_temp_strength_down.png";
    public string? CustomBigBetaIconPath => null;
}
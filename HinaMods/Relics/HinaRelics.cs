using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Godot;
using Kaguya.HinaMods.Character;
using Kaguya.HinaMods.Extensions;

namespace Kaguya.HinaMods.Relics;

[Pool(typeof(HinaModsRelicPool))]

/// <summary>
/// 辉夜侧遗物基类：按 {类名去前缀小写}.png 在遗物图标目录找图，
/// 缺失时回落到 relic.png / relic_outline.png；挂在 HinaModsRelicPool 下。
/// 基础设施类，无本地化键。
/// </summary>
public abstract class HinaRelics : CustomRelicModel
{
    public override string PackedIconPath
    {
        get
        {
            var path = $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".RelicImagePath();
            return ResourceLoader.Exists(path) ? path : "relic.png".RelicImagePath();
        }
    }

    protected override string PackedIconOutlinePath
    {
        get
        {
            var path = $"{Id.Entry.RemovePrefix().ToLowerInvariant()}_outline.png".RelicImagePath();
            return ResourceLoader.Exists(path) ? path : "relic_outline.png".RelicImagePath();
        }
    }

    protected override string BigIconPath
    {
        get
        {
            var path = $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigRelicImagePath();
            return ResourceLoader.Exists(path) ? path : "relic.png".BigRelicImagePath();
        }
    }
}
namespace Kaguya;

using System;
using System.IO;
using Godot;

/// <summary>
/// 辉夜侧内容版本。切换后需要重启游戏生效（<see cref="MegaCrit.Sts2.Core.Models.ModelDb"/> 在首次访问时缓存内容）。
/// </summary>
public enum HinaContentVersion
{
    /// <summary>当前版本（KAGUYA-* 系列 ID）。</summary>
    New = 0,

    /// <summary>经典版（master 分支的实现，LEGACY_* 系列 ID）。</summary>
    Legacy = 1,
}

public sealed class KaguyaModConfig : BaseLib.Config.SimpleModConfig
{
    public static bool NonKaguyaCharactersSeeKaguyaContent { get; set; } = false;
    public static bool PlayProcessAnimations { get; set; } = true;
    public static bool AllowAncientCoexistence { get; set; } = false;
    public static bool AllowMemoryFragmentForAllCharacters { get; set; } = false;
    public static bool AllowMainMenuSceneReplacement { get; set; } = true;
    public static bool AllowMainMenuBgmReplacement { get; set; } = true;

    /// <summary>
    /// 辉夜侧内容版本：新版 / 经典版。
    /// 只影响辉夜（HinaMods）的角色卡牌能力遗物药水，彩叶侧不受影响。
    /// 改动后需重启游戏。
    /// </summary>
    public static HinaContentVersion HinaVersion { get; set; } = HinaContentVersion.New;

    /// <summary>
    /// BaseLib 已经实例化过配置并完成 <c>Load()</c>。
    /// 在此之前（模型注册阶段可能更早）静态属性还是默认值，
    /// 需要直接从 cfg 读，否则会把版本判断成 New。
    /// </summary>
    private static bool _loaded;

    /// <summary>从磁盘读到的版本，null 表示还没读。</summary>
    private static HinaContentVersion? _diskVersion;

    public KaguyaModConfig()
    {
        // 基类构造里执行了 Init()（含 Load()），到这里静态属性已是磁盘上的值。
        _loaded = true;
    }

    /// <summary>当前是否使用经典版辉夜内容。</summary>
    public static bool UseLegacyHina
        => ResolveVersion() == HinaContentVersion.Legacy;

    /// <summary>
    /// 取当前生效的版本。BaseLib 已加载就用内存值（权威）；
    /// 否则说明还在初始化早期，直接读 cfg 兜底。
    /// </summary>
    public static HinaContentVersion ResolveVersion()
    {
        if (_loaded)
            return HinaVersion;
        return ReadVersionFromDisk();
    }

    /// <summary>
    /// 直接读 <c>user://mod_configs/Kaguya.cfg</c>。
    /// 该文件是 BaseLib 用 JSON 序列化的 <c>Dictionary&lt;string,string&gt;</c>，
    /// 值经 TypeConverter 转成字符串，所以枚举是 "New" / "Legacy"。
    /// </summary>
    private static HinaContentVersion ReadVersionFromDisk()
    {
        if (_diskVersion.HasValue)
            return _diskVersion.Value;

        var result = HinaContentVersion.New;
        try
        {
            string path = Path.Combine(OS.GetUserDataDir(), "mod_configs", "Kaguya.cfg");
            if (File.Exists(path))
            {
                string text = File.ReadAllText(path);
                int key = text.IndexOf("\"HinaVersion\"", StringComparison.Ordinal);
                if (key >= 0)
                {
                    int colon = text.IndexOf(':', key);
                    int q1 = colon >= 0 ? text.IndexOf('"', colon + 1) : -1;
                    int q2 = q1 >= 0 ? text.IndexOf('"', q1 + 1) : -1;
                    if (q2 > q1)
                    {
                        string value = text.Substring(q1 + 1, q2 - q1 - 1);
                        if (value.Equals("Legacy", StringComparison.OrdinalIgnoreCase))
                            result = HinaContentVersion.Legacy;
                    }
                }
            }
        }
        catch
        {
            // 读不到就按默认（New）处理，不阻塞启动
        }

        _diskVersion = result;
        return result;
    }
}

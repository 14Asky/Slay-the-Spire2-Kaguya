using BaseLib.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Kaguya.HinaMods.Visuals;

/// <summary>
/// 辉夜战斗立绘（Spine 版）的装配工具。
///
/// 关键点：
///   1. 骨架数据走**资源**（<c>scenes/hinamods/spine/kaguya_spine_data.tres</c>），
///      不再用 <c>load_from_file</c> 读原始 .skel/.atlas。
///      好处：`.tres` 引用的导入产物（.spskel/.spatlas）会被导出器当作普通资源打进 pck，
///      **不依赖 export_presets 的 include_filter** —— 上一版就是因为 pck 里没带 .skel/.atlas
///      导致运行时 `skel=30 / atlas=15`、立绘不可见。前提是这批资源已在编辑器里导入过。
///   2. <c>SpineSprite</c> 是 GDExtension 类型。场景里 <c>Visuals</c> 先放普通 <c>Node2D</c> 占位，
///      真正的 SpineSprite 在节点**入树之前**创建并替换 —— 这样 .tscn 里不写 SpineSprite，
///      没有装编辑器扩展的人打开场景也不会弹「节点类型不可用」。
///   3. 替换时机是硬约束：引擎 <c>NCreatureVisuals._Ready</c> 会立刻 <c>GetNode("%Visuals")</c>
///      并检查 <c>GetSkeleton()?.GetData() != null</c>，不满足就永久关闭 spine 动画；
///      而入树过程中父节点 busy（"Parent node is busy setting up children"），add_child 会失败。
///   4. 装配失败（数据缺失 / 扩展没加载）时回退到旧的序列帧立绘 <c>HinaVisual.tscn</c>，
///      不会再出现「角色隐形」。
/// </summary>
public static class KaguyaSpineVisual
{
    /// <summary>立绘场景（根节点会被 BaseLib 自动转换成 NCreatureVisuals）。</summary>
    public const string ScenePath = "res://scenes/hinamods/KaguyaSpine.tscn";

    /// <summary>旧序列帧立绘（兜底用）。</summary>
    public const string LegacyScenePath = "res://scenes/hinamods/HinaVisual.tscn";

    /// <summary>骨架数据资源（编辑器里由 .skel + .atlas 生成）。</summary>
    public const string SkeletonDataPath = "res://scenes/hinamods/spine/kaguya_spine_data.tres";

    /// <summary>原始骨架文件（加载 .tres 失败时的兜底路径，需靠 include_filter 打进 pck）。</summary>
    public const string SkeletonPath = "res://scenes/hinamods/spine/kaguya.skel";

    /// <summary>原始图集文件。</summary>
    public const string AtlasPath = "res://scenes/hinamods/spine/kaguya.atlas";

    private const string SpineClass = "SpineSprite";
    private const string VisualsName = "Visuals";
    private const string PlaceholderName = "VisualsPlaceholder";

    /// <summary>场景里没找到占位节点时的兜底变换（对应 tscn 里的值）。</summary>
    private static readonly Vector2 FallbackPosition = new(-14f, -10f);
    private static readonly Vector2 FallbackScale = new(0.409f, 0.409f);

    private static Resource? _cachedData;
    private static bool _legacyRegistered;

    /// <summary>
    /// 创建辉夜立绘：Spine 版优先，失败则回退旧序列帧版。
    /// 由 <c>HinaCharacter.CreateCustomVisuals()</c> 调用（节点入树前）。
    /// </summary>
    public static NCreatureVisuals? CreateVisuals()
    {
        // 经典版：直接走旧版序列帧立绘（HinaVisual.tscn）。
        // 只有那套资源带 MoonsIdle / MoonsAttack / MoonsHit / MoonsDead ——
        // 月之赐福（8000 层）的立绘切换依赖它的 AnimatedSprite2D「Visuals」节点，
        // Spine 骨架里没有对应动画，所以经典版必须用旧视觉。
        if (Kaguya.KaguyaModConfig.UseLegacyHina)
            return CreateLegacyVisuals();

        NCreatureVisuals? visuals = Instantiate(ScenePath);
        if (visuals == null)
            return CreateLegacyVisuals();

        if (Install(visuals))
            return visuals;

        GD.PushWarning("[Kaguya] Spine 立绘装配失败，回退到旧的序列帧立绘");
        visuals.Free();
        return CreateLegacyVisuals();
    }

    /// <summary>兜底：旧的序列帧立绘（HinaVisual.tscn）。</summary>
    public static NCreatureVisuals? CreateLegacyVisuals()
    {
        if (!_legacyRegistered)
        {
            // 旧场景不在 CustomVisualPath 里，没被注册过自动转换，这里补一次
            LegacyScenePath.RegisterSceneForConversion<NCreatureVisuals>();
            _legacyRegistered = true;
        }

        return Instantiate(LegacyScenePath);
    }

    /// <summary>
    /// 把 <paramref name="root"/> 下名为 Visuals 的占位节点替换成装好骨架的 SpineSprite。
    /// 必须在 root 入树前调用；重复调用安全。
    /// </summary>
    public static bool Install(Node root)
    {
        Node2D? placeholder = root.GetNodeOrNull<Node2D>(VisualsName);
        if (placeholder != null && placeholder.GetClass() == SpineClass)
            return true; // 已经是 SpineSprite

        Resource? data = LoadSkeletonData();
        if (data == null)
            return false;

        Node2D? spine = CreateSpineSprite();
        if (spine == null)
            return false;

        int index = placeholder?.GetIndex() ?? 0;
        Vector2 position = placeholder?.Position ?? FallbackPosition;
        Vector2 scale = placeholder?.Scale ?? FallbackScale;
        Color modulate = placeholder?.Modulate ?? Colors.White;
        int zIndex = placeholder?.ZIndex ?? 0;
        bool visible = placeholder?.Visible ?? true;

        // 先让占位节点交出 "Visuals" 名字与唯一名，否则新节点会被自动改名成 Visuals2
        if (placeholder != null)
        {
            placeholder.UniqueNameInOwner = false;
            placeholder.Name = PlaceholderName;
        }

        spine.Name = VisualsName;
        spine.Position = position;
        spine.Scale = scale;
        spine.Modulate = modulate;
        spine.ZIndex = zIndex;
        spine.Visible = visible;

        // 骨架必须在入树前装好：NCreatureVisuals._Ready 立刻校验 GetSkeleton().GetData()
        spine.Set("skeleton_data_res", data);

        root.AddChild(spine);
        spine.Owner = root; // 让 %Visuals 登记进父节点的唯一名表
        spine.UniqueNameInOwner = true;
        root.MoveChild(spine, index);

        if (placeholder != null)
        {
            root.RemoveChild(placeholder);
            placeholder.Free();
        }

        return true;
    }

    private static NCreatureVisuals? Instantiate(string path)
    {
        PackedScene? scene = ResourceLoader.Load<PackedScene>(path);
        if (scene == null)
        {
            GD.PushError($"[Kaguya] 找不到立绘场景 {path}");
            return null;
        }

        Node? root = scene.Instantiate();
        if (root is not NCreatureVisuals visuals)
        {
            GD.PushError($"[Kaguya] {path} 没有被转换成 NCreatureVisuals（实际 {root?.GetType().Name ?? "null"}）");
            root?.Free();
            return null;
        }

        return visuals;
    }

    private static Resource? LoadSkeletonData()
    {
        if (_cachedData != null)
            return _cachedData;

        // 首选：编辑器导入好的 .tres（依赖 .spskel/.spatlas 导入产物）
        _cachedData = ResourceLoader.Load<Resource>(SkeletonDataPath);
        if (_cachedData != null)
            return _cachedData;

        // 兜底：直接用 spine-godot 运行时读原始 .skel/.atlas。
        // 用于「导出时没加载到编辑器扩展、.spskel 没被打进 pck」的情况——
        // 此时只要 include_filter 带了 *.skel,*.atlas，这条路就能跑通。
        GD.PushWarning($"[Kaguya] {SkeletonDataPath} 加载失败，改用运行时读取原始骨架文件");
        _cachedData = BuildSkeletonDataAtRuntime();
        return _cachedData;
    }

    /// <summary>运行时用 load_from_file / load_from_atlas_file 拼骨架数据。</summary>
    private static Resource? BuildSkeletonDataAtRuntime()
    {
        GodotObject? skel = ClassDB.Instantiate("SpineSkeletonFileResource").AsGodotObject();
        GodotObject? atlas = ClassDB.Instantiate("SpineAtlasResource").AsGodotObject();
        GodotObject? data = ClassDB.Instantiate("SpineSkeletonDataResource").AsGodotObject();
        if (skel == null || atlas == null || data == null)
        {
            GD.PushError("[Kaguya] spine-godot 资源类不可用（SpineSkeleton/Atlas/DataResource）");
            return null;
        }

        int skelError = skel.Call("load_from_file", SkeletonPath).AsInt32();
        int atlasError = atlas.Call("load_from_atlas_file", AtlasPath).AsInt32();
        if (skelError != 0 || atlasError != 0)
        {
            GD.PushError($"[Kaguya] 读取原始骨架失败 skel={skelError} atlas={atlasError}" +
                         "（pck 里是否漏了 kaguya.skel / kaguya.atlas？检查 include_filter）");
            return null;
        }

        data.Set("skeleton_file_res", skel);
        data.Set("atlas_res", atlas);
        return data as Resource;
    }

    private static Node2D? CreateSpineSprite()
    {
        GodotObject? obj = ClassDB.Instantiate(SpineClass).AsGodotObject();
        if (obj is Node2D node)
            return node;

        GD.PushError("[Kaguya] spine-godot 运行时不可用，无法创建 SpineSprite");
        obj?.Free();
        return null;
    }
}

using BaseLib.Extensions;
using Godot;
using System;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Kaguya.HinaMods.Visuals;

/// <summary>
/// 辉夜战斗立绘（Spine 版）的装配工具。
///
/// 导入资源必须通过内部数据校验；空壳资源改用原始 skel/atlas 重建。
/// SpineSprite 的骨架、数据与动画状态均验证成功后，才替换场景占位节点。
/// 以上操作在入树前完成，避免 NCreatureVisuals._Ready 使用无效的 Spine 绑定。
/// 任一步失败都回退到 HinaVisual.tscn，保留旧的序列帧动作。
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
    private static bool _diagnosticsLogged;
    private const string FallbackMeta = "kaguya_spine_fallback";
    private static readonly string[] RequiredAnimations = { "idle_loop", "attack", "cast", "hurt", "die" };

    public static bool IsLegacyFallback(Node? visuals) =>
        GodotObject.IsInstanceValid(visuals) && visuals.HasMeta(FallbackMeta);

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
            return CreateFallback();

        try
        {
            if (Install(visuals))
                return visuals;
        }
        catch (Exception ex)
        {
            _cachedData = null;
            GD.PushWarning($"[Kaguya] Spine 装配异常: {ex}");
        }

        GD.PushWarning("[Kaguya] Spine 立绘装配失败，回退到旧的序列帧立绘");
        visuals.Free();
        return CreateFallback();
    }

    private static NCreatureVisuals? CreateFallback()
    {
        var fallback = CreateLegacyVisuals();
        fallback?.SetMeta(FallbackMeta, true);
        return fallback;
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
            return IsSpriteReady(placeholder, out _);

        Resource? data = LoadSkeletonData();
        if (data == null)
            return false;

        Node2D? spine = CreateSpineSprite();
        if (spine == null)
            return false;

        // 在交出占位节点前验证真正的原生对象；非空的 Resource/SpineSprite 可能只是空壳。
        spine.Set("skeleton_data_res", data);
        if (!IsSpriteReady(spine, out string reason))
        {
            GD.PushWarning($"[Kaguya] SpineSprite 无效: {reason}");
            _cachedData = null;
            spine.Free();
            return false;
        }

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
        try
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
        catch (Exception ex)
        {
            GD.PushWarning($"[Kaguya] 立绘场景加载异常 {path}: {ex.Message}");
            return null;
        }
    }

    private static Resource? LoadSkeletonData()
    {
        LogDiagnostics();
        if (IsDataReady(_cachedData, out _))
            return _cachedData;
        _cachedData = null;

        Resource? imported = null;
        try { imported = ResourceLoader.Load<Resource>(SkeletonDataPath); }
        catch (Exception ex) { GD.PushWarning($"[Kaguya] 导入骨架加载异常: {ex.Message}"); }
        if (IsDataReady(imported, out string reason))
            return _cachedData = imported;

        GD.PushWarning($"[Kaguya] 导入骨架无效 ({SkeletonDataPath}): {reason}；尝试原始文件");
        var rebuilt = BuildSkeletonDataAtRuntime();
        if (IsDataReady(rebuilt, out reason))
        {
            _cachedData = rebuilt;
            GD.Print($"[Kaguya] Spine 原始文件重建成功，骨架版本={rebuilt.Call("get_version")}");
        }
        else
            GD.PushWarning($"[Kaguya] 原始骨架重建无效: {reason}，将使用旧立绘");
        return _cachedData;
    }

    private static bool IsDataReady(GodotObject? data, out string reason)
    {
        try { return ValidateData(data, out reason); }
        catch (Exception ex) { reason = ex.Message; return false; }
    }

    private static bool ValidateData(GodotObject? data, out string reason)
    {
        reason = "骨架数据未加载";
        if (!GodotObject.IsInstanceValid(data) || !data.HasMethod("is_skeleton_data_loaded") ||
            !data.Call("is_skeleton_data_loaded").AsBool())
            return false;

        var atlas = data.Call("get_atlas_res").AsGodotObject();
        if (!GodotObject.IsInstanceValid(atlas) || !atlas.HasMethod("get_textures"))
        {
            reason = "图集未加载";
            return false;
        }
        var textures = atlas.Call("get_textures").AsGodotArray();
        if (textures.Count == 0) { reason = "图集没有贴图"; return false; }
        foreach (var entry in textures)
        {
            if (entry.AsGodotObject() is not Texture2D texture || !GodotObject.IsInstanceValid(texture) ||
                texture.GetWidth() <= 0 || texture.GetHeight() <= 0)
            { reason = "图集贴图无效"; return false; }
        }
        foreach (string animation in RequiredAnimations)
        {
            if (data.Call("find_animation", animation).AsGodotObject() == null)
            { reason = $"缺少动作 {animation}"; return false; }
        }
        reason = "ok";
        return true;
    }

    /// <summary>直接检查原生对象，避开 MegaSpine 将 Object(null) 包成非空绑定的问题。</summary>
    public static bool IsSpriteReady(GodotObject? sprite, out string reason)
    {
        reason = "SpineSprite 不可用";
        try
        {
            if (!GodotObject.IsInstanceValid(sprite) || sprite.GetClass() != SpineClass)
                return false;
            var skeleton = sprite.Call("get_skeleton").AsGodotObject();
            if (!GodotObject.IsInstanceValid(skeleton)) { reason = "实际骨架为空"; return false; }
            if (!IsDataReady(skeleton.Call("get_data").AsGodotObject(), out reason))
                return false;
            if (!GodotObject.IsInstanceValid(sprite.Call("get_animation_state").AsGodotObject()))
            { reason = "动画状态为空"; return false; }
            return true;
        }
        catch (Exception ex) { reason = ex.Message; return false; }
    }

    private static void LogDiagnostics()
    {
        if (_diagnosticsLogged) return;
        _diagnosticsLogged = true;
        GD.Print($"[Kaguya] Spine 环境: OS={OS.GetName()} Godot={Engine.GetVersionInfo()["string"]} " +
                 $"DLL={typeof(KaguyaSpineVisual).Assembly.Location} version={typeof(KaguyaSpineVisual).Assembly.GetName().Version} " +
                 $"SpineSprite={ClassDB.ClassExists(SpineClass)}");
        foreach (string path in new[] { SkeletonDataPath, SkeletonPath, AtlasPath, "res://scenes/hinamods/spine/kaguya.png" })
        {
            using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
            GD.Print($"[Kaguya] Spine 文件: {path} resource={ResourceLoader.Exists(path)} bytes={file?.GetLength().ToString() ?? "missing"}");
        }
    }

    /// <summary>运行时用 load_from_file / load_from_atlas_file 拼骨架数据。</summary>
    private static Resource? BuildSkeletonDataAtRuntime()
    {
        if (!ClassDB.ClassExists("SpineSkeletonFileResource") || !ClassDB.ClassExists("SpineAtlasResource") ||
            !ClassDB.ClassExists("SpineSkeletonDataResource"))
            return null;
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
        if (!ClassDB.ClassExists(SpineClass)) return null;
        GodotObject? obj = ClassDB.Instantiate(SpineClass).AsGodotObject();
        if (obj is Node2D node)
            return node;

        GD.PushError("[Kaguya] spine-godot 运行时不可用，无法创建 SpineSprite");
        obj?.Free();
        return null;
    }
}

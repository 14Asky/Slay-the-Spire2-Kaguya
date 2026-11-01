using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Saves;
using System;

namespace Kaguya;

/// <summary>
/// 替换主界面 BGM，从 3 首曲目中随机轮换播放，永不连续重复同一首
/// </summary>
[HarmonyPatch(typeof(NMainMenu))]
public static class MainMenuBGMPatch
{
    private static readonly string[] BgmTracks =
    {
        "res://debug_audio/Remember.ogg",
        "res://debug_audio/LoveMyself.ogg",
        "res://debug_audio/HappySyn.ogg",
    };

    private static AudioStreamPlayer _bgmPlayer;
    private static int _lastTrackIndex = -1;

    /// <summary>主菜单右下角的切歌按钮（只显示音符 ♪）。</summary>
    private static Button? _nextButton;

    [HarmonyPatch(nameof(NMainMenu._Ready))]
    [HarmonyPostfix]
    static void ReadyPostfix(NMainMenu __instance)
    {
        if (!KaguyaModConfig.AllowMainMenuBgmReplacement)
            return;

        // 清理上一轮的播放器
        CleanupPlayer();

        // 停止 FMOD 原版 BGM
        NAudioManager.Instance?.StopMusic();

        // 随机选一首播放
        _lastTrackIndex = PickNextTrackIndex();
        _bgmPlayer = CreatePlayer(_lastTrackIndex);
        __instance.AddChild(_bgmPlayer);

        // 同步当前 BGM 音量
        ApplyVolume(SaveManager.Instance.SettingsSave.VolumeBgm);

        _bgmPlayer.Play();

        AddNextTrackButton(__instance);
    }

    /// <summary>
    /// 在右下角放一个只显示 ♪ 的圆形按钮，点击手动切到下一首。
    /// 不放进主菜单按钮列表，是为了不改动原版布局容器。
    /// </summary>
    private static void AddNextTrackButton(NMainMenu menu)
    {
        if (GodotObject.IsInstanceValid(_nextButton))
            return;

        Button btn = new()
        {
            Text = "♪",
            FocusMode = Control.FocusModeEnum.None,
            TooltipText = "切换 BGM",
        };

        // 右下角锚定，留 32px 边距，直径 56
        btn.SetAnchorsPreset(Control.LayoutPreset.BottomRight);
        btn.OffsetLeft = -88f;
        btn.OffsetTop = -88f;
        btn.OffsetRight = -32f;
        btn.OffsetBottom = -32f;

        StyleBoxFlat normal = MakeRoundStyle(0.45f);
        btn.AddThemeStyleboxOverride("normal", normal);
        btn.AddThemeStyleboxOverride("hover", MakeRoundStyle(0.65f));
        btn.AddThemeStyleboxOverride("pressed", MakeRoundStyle(0.8f));
        btn.AddThemeStyleboxOverride("focus", normal);

        btn.AddThemeColorOverride("font_color", Colors.White);
        btn.AddThemeFontSizeOverride("font_size", 26);

        btn.Pressed += NextTrack;
        menu.AddChild(btn);
        _nextButton = btn;
    }

    private static StyleBoxFlat MakeRoundStyle(float alpha)
    {
        return new StyleBoxFlat
        {
            BgColor = new Color(0f, 0f, 0f, alpha),
            CornerRadiusTopLeft = 28,
            CornerRadiusTopRight = 28,
            CornerRadiusBottomLeft = 28,
            CornerRadiusBottomRight = 28,
            ContentMarginLeft = 0f,
            ContentMarginRight = 0f,
            ContentMarginTop = 0f,
            ContentMarginBottom = 0f,
        };
    }

    /// <summary>手动切下一首：顺序循环，不走随机（按钮的直觉是「下一首」）。</summary>
    private static void NextTrack()
    {
        if (!GodotObject.IsInstanceValid(_bgmPlayer) || BgmTracks.Length == 0)
            return;

        int next = (_lastTrackIndex + 1) % BgmTracks.Length;
        AudioStream? stream = GD.Load<AudioStream>(BgmTracks[next]);
        if (stream == null)
            return;

        _lastTrackIndex = next;
        _bgmPlayer.Stream = stream;
        _bgmPlayer.Play();
    }

    private static void OnTrackFinished()
    {
        if (!GodotObject.IsInstanceValid(_bgmPlayer))
            return;

        _lastTrackIndex = PickNextTrackIndex();
        _bgmPlayer.Stream = GD.Load<AudioStream>(BgmTracks[_lastTrackIndex]);
        _bgmPlayer.Play();
    }

    private static int PickNextTrackIndex()
    {
        if (BgmTracks.Length <= 1)
            return 0;

        int index;
        do
        {
            index = Random.Shared.Next(BgmTracks.Length);
        } while (index == _lastTrackIndex);

        return index;
    }

    private static AudioStreamPlayer CreatePlayer(int trackIndex)
    {
        var player = new AudioStreamPlayer();
        player.Bus = "Music";
        player.Stream = GD.Load<AudioStream>(BgmTracks[trackIndex]);
        player.Finished += OnTrackFinished;
        return player;
    }

    private static void CleanupPlayer()
    {
        if (!GodotObject.IsInstanceValid(_bgmPlayer))
        {
            _bgmPlayer = null;
            return;
        }

        _bgmPlayer.Finished -= OnTrackFinished;
        _bgmPlayer.Stop();
        _bgmPlayer.QueueFree();
        _bgmPlayer = null;
    }

    internal static void ApplyVolume(float volume)
    {
        if (!GodotObject.IsInstanceValid(_bgmPlayer))
            return;

        _bgmPlayer.VolumeDb = Mathf.LinearToDb(Mathf.Max(volume, 0.0001f));
    }
}

/// <summary>
/// 拦截游戏 BGM 音量设置，同步到自定义主菜单 BGM 播放器
/// </summary>
[HarmonyPatch(typeof(NAudioManager), nameof(NAudioManager.SetBgmVol))]
public static class MainMenuBGMVolumePatch
{
    [HarmonyPostfix]
    static void Postfix(float volume)
    {
        MainMenuBGMPatch.ApplyVolume(volume);
    }
}

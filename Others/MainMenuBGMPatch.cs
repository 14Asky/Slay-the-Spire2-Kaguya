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

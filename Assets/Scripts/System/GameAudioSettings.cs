using System;
using UnityEngine;

/// <summary>소리 설정 - 배경음·효과음 볼륨(0~1), 음소거 (PlayerPrefs). 음소거 = AudioListener.volume 0</summary>
public static class GameAudioSettings
{
    public const float DefaultVolume = 0.8f;

    private const string BgmVolumeKey = "DUT_BgmVolume";
    private const string SfxVolumeKey = "DUT_SfxVolume";
    private const string MuteKey = "DUT_Mute"; // 0·1

    /// <summary>값 변경 알림 - 배경음·효과음 재생기가 구독</summary>
    public static event Action Changed;

    public static float BgmVolume => PlayerPrefs.GetFloat(BgmVolumeKey, DefaultVolume);
    public static float SfxVolume => PlayerPrefs.GetFloat(SfxVolumeKey, DefaultVolume);
    public static bool IsMuted => PlayerPrefs.GetInt(MuteKey, 0) == 1;

    /// <summary>게임 시작 시 저장된 음소거 적용</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplyOnStart() => ApplyMute();

    public static void SetBgmVolume(float volume)
    {
        PlayerPrefs.SetFloat(BgmVolumeKey, Mathf.Clamp01(volume));
        Changed?.Invoke();
    }

    public static void SetSfxVolume(float volume)
    {
        PlayerPrefs.SetFloat(SfxVolumeKey, Mathf.Clamp01(volume));
        Changed?.Invoke();
    }

    public static void SetMuted(bool isMuted)
    {
        PlayerPrefs.SetInt(MuteKey, isMuted ? 1 : 0);
        ApplyMute();
        Changed?.Invoke();
    }

    /// <summary>디스크 기록 - 설정 창 닫을 때</summary>
    public static void Save() => PlayerPrefs.Save();

    private static void ApplyMute() => AudioListener.volume = IsMuted ? 0f : 1f;
}

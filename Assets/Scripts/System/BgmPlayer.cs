using UnityEngine;

/// <summary>씬 배경음 반복 재생 - 최종 볼륨 = 곡 기본 볼륨 × 설정 배경음 볼륨</summary>
[RequireComponent(typeof(AudioSource))]
public class BgmPlayer : MonoBehaviour
{
    [SerializeField] private AudioClip _clip;
    [SerializeField, Range(0f, 1f)] private float _baseVolume = 1f; // 곡별 기본 크기

    private AudioSource _source;

    private void Awake()
    {
        _source = GetComponent<AudioSource>();
        _source.clip = _clip;
        _source.loop = true;
        _source.playOnAwake = false;
        ApplyVolume();
    }

    private void OnEnable()
    {
        GameAudioSettings.Changed += ApplyVolume;
        if (_clip != null && !_source.isPlaying) _source.Play();
    }

    private void OnDisable() => GameAudioSettings.Changed -= ApplyVolume;

    private void ApplyVolume() => _source.volume = _baseVolume * GameAudioSettings.BgmVolume;
}

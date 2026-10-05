using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>방어선 피격 연출 - 바리케이드 플래시 · 붉은 비네트 · 카메라 쉐이크 · HP 위험 배너</summary>
public class PlayerHitFeedback : MonoBehaviour
{
    [SerializeField] private Health _health;
    [SerializeField] private SpriteFlash _barrierFlash;

    [Header("화면 연출 (비네트 · 쉐이크)")]
    [SerializeField] private Image _vignette;
    [SerializeField] private Transform _camera;

    [Tooltip("화면 연출 전역 쿨다운(초) - 이 시간당 최대 1회")]
    [SerializeField] private float _screenFxCooldown = 1.5f;

    [Tooltip("비네트 전체 시간(초)")]
    [SerializeField] private float _vignetteDuration = 0.35f;

    [Range(0f, 1f)] [SerializeField] private float _vignetteMaxAlpha = 1f;

    [SerializeField] private float _shakeDuration = 0.15f;

    [Tooltip("쉐이크 진폭(unit)")]
    [SerializeField] private float _shakeAmplitude = 0.06f;

    [Header("HP 위험 배너")]
    [SerializeField] private BannerText _criticalBanner;

    [Tooltip("HP 비율이 이 값 이하로 처음 내려가면 1회 표시")]
    [Range(0f, 1f)] [SerializeField] private float _criticalRatio = 0.25f;

    [SerializeField] private string _criticalMessage = "방어선이 위험합니다";

    private float _nextScreenFxTime; // Time.time 기준
    private bool _hasShownCritical;
    private bool _isBossWarningActive;
    private Vector3 _cameraOrigin;

    /// <summary>카메라 원위치 저장 · 비네트 숨김</summary>
    private void Awake()
    {
        _cameraOrigin = _camera.localPosition;
        SetVignetteAlpha(0f);
    }

    private void OnDisable() => StopScreenFx();

    /// <summary>피격 1회 - 플레이어 Health.OnDamaged 에 연결</summary>
    public void PlayHit()
    {
        // 사망 타격 - 게임이 멈추므로(timeScale 0) 화면 연출을 끝내고 원위치
        if (_health.CurrentHp <= 0)
        {
            StopScreenFx();
            return;
        }

        _barrierFlash.Flash();
        PlayScreenFx();
        CheckCritical();
    }

    /// <summary>비네트 + 쉐이크 - 쿨다운 중이면 무시</summary>
    public void PlayScreenFx()
    {
        if (_isBossWarningActive || Time.time < _nextScreenFxTime) return;
        _nextScreenFxTime = Time.time + _screenFxCooldown;

        StartCoroutine(VignetteRoutine(_vignetteDuration));
        StartCoroutine(ShakeRoutine());
    }

    /// <summary>보스 등장 경고 - 쿨다운 없이 비네트만 재생</summary>
    public void PlayBossWarning(float duration)
    {
        StopScreenFx();
        _isBossWarningActive = true;
        StartCoroutine(BossWarningRoutine(duration));
    }

    /// <summary>보스 등장 비네트 즉시 종료</summary>
    public void StopBossWarning()
    {
        if (_isBossWarningActive) StopScreenFx();
    }

    /// <summary>HP 위험 첫 진입 시 배너 1회</summary>
    private void CheckCritical()
    {
        if (_hasShownCritical || _health.CurrentHp <= 0) return;
        if (_health.CurrentHp > _health.MaxHp * _criticalRatio) return;

        _hasShownCritical = true;
        _criticalBanner.Show(_criticalMessage);
    }

    /// <summary>진행 중인 비네트·쉐이크 즉시 종료 + 카메라 원위치</summary>
    public void StopScreenFx()
    {
        StopAllCoroutines();
        _isBossWarningActive = false;
        _camera.localPosition = _cameraOrigin;
        SetVignetteAlpha(0f);
    }

    private IEnumerator BossWarningRoutine(float duration)
    {
        yield return VignetteRoutine(duration);
        _isBossWarningActive = false;
    }

    private IEnumerator VignetteRoutine(float duration)
    {
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float progress = t / duration;
            float alpha = progress < 0.3f ? progress / 0.3f : (1f - progress) / 0.7f; // 빠르게 켜지고 천천히 꺼짐
            SetVignetteAlpha(alpha * _vignetteMaxAlpha);
            yield return null;
        }
        SetVignetteAlpha(0f);
    }

    private IEnumerator ShakeRoutine()
    {
        for (float t = 0f; t < _shakeDuration; t += Time.deltaTime)
        {
            // 정지 중(일시정지·게임 오버)엔 흔들지 않음 - deltaTime 0 이면 t 가 안 늘어 계속 흔들리던 문제
            bool isPaused = Time.deltaTime <= 0f;
            _camera.localPosition = isPaused ? _cameraOrigin : _cameraOrigin + (Vector3)(Random.insideUnitCircle * _shakeAmplitude);
            yield return null;
        }
        _camera.localPosition = _cameraOrigin;
    }

    private void SetVignetteAlpha(float alpha)
    {
        Color color = _vignette.color;
        color.a = alpha;
        _vignette.color = color;
    }
}

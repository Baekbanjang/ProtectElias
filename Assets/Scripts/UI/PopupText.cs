using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>큰 문구 팝업 - 확대 등장 → 유지 → (페이드)</summary>
[RequireComponent(typeof(CanvasGroup))]
public class PopupText : MonoBehaviour
{
    [SerializeField] private TMP_Text _label;

    [Tooltip("확대 등장 시간(초)")]
    [SerializeField] private float _popInDuration = 0.15f;

    [Tooltip("등장 시작 배율")]
    [SerializeField] private float _popStartScale = 1.8f;

    [Tooltip("페이드 아웃 시간(초)")]
    [SerializeField] private float _fadeOutDuration = 0.2f;

    [Tooltip("정지(timeScale 0) 중에도 재생 - 게임오버용")]
    [SerializeField] private bool _useUnscaledTime;

    private CanvasGroup _group;

    /// <summary>숨김 상태로 시작</summary>
    private void Awake()
    {
        _group = GetComponent<CanvasGroup>();
        _group.alpha = 0f;
    }

    /// <summary>문구 1회 재생 - duration = 전체 시간, isFadeOut false 면 표시 유지</summary>
    public IEnumerator Play(string message, float duration, bool isFadeOut = true)
    {
        _label.text = message;
        for (float t = 0f; t < duration; t += _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime)
        {
            float pop = Mathf.Clamp01(t / _popInDuration);
            float ease = 1f - (1f - pop) * (1f - pop); // ease-out
            transform.localScale = Vector3.one * Mathf.Lerp(_popStartScale, 1f, ease);

            float fade = isFadeOut ? Mathf.Clamp01((duration - t) / _fadeOutDuration) : 1f;
            _group.alpha = Mathf.Min(pop, fade);
            yield return null;
        }
        transform.localScale = Vector3.one;
        _group.alpha = isFadeOut ? 0f : 1f;
    }

    /// <summary>즉시 숨김</summary>
    public void Hide() => _group.alpha = 0f;
}

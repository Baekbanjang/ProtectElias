using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>배너 문구 - 나타남 → 유지 → 페이드</summary>
[RequireComponent(typeof(CanvasGroup))]
public class BannerText : MonoBehaviour
{
    [SerializeField] private TMP_Text _label;
    [SerializeField] private float _fadeInDuration = 0.15f;
    [SerializeField] private float _holdDuration = 0.7f;
    [SerializeField] private float _fadeOutDuration = 0.35f;

    private CanvasGroup _group;
    private Coroutine _showRoutine;

    /// <summary>숨김 상태로 시작</summary>
    private void Awake()
    {
        _group = GetComponent<CanvasGroup>();
        _group.alpha = 0f;
    }

    /// <summary>문구 표시 - 표시 중이면 처음부터 다시</summary>
    public void Show(string message)
    {
        _label.text = message;
        if (_showRoutine != null) StopCoroutine(_showRoutine);
        _showRoutine = StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        yield return Fade(0f, 1f, _fadeInDuration);
        yield return new WaitForSeconds(_holdDuration);
        yield return Fade(1f, 0f, _fadeOutDuration);
        _showRoutine = null;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            _group.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        _group.alpha = to;
    }
}

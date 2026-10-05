using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>스킬 컷인 - 배경 띠·초상 프레임·반짝임, 등장→유지→퇴장→공백 (실제 시간, 정지 중 재생)</summary>
[RequireComponent(typeof(CanvasGroup))]
public class SkillCutin : MonoBehaviour
{
    public enum CutinPhase { Hidden, Enter, Hold, Exit, Gap }

    [SerializeField] private GameObject _content; // 재생 중에만 켬
    [SerializeField] private RectTransform _band;  // 배경 띠
    [SerializeField] private RectTransform _frontBand; // 배경 띠 앞쪽 레이어(수은 전경) - 초상 뒤, 뒷띠 앞

    [Header("초상")]
    [SerializeField] private Image _portrait;

    [Tooltip("초상 표시 배율 - 원본 캔버스 기준")]
    [SerializeField] private float _portraitScale = 1.2083f;

    [Tooltip("초상 1프레임 시간(초)")]
    [SerializeField] private float _portraitFrameDuration = 0.02f;

    [Tooltip("등장·퇴장 이동 거리(px) - 오른쪽에서 들어옴")]
    [SerializeField] private float _slideDistance = 120f;

    [Header("반짝임")]
    [SerializeField] private Image[] _glints;

    [Tooltip("glint 4장 + particle 4장")]
    [SerializeField] private Sprite[] _glintFrames;

    [Tooltip("반짝임 표시 배율")]
    [SerializeField] private float _glintScale = 2f;

    [Tooltip("반짝임 1프레임 시간(초)")]
    [SerializeField] private float _glintFrameDuration = 0.08f;

    [Header("박자(초)")]
    [SerializeField] private float _enterDuration = 0.24f;
    [SerializeField] private float _holdDuration = 1.38f;
    [SerializeField] private float _exitDuration = 0.26f;
    [SerializeField] private float _gapDuration = 0.12f;

    private CanvasGroup _group;
    private Vector2 _portraitBasePosition;

    public CutinPhase Phase { get; private set; }

    private void Awake()
    {
        _group = GetComponent<CanvasGroup>();
        _portraitBasePosition = _portrait.rectTransform.anchoredPosition;
        _content.SetActive(false);
    }

    /// <summary>1회 재생 - 끝날 때까지 대기</summary>
    public IEnumerator Play(Sprite[] portraitFrames)
    {
        float holdStart = _enterDuration;
        float exitStart = holdStart + _holdDuration;
        float gapStart = exitStart + _exitDuration;
        float total = gapStart + _gapDuration;
        int lastFrame = portraitFrames.Length - 1;

        _content.SetActive(true);
        for (float t = 0f; t < total; t += Time.unscaledDeltaTime) // 한 시간축 - 구간 경계 누적 오차 없음
        {
            if (t < holdStart)
            {
                Phase = CutinPhase.Enter;
                float k = 1f - t / _enterDuration;
                ApplyVisual(1f - k * k, 0, portraitFrames, 0f); // ease-out
            }
            else if (t < exitStart)
            {
                Phase = CutinPhase.Hold;
                float hold = t - holdStart;
                ApplyVisual(1f, Mathf.Min((int)(hold / _portraitFrameDuration), lastFrame), portraitFrames, hold);
            }
            else if (t < gapStart)
            {
                Phase = CutinPhase.Exit;
                float k = 1f - (t - exitStart) / _exitDuration;
                ApplyVisual(k * k, lastFrame, portraitFrames, t - holdStart); // ease-in 퇴장
            }
            else
            {
                Phase = CutinPhase.Gap;
                _content.SetActive(false);
            }
            yield return null;
        }

        _content.SetActive(false);
        Phase = CutinPhase.Hidden;
    }

    /// <summary>진행도(0~1) 반영 - 투명도·띠 높이·초상 위치·프레임·반짝임</summary>
    private void ApplyVisual(float amount, int portraitFrame, Sprite[] portraitFrames, float time)
    {
        _group.alpha = amount;
        Vector3 bandScale = new Vector3(1f, Mathf.Lerp(0.2f, 1f, amount), 1f);
        _band.localScale = bandScale;
        _frontBand.localScale = bandScale;
        _portrait.rectTransform.anchoredPosition = _portraitBasePosition + Vector2.right * (_slideDistance * (1f - amount));
        ApplyFrame(_portrait, portraitFrames[portraitFrame], _portraitScale);

        int framesPerTag = _glintFrames.Length / 2;
        for (int i = 0; i < _glints.Length; i++)
        {
            int tagStart = i % 2 == 0 ? 0 : framesPerTag; // 짝수 glint · 홀수 particle
            int frame = ((int)(time / _glintFrameDuration) + i) % framesPerTag;
            ApplyFrame(_glints[i], _glintFrames[tagStart + frame], _glintScale);
        }
    }

    /// <summary>트림된 스프라이트를 원본 캔버스 중심 기준으로 배치 - 피벗·크기 맞춤</summary>
    private static void ApplyFrame(Image image, Sprite sprite, float scale)
    {
        image.sprite = sprite;
        RectTransform rect = image.rectTransform;
        rect.pivot = sprite.pivot / sprite.rect.size;
        rect.sizeDelta = sprite.rect.size * scale;
    }
}

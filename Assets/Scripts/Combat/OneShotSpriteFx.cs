using UnityEngine;
using UnityEngine.Pool;

/// <summary>1회 재생 스프라이트 효과 - 마지막 프레임 후 풀 반환 (잔상·명중 섬광)</summary>
[RequireComponent(typeof(SpriteRenderer))]
public class OneShotSpriteFx : MonoBehaviour
{
    [SerializeField] private Sprite[] _frames;

    [Tooltip("프레임별 표시 시간(초) - 게임 시간")]
    [SerializeField] private float[] _durations;

    [Tooltip("재생 동안 투명도 변화(시작 → 끝) - 잔상이 창과 구분되도록. 1 → 1 이면 변화 없음")]
    [SerializeField] private float _startAlpha = 1f;
    [SerializeField] private float _endAlpha = 1f;

    private SpriteRenderer _spriteRenderer;
    private IObjectPool<OneShotSpriteFx> _pool;
    private int _frameIndex;
    private float _frameElapsed;
    private float _totalElapsed;
    private float _totalDuration;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        foreach (float duration in _durations) _totalDuration += duration;
    }

    /// <summary>풀 등록</summary>
    public void SetPool(IObjectPool<OneShotSpriteFx> pool) => _pool = pool;

    /// <summary>위치 지정 후 첫 프레임부터 재생</summary>
    public void Play(Vector2 position)
    {
        transform.position = position;
        _frameIndex = 0;
        _frameElapsed = 0f;
        _totalElapsed = 0f;
        _spriteRenderer.sprite = _frames[0];
        SetAlpha(_startAlpha);
    }

    /// <summary>프레임 진행 - 끝나면 반환</summary>
    private void Update()
    {
        _frameElapsed += Time.deltaTime;
        _totalElapsed += Time.deltaTime;
        while (_frameElapsed >= _durations[_frameIndex])
        {
            _frameElapsed -= _durations[_frameIndex];
            _frameIndex++;
            if (_frameIndex >= _frames.Length)
            {
                _pool.Release(this);
                return;
            }
        }
        _spriteRenderer.sprite = _frames[_frameIndex];
        SetAlpha(Mathf.Lerp(_startAlpha, _endAlpha, _totalElapsed / _totalDuration));
    }

    private void SetAlpha(float alpha)
    {
        Color color = _spriteRenderer.color;
        color.a = alpha;
        _spriteRenderer.color = color;
    }
}

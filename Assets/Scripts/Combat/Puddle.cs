using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// 착지형 무기 웅덩이 - 반경 내 생존 적에게 주기적 피해, 수명 종료 시 풀 반환
/// </summary>
public class Puddle : MonoBehaviour
{
    [Tooltip("스프라이트 기준 반경(유닛) - 스케일 계산 기준")]
    [SerializeField] private float _spriteRadius = 0.25f;

    [Tooltip("피해 판정 대상 레이어")]
    [SerializeField] private LayerMask _enemyLayer = 1 << 7;

    [Header("루프 애니메이션 (선택)")]
    [Tooltip("루프 재생할 프레임 - 비우면 정적 스프라이트")]
    [SerializeField] private Sprite[] _animationFrames;

    [Tooltip("프레임당 재생 시간(초)")]
    [SerializeField] private float _frameDuration = 0.11f;

    private SpriteRenderer _spriteRenderer;
    private IObjectPool<Puddle> _pool;
    private bool _isReleased;

    private float _radius;
    private float _duration;
    private float _tickInterval;
    private int _tickDamage;
    private float _elapsed;
    private float _tickTimer;
    private float _frameTimer;
    private int _frameIndex;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>풀 등록</summary>
    public void SetPool(IObjectPool<Puddle> pool)
    {
        _pool = pool;
    }

    /// <summary>생성 - 위치·반경·지속시간·틱 간격·틱 피해 지정</summary>
    public void Spawn(Vector2 position, float radius, float duration, float tickInterval, int tickDamage)
    {
        transform.position = position;
        float scale = radius / _spriteRadius;
        transform.localScale = new Vector3(scale, scale, 1f);

        _radius = radius;
        _duration = duration;
        _tickInterval = tickInterval;
        _tickDamage = tickDamage;
        _elapsed = 0f;
        _tickTimer = 0f;
        _isReleased = false;

        _frameTimer = 0f;
        _frameIndex = 0;
        if (_spriteRenderer != null && _animationFrames != null && _animationFrames.Length > 0)
            _spriteRenderer.sprite = _animationFrames[0];
    }

    /// <summary>틱 누적 후 피해 적용, 애니 프레임 진행, 수명 종료 시 소멸</summary>
    private void Update()
    {
        _elapsed += Time.deltaTime;
        _tickTimer += Time.deltaTime;

        if (_tickTimer >= _tickInterval)
        {
            _tickTimer -= _tickInterval;
            ApplyTick();
        }

        AdvanceAnimation();

        if (_elapsed >= _duration) Despawn();
    }

    /// <summary>프레임 루프 재생</summary>
    private void AdvanceAnimation()
    {
        if (_animationFrames == null || _animationFrames.Length == 0) return;

        _frameTimer += Time.deltaTime;
        if (_frameTimer >= _frameDuration)
        {
            _frameTimer -= _frameDuration;
            _frameIndex = (_frameIndex + 1) % _animationFrames.Length;
            _spriteRenderer.sprite = _animationFrames[_frameIndex];
        }
    }

    /// <summary>반경 내 생존 적 전원에 틱 피해 - 사망 연출 중인 적 제외</summary>
    private void ApplyTick()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _radius, _enemyLayer);
        foreach (Collider2D hit in hits)
        {
            Health health = hit.GetComponent<Health>();
            if (health == null) continue;

            EnemyMover enemy = hit.GetComponent<EnemyMover>();
            if (enemy != null && enemy.IsDying) continue;

            health.TakeDamage(_tickDamage);
            Projectile.NotifyEnemyDamaged();
        }
    }

    private void Despawn()
    {
        // 한 프레임에 중복 호출 방지
        if (_isReleased) return;
        _isReleased = true;

        if (_pool != null) _pool.Release(this);
        else Destroy(gameObject);
    }
}

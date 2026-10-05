using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;


[RequireComponent(typeof(Rigidbody2D))] // RequireComponent는 해당타입이 클래스에 자동으로 부착
public class Projectile : MonoBehaviour
{
    private int _damage = 1;
    private int _pierceRemaining;
    private float _explosionRadius;

    [Tooltip("화면 밖 넘으면 소멸")]
    [SerializeField] private float _despawnY = 6.0f;
    [Tooltip("좌우 이탈 소멸 거리(유닛) - 전투 영역 반폭 2.81 + 여유")]
    [SerializeField] private float _despawnX = 3.2f;

    [Tooltip("폭발 판정 대상 레이어")]
    [SerializeField] private LayerMask _enemyLayer = 1 << 7;

    [Tooltip("착지형 포물선 높이(유닛) - 시각 효과용")]
    [SerializeField] private float _arcHeight = 0.6f;

    [Tooltip("비행 중 회전 속도(도/초) - 0 이면 회전 없음")]
    [SerializeField] private float _spinSpeed;

    private Rigidbody2D _rigidbody;
    private SpriteRenderer _spriteRenderer;
    private Collider2D _collider;
    private Sprite _defaultSprite; // 무기 스프라이트 미지정 시 복원
    private IObjectPool<Projectile> _pool;
    private ProjectilePool _owner;
    private bool _isReleased;

    // 무기별 표시 - 발사마다 프리팹 기본으로 되돌린 뒤 덮어씀
    private Vector3 _defaultScale;
    private float _currentSpinSpeed;
    private Sprite[] _frames;
    private float _frameInterval;
    private float _frameTimer;
    private int _frameIndex;
    private OneShotSpriteFx _hitFx;
    private OneShotSpriteFx _explosionFx;
    private float _explosionFxScale;

    // 착지형(수류탄식) 비행 상태
    private bool _isLanding;
    private float _flightElapsed;
    private float _flightDuration;
    private Vector2 _flightStart;
    private Vector2 _flightTarget;
    private PuddlePool _puddlePool;
    private float _puddleDuration;
    private float _puddleTickInterval;
    private int _puddleTickDamage;

    // 명중 효과 - 화상·감전
    private float _burnChance;
    private float _burnDuration;
    private int _burnDamagePerSecond;
    private float _stunChance;
    private float _stunDuration;

    // 관통 중 같은 적 중복 명중 방지
    private readonly HashSet<Health> _hitEnemies = new HashSet<Health>();

    /// <summary>무기 투사체·폭발이 적에게 피해를 줄 때마다 호출 - 성배 아티팩트</summary>
    public static event System.Action EnemyDamaged;

    /// <summary>플레이 시작 시 구독 초기화 - 도메인 리로드 꺼짐 대비</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => EnemyDamaged = null;

    /// <summary>투사체 밖(민트 웅덩이 등)의 무기 피해도 명중으로 알림</summary>
    public static void NotifyEnemyDamaged() => EnemyDamaged?.Invoke();

    // 컴포넌트 캐싱
    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _collider = GetComponent<Collider2D>();
        if (_spriteRenderer != null) _defaultSprite = _spriteRenderer.sprite;
        _defaultScale = transform.localScale;
        _currentSpinSpeed = _spinSpeed;
    }

    /// <summary>풀 등록 </summary>
    public void SetPool(IObjectPool<Projectile> pool)
    {
        _pool = pool;
    }

    /// <summary>소속 풀 등록 - 효과 재생용</summary>
    public void SetOwner(ProjectilePool owner) => _owner = owner;

    /// <summary>무기별 표시 적용 - 배율·방향·회전·반복 프레임·효과. Shoot 뒤에 호출</summary>
    public void SetVisual(WeaponData data, Vector2 direction)
    {
        if (data.ProjectileScale > 0f) transform.localScale = new Vector3(data.ProjectileScale, data.ProjectileScale, 1f);
        if (data.ProjectileSpinSpeed > 0f) _currentSpinSpeed = data.ProjectileSpinSpeed;

        if (data.IsFacingDirection)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f; // 그림 위쪽 기준
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        Sprite[] frames = data.ProjectileFrames;
        if (frames != null && frames.Length > 1 && _spriteRenderer != null)
        {
            _frames = frames;
            _frameInterval = Mathf.Max(0.01f, data.ProjectileFrameInterval);
            _spriteRenderer.sprite = frames[0];
        }

        _hitFx = data.HitFx;
        _explosionFx = data.ExplosionFx;
        _explosionFxScale = data.ExplosionFxScale;
    }

    /// <summary>표시 초기화 - 프리팹 기본</summary>
    private void ResetVisual()
    {
        transform.localScale = _defaultScale;
        transform.rotation = Quaternion.identity;
        _currentSpinSpeed = _spinSpeed;
        _frames = null;
        _frameTimer = 0f;
        _frameIndex = 0;
        _hitFx = null;
        _explosionFx = null;
    }

    /// <summary>명중 효과 설정 - Shoot 뒤에 호출</summary>
    public void SetHitEffects(float burnChance, float burnDuration, int burnDamagePerSecond, float stunChance, float stunDuration)
    {
        _burnChance = burnChance;
        _burnDuration = burnDuration;
        _burnDamagePerSecond = burnDamagePerSecond;
        _stunChance = stunChance;
        _stunDuration = stunDuration;
    }

    /// <summary>발사 - 관통 횟수·폭발 반경·색·스프라이트(null = 기본) 포함</summary>
    public void Shoot(Vector2 direction, float speed, int damage, int pierceCount, float explosionRadius, Color color, Sprite sprite)
    {
        _isReleased = false;
        ClearHitEffects();
        ResetVisual();
        _isLanding = false;
        _damage = damage;
        _pierceRemaining = pierceCount;
        _explosionRadius = explosionRadius;
        _hitEnemies.Clear();

        if (_collider != null) _collider.enabled = true;

        if (_spriteRenderer != null)
        {
            _spriteRenderer.color = color;
            _spriteRenderer.sprite = sprite != null ? sprite : _defaultSprite;
        }

        // normalized = 길이 1로. 거리 정보를 버리고 방향만 취함
        // 안 하면 먼 타겟일수록 투사체가 빨라짐
        _rigidbody.linearVelocity = direction.normalized * speed;
    }

    /// <summary>착지형 발사 - 목표 지점까지 포물선 이동 후 폭발 + 웅덩이 생성 (비행 중 충돌 없음)</summary>
    public void ShootLanding(Vector2 landingPosition, float flightDuration, int damage, float explosionRadius,
        PuddlePool puddlePool, float puddleDuration, float puddleTickInterval, int puddleTickDamage,
        Color color, Sprite sprite)
    {
        _isReleased = false;
        ClearHitEffects();
        ResetVisual();
        _damage = damage;
        _pierceRemaining = 0;
        _explosionRadius = explosionRadius;
        _hitEnemies.Clear();

        _isLanding = true;
        _flightElapsed = 0f;
        _flightDuration = Mathf.Max(0.01f, flightDuration);
        _flightStart = transform.position;
        _flightTarget = landingPosition;

        _puddlePool = puddlePool;
        _puddleDuration = puddleDuration;
        _puddleTickInterval = puddleTickInterval;
        _puddleTickDamage = puddleTickDamage;

        if (_collider != null) _collider.enabled = false; // 비행 중 충돌 없음
        _rigidbody.linearVelocity = Vector2.zero;

        if (_spriteRenderer != null)
        {
            _spriteRenderer.color = color;
            _spriteRenderer.sprite = sprite != null ? sprite : _defaultSprite;
        }
    }

    private void Update()
    {
        if (_currentSpinSpeed != 0f) transform.Rotate(0f, 0f, -_currentSpinSpeed * Time.deltaTime); // 투척물 회전
        if (_frames != null) UpdateFrames();

        if (_isLanding)
        {
            UpdateFlight();
            return;
        }

        // 발사체 소멸 - 화면 밖 이탈
        if (transform.position.y >= _despawnY || Mathf.Abs(transform.position.x) >= _despawnX)
        {
            Despawn();
        }
    }

    /// <summary>반복 프레임 순환</summary>
    private void UpdateFrames()
    {
        _frameTimer += Time.deltaTime;
        if (_frameTimer < _frameInterval) return;

        _frameTimer -= _frameInterval;
        _frameIndex = (_frameIndex + 1) % _frames.Length;
        _spriteRenderer.sprite = _frames[_frameIndex];
    }

    /// <summary>착지형 포물선 이동 - 직선 보간 + 사인 곡선 높이 오프셋</summary>
    private void UpdateFlight()
    {
        _flightElapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_flightElapsed / _flightDuration);

        Vector2 groundPosition = Vector2.Lerp(_flightStart, _flightTarget, t);
        float arc = Mathf.Sin(t * Mathf.PI) * _arcHeight;
        transform.position = groundPosition + new Vector2(0f, arc);

        if (t >= 1f) Land();
    }

    /// <summary>착지 - 정확한 위치로 보정 후 폭발 + 웅덩이 생성</summary>
    private void Land()
    {
        _isLanding = false;
        transform.position = _flightTarget;
        if (_collider != null) _collider.enabled = true;

        Explode();
        if (_puddlePool != null) _puddlePool.Spawn(_flightTarget, _explosionRadius, _puddleDuration, _puddleTickInterval, _puddleTickDamage);

        Despawn();
    }

    /// <summary>발사체 명중 - 관통·폭발 판정 후 소멸 여부 결정</summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_isReleased) return;

        Health health = other.GetComponent<Health>();
        if (health == null || _hitEnemies.Contains(health)) return;

        _hitEnemies.Add(health);

        if (_explosionRadius > 0f)
        {
            Explode();
        }
        else
        {
            DamageEnemy(health);
            if (_hitFx != null && _owner != null) _owner.SpawnFx(_hitFx, other.bounds.center, 1f);
        }

        // 관통 남으면 소멸하지 않고 1 감소
        if (_pierceRemaining > 0)
        {
            _pierceRemaining--;
        }
        else
        {
            Despawn();
        }
    }

    /// <summary>반경 내 적 전체에 동일 피해</summary>
    private void Explode()
    {
        Vector2 center = transform.position;
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, _explosionRadius, _enemyLayer);
        foreach (Collider2D hit in hits)
        {
            Health health = hit.GetComponent<Health>();
            if (health != null) DamageEnemy(health);
        }

        if (_explosionFx != null && _owner != null) _owner.SpawnFx(_explosionFx, center, _explosionFxScale);

        DrawExplosionGizmo(center);
    }

    /// <summary>적 피해 적용 + 명중 알림</summary>
    private void DamageEnemy(Health health)
    {
        if (health.IsDead) return;

        health.TakeDamage(_damage);
        EnemyDamaged?.Invoke();

        ApplyHitEffects(health);
    }

    /// <summary>화상·감전 확률 판정 - 살아남은 적만</summary>
    private void ApplyHitEffects(Health health)
    {
        if (health.IsDead) return;
        if (_burnChance <= 0f && _stunChance <= 0f) return;
        if (!health.TryGetComponent(out EnemyMover enemy)) return;

        if (Random.value < _burnChance) enemy.ApplyBurn(_burnDuration, _burnDamagePerSecond);
        if (Random.value < _stunChance) enemy.ApplyStun(_stunDuration);
    }

    /// <summary>명중 효과 초기화</summary>
    private void ClearHitEffects()
    {
        _burnChance = 0f;
        _stunChance = 0f;
    }

    /// <summary>폭발 범위 시각 확인용 - Scene 뷰 전용</summary>
    private void DrawExplosionGizmo(Vector2 center)
    {
        const int segments = 16;
        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * Mathf.PI * 2f / segments;
            float angle2 = (i + 1) * Mathf.PI * 2f / segments;
            Vector2 p1 = center + new Vector2(Mathf.Cos(angle1), Mathf.Sin(angle1)) * _explosionRadius;
            Vector2 p2 = center + new Vector2(Mathf.Cos(angle2), Mathf.Sin(angle2)) * _explosionRadius;
            Debug.DrawLine(p1, p2, Color.red, 0.2f);
        }
    }

    /// <summary>소멸 - 풀 반환</summary>
    public void Despawn()
    {
        // 한 프레임에 적 2마리와 겹치면 OnTriggerEnter2D 가 2번 호출
        // 중복 반환 시 collectionCheck 가 예외를 던지므로 차단
        if (_isReleased) return;
        _isReleased = true;

        // 재사용 대비 상태 초기화. 안 하면 다음에 꺼냈을 때 이미 움직임
        _rigidbody.linearVelocity = Vector2.zero;
        _pierceRemaining = 0;
        _hitEnemies.Clear();
        _isLanding = false;
        transform.rotation = Quaternion.identity;

        if (_pool != null)
        {
            _pool.Release(this);   // 끄고 풀로 반환
        }
        else
        {
            Destroy(gameObject);   // 풀 없이 쓰는 경우 대비
        }
    }
}

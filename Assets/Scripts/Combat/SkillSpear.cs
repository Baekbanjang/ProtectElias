using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>스킬 창 - 줄을 따라 위로 직진, 창끝이 지난 적마다 1회 피해·명중 섬광, 일정 간격 잔상</summary>
public class SkillSpear : MonoBehaviour
{
    [Tooltip("이동 속도(유닛/초)")]
    [SerializeField] private float _speed = 14f;

    [Tooltip("창끝 위치 - 피벗(중심)에서 위쪽 거리(유닛)")]
    [SerializeField] private float _tipOffset = 1.3046875f;

    [Tooltip("판정 반폭(유닛) - 줄 중심 기준, 적 피격 범위와 겹치면 명중")]
    [SerializeField] private float _hitHalfWidth = 0.45f;

    [Tooltip("판정 상한(유닛) - 전투 영역 위 끝. 화면 밖 적 제외")]
    [SerializeField] private float _hitTopY = 4.125f;

    [Tooltip("소멸 높이(유닛) - 중심이 넘으면 반환")]
    [SerializeField] private float _despawnY = 5.8f;

    [Tooltip("잔상 생성 간격(초)")]
    [SerializeField] private float _trailInterval = 0.06f;

    [SerializeField] private OneShotSpriteFx _trailPrefab;
    [SerializeField] private OneShotSpriteFx _impactPrefab;

    private readonly HashSet<EnemyMover> _hitEnemies = new HashSet<EnemyMover>();
    private readonly List<EnemyMover> _newHits = new List<EnemyMover>(); // 순회 중 사망 제거 대비
    private SkillController _owner;
    private IObjectPool<SkillSpear> _pool;
    private int _damage;
    private float _trailTimer;

    public OneShotSpriteFx TrailPrefab => _trailPrefab;
    public OneShotSpriteFx ImpactPrefab => _impactPrefab;

    /// <summary>풀 등록</summary>
    public void SetPool(IObjectPool<SkillSpear> pool) => _pool = pool;

    /// <summary>발사 - 위치·피해 지정, 명중 기록 초기화</summary>
    public void Launch(SkillController owner, Vector2 position, int damage)
    {
        _owner = owner;
        _damage = damage;
        _trailTimer = 0f;
        _hitEnemies.Clear();
        transform.position = position;
    }

    /// <summary>이동 → 창끝 판정 → 잔상 → 소멸</summary>
    private void Update()
    {
        transform.position += Vector3.up * (_speed * Time.deltaTime);
        Vector2 position = transform.position;

        HitEnemies(position.x, Mathf.Min(position.y + _tipOffset, _hitTopY));

        _trailTimer -= Time.deltaTime;
        if (_trailTimer <= 0f)
        {
            _trailTimer += _trailInterval;
            _owner.SpawnTrail(position);
        }

        if (position.y >= _despawnY)
        {
            _hitEnemies.Clear();
            _pool.Release(this);
        }
    }

    /// <summary>줄 안에서 창끝 아래로 들어온 적 - 1회씩 피해 + 명중 섬광</summary>
    private void HitEnemies(float laneX, float tipY)
    {
        _newHits.Clear();
        foreach (EnemyMover enemy in EnemyMover.Active)
        {
            if (enemy.IsDying || _hitEnemies.Contains(enemy)) continue;

            Bounds bounds = enemy.HitBounds;
            if (bounds.max.x < laneX - _hitHalfWidth || bounds.min.x > laneX + _hitHalfWidth) continue;
            if (bounds.center.y > tipY) continue;

            _newHits.Add(enemy);
        }

        foreach (EnemyMover enemy in _newHits)
        {
            _hitEnemies.Add(enemy);
            _owner.SpawnImpact(enemy.HitBounds.center);
            if (enemy.TryGetComponent(out Health health)) health.TakeDamage(_damage);
        }
    }
}

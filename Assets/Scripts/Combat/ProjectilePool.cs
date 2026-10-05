using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// 투사체 풀
/// </summary>
public class ProjectilePool : MonoBehaviour
{
    [SerializeField] private Projectile _prefab;

    [Tooltip("내부 저장소 초기 용량")] 
    [SerializeField]
    private int _defaultCapacity = 32;
    
    [Tooltip("풀 상한. 초과분은 반환되지 않고 파괴")]
    [SerializeField] private int _maxSize = 200;

    [Tooltip("동시 투사체 상한 - 초과 시 가장 오래된 것부터 회수 (안전장치, 닿으면 버그 신호)")]
    [SerializeField] private int _maxActive = 200;

    [Tooltip("착지형 무기 전용 - 웅덩이 풀 (해당 없으면 비움)")]
    [SerializeField] private PuddlePool _puddlePool;

    private ObjectPool<Projectile> _pool;
    private readonly List<Projectile> _active = new List<Projectile>(); // 대여 순서 = 오래된 순
    private readonly Dictionary<OneShotSpriteFx, ObjectPool<OneShotSpriteFx>> _fxPools = new Dictionary<OneShotSpriteFx, ObjectPool<OneShotSpriteFx>>(); // 효과 종류별 풀

    /// <summary>상한 도달로 강제 회수된 누적 수 - 0 아니면 조사</summary>
    public int ReclaimedCount { get; private set; }

    public PuddlePool PuddlePool => _puddlePool;

    /// <summary>풀 구성</summary>
    private void Awake()
    {
        _pool = new ObjectPool<Projectile>(
            createFunc: Create, // 풀이 비었을 떄 생성
            actionOnGet: OnGet, // 객체 꺼낼 떄
            actionOnRelease: OnRelease, // 객체 되돌릴 때
            actionOnDestroy: OnDestroyItem, // 풀 사이즈 초과 시 삭제할 때
            collectionCheck: true, // 중복 반환 검사
            defaultCapacity: _defaultCapacity, // 내부 저장소 초기 용량
            maxSize: _maxSize // 풀 사이즈
            );
    }

    /// <summary>투사체 1개 대여</summary>
    public Projectile Get(Vector2 position)
    {
        if (_active.Count >= _maxActive) ReclaimOldest();

        Projectile projectile = _pool.Get();
        projectile.transform.position = position;
        return projectile;
    }
    
    /// <summary>상한 도달 - 가장 오래된 투사체 강제 회수</summary>
    private void ReclaimOldest()
    {
        if (ReclaimedCount == 0) Debug.LogWarning($"동시 투사체 상한({_maxActive}) 도달 - 오래된 것부터 회수. 밸런스가 아니라 버그 신호");

        ReclaimedCount++;
        _active[0].Despawn();
    }

    /// <summary>풀이 비었을 때만 호출</summary>
    private Projectile Create()
    {
        Projectile projectile = Instantiate(_prefab, transform);
        projectile.SetPool(_pool);
        projectile.SetOwner(this);
        return projectile;
    }

    /// <summary>명중·폭발 효과 1회 재생 - 종류별 풀</summary>
    public void SpawnFx(OneShotSpriteFx prefab, Vector2 position, float scale)
    {
        if (!_fxPools.TryGetValue(prefab, out ObjectPool<OneShotSpriteFx> pool))
        {
            pool = CreateFxPool(prefab);
            _fxPools.Add(prefab, pool);
        }

        OneShotSpriteFx fx = pool.Get();
        fx.transform.localScale = new Vector3(scale, scale, 1f);
        fx.Play(position);
    }

    /// <summary>효과 풀 생성</summary>
    private ObjectPool<OneShotSpriteFx> CreateFxPool(OneShotSpriteFx prefab)
    {
        ObjectPool<OneShotSpriteFx> pool = null;
        pool = new ObjectPool<OneShotSpriteFx>(
            () => { OneShotSpriteFx fx = Instantiate(prefab, transform); fx.SetPool(pool); return fx; },
            fx => fx.gameObject.SetActive(true),
            fx => fx.gameObject.SetActive(false));
        return pool;
    }

    /// <summary>대여 시</summary>
    private void OnGet(Projectile projectile)
    {
        projectile.gameObject.SetActive(true);
        _active.Add(projectile);
    }

    /// <summary>반환 시</summary>
    private void OnRelease(Projectile projectile)
    {
        projectile.gameObject.SetActive(false);
        _active.Remove(projectile);
    }

    /// <summary>상한 초과 시에만 호출 </summary>
    private void OnDestroyItem(Projectile projectile)
    {
        // Play 종료 시엔 씬과 함께 이미 파괴
        if (projectile == null) return;

        Debug.LogWarning($"투사체 풀 상한({_maxSize}) 초과로 파괴. 밸런스가 아니라 버그 신호");
        
        Destroy(projectile.gameObject);
    }
}

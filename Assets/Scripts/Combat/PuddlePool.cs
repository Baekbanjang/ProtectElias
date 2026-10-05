using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// 웅덩이 풀
/// </summary>
public class PuddlePool : MonoBehaviour
{
    [SerializeField] private Puddle _prefab;

    [Tooltip("내부 저장소 초기 용량")]
    [SerializeField] private int _defaultCapacity = 16;

    [Tooltip("풀 상한. 초과분은 반환되지 않고 파괴")]
    [SerializeField] private int _maxSize = 64;

    private ObjectPool<Puddle> _pool;

    /// <summary>풀 구성</summary>
    private void Awake()
    {
        _pool = new ObjectPool<Puddle>(
            createFunc: Create,
            actionOnGet: OnGet,
            actionOnRelease: OnRelease,
            actionOnDestroy: OnDestroyItem,
            collectionCheck: true,
            defaultCapacity: _defaultCapacity,
            maxSize: _maxSize
            );
    }

    /// <summary>웅덩이 1개 대여 - 위치·반경·지속시간·틱 간격·틱 피해 지정</summary>
    public Puddle Spawn(Vector2 position, float radius, float duration, float tickInterval, int tickDamage)
    {
        Puddle puddle = _pool.Get();
        puddle.Spawn(position, radius, duration, tickInterval, tickDamage);
        return puddle;
    }

    /// <summary>풀이 비었을 때만 호출</summary>
    private Puddle Create()
    {
        Puddle puddle = Instantiate(_prefab, transform);
        puddle.SetPool(_pool);
        return puddle;
    }

    private void OnGet(Puddle puddle)
    {
        puddle.gameObject.SetActive(true);
    }

    private void OnRelease(Puddle puddle)
    {
        puddle.gameObject.SetActive(false);
    }

    private void OnDestroyItem(Puddle puddle)
    {
        if (puddle == null) return;

        Debug.LogWarning($"웅덩이 풀 상한({_maxSize}) 초과로 파괴. 밸런스가 아니라 버그 신호");

        Destroy(puddle.gameObject);
    }
}

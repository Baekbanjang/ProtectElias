using UnityEngine;
using UnityEngine.Events;


/// <summary>
/// 체력 공용 컴포넌트 - 플레이어, 적, 보스
/// 사망 시 동작은 OnDied 연결처가 결정
/// </summary>
public class Health : MonoBehaviour
{
    [SerializeField] private int _maxHp = 100;

    [Tooltip("HP 0 도달 시 호출. 인스펙터에서 연결")] 
    [SerializeField] private UnityEvent _onDied;

    [Tooltip("피해 적용 시마다 호출 - 피격 연출 연결")]
    [SerializeField] private UnityEvent _onDamaged;

    private int _currentHp;
    private bool _isDead;
    
    // 읽기 적용 - >=는 읽기 적용 프로퍼티(Get)
    public int MaxHp => _maxHp;
    public int CurrentHp => _currentHp;
    public bool IsDead => _isDead;

    private void Awake()
    {
        _currentHp = _maxHp;
    }

    /// <summary>피해 적용, 사망 판정. 사망 후 호출은 무시</summary>
    public void TakeDamage(int amount)
    {
        if (_isDead) return;
        
        _currentHp -= amount;
        Debug.Log($"{name} HP {_currentHp} / {_maxHp}  (−{amount})");
        _onDamaged.Invoke();

        if (_currentHp <= 0)
        {
            _currentHp = 0;
            _isDead = true;
            _onDied.Invoke();
        }
    }
    
    /// <summary>최대 HP 증가 + 증가분만큼 회복</summary>
    public void IncreaseMaxHp(int amount)
    {
        if (_isDead) return;

        _maxHp += amount;
        _currentHp += amount;
        Debug.Log($"{name} 최대 HP {_maxHp}  (+{amount})  현재 {_currentHp}");
    }

    /// <summary>최대 HP 증감 - 회복 없음, 현재 HP 는 새 최대치로 제한(사망 없음)</summary>
    public void AdjustMaxHp(int amount)
    {
        if (_isDead) return;

        _maxHp = Mathf.Max(1, _maxHp + amount);
        _currentHp = Mathf.Min(_currentHp, _maxHp);
        Debug.Log($"{name} 최대 HP {_maxHp}  ({amount:+0;-0})  현재 {_currentHp}");
    }

    /// <summary>HP 회복 - 최대 HP 초과 불가</summary>
    public void Heal(int amount)
    {
        if (_isDead) return;

        _currentHp = Mathf.Min(_maxHp, _currentHp + amount);
    }
}

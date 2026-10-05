using UnityEngine;
using UnityEngine.Events;

/// <summary>플레이어 경험치 · 레벨</summary>
public class PlayerLevel : MonoBehaviour
{
    [Tooltip("기본 필요 EXP")]
    [SerializeField] private int _baseExp = 5;
    
    [Tooltip("레벨당 필요 EXP 증가량")]
    [SerializeField] private int _expGrowth = 3;
    
    [Tooltip("레벨업 시 호출 이벤트")]
    [SerializeField] private UnityEvent _onLevelUp;
    
    [Tooltip("경험치 특성 배율 참조")]
    [SerializeField] private PlayerStats _stats;


    public int Level { get; private set; } = 1;
    public float CurrentExp { get; private set; }
    public int ExpToNext => _baseExp + _expGrowth * (Level - 1);

    /// <summary>EXP 획득 - 넘친 EXP 는 다음 레벨로 이월</summary>
    public void AddExp(int amount)
    {
        CurrentExp += amount * _stats.ExpMultiplier;
        while (CurrentExp >= ExpToNext) // 한 번에 여러 레벨업 가능
        {
            CurrentExp -= ExpToNext;
            Level++;
            Debug.Log($"레벨 업 - Lv.{Level}");
            _onLevelUp.Invoke();
        }
    }
}

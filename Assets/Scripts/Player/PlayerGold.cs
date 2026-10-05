using UnityEngine;

/// <summary>플레이어 골드 - 한 판 지갑, 처치 획득·새로고침 소비</summary>
public class PlayerGold : MonoBehaviour
{
    private const float FloatTolerance = 0.001f; // float 누적 오차 보정 (1.2 × 10 = 11.999…)

    private PlayerStats _stats;
    private float _gold;        // 현재 골드 - 특성 배율 소수점 누적
    private float _totalEarned; // 이번 판 획득 합계

    public int Gold => ToWhole(_gold);
    public int TotalEarned => ToWhole(_totalEarned);

    private void Awake()
    {
        _stats = GetComponent<PlayerStats>();
    }

    /// <summary>골드 획득 - 골드 특성 배율 적용</summary>
    public void AddGold(int amount)
    {
        float gain = amount * _stats.GoldMultiplier;
        _gold += gain;
        _totalEarned += gain;
    }

    /// <summary>골드 소비 - 부족하면 false</summary>
    public bool TrySpend(int cost)
    {
        if (Gold < cost) return false;

        _gold -= cost;
        return true;
    }

    private static int ToWhole(float value) => Mathf.FloorToInt(value + FloatTolerance);
}

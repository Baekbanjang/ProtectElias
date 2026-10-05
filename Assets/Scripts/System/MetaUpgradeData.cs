using UnityEngine;

/// <summary>메타 강화 항목 - 레벨 저장·단계별 구매 비용·초기화 환불</summary>
[CreateAssetMenu(fileName = "MetaUpgradeData", menuName = "Scriptable Objects/MetaUpgradeData")]
public class MetaUpgradeData : ScriptableObject
{
    [Tooltip("저장 키 - DUT_Meta_<id>")]
    [SerializeField] private string _id;

    [SerializeField] private string _displayName;

    [TextArea]
    [SerializeField] private string _description;

    [SerializeField] private Sprite _icon;

    [SerializeField] private int _maxLevel = 5;

    [Tooltip("다음 레벨 비용 = 이 값 × 다음 레벨")]
    [SerializeField] private int _costPerLevel = 50;

    [Tooltip("단계별 구매 비용 - 비어 있으면 기존 비용 공식 사용")]
    [SerializeField] private int[] _levelCosts;

    [Tooltip("레벨당 효과 - 비율은 0.1 = 10%")]
    [SerializeField] private float _valuePerLevel;

    public string DisplayName => _displayName;
    public string Description => _description;
    public Sprite Icon => _icon;
    public int MaxLevel => _maxLevel;

    public int Level => SaveData.GetMetaLevel(_id);
    public bool IsMaxLevel => Level >= _maxLevel;
    public int NextCost => IsMaxLevel ? 0 : GetLevelCost(Level + 1);
    public int SpentGold
    {
        get
        {
            int total = 0;
            for (int level = 1; level <= Level; level++) total += GetLevelCost(level);
            return total;
        }
    }
    public float TotalValue => _valuePerLevel * Level;               // 현재 레벨 효과 합
    public int TotalCount => Mathf.RoundToInt(TotalValue);           // 횟수·칸 수 효과

    private int GetLevelCost(int level)
    {
        return _levelCosts != null && level <= _levelCosts.Length
            ? _levelCosts[level - 1]
            : _costPerLevel * level;
    }

    /// <summary>다음 레벨 구매 - 최대 레벨·골드 부족이면 false</summary>
    public bool TryUpgrade()
    {
        if (IsMaxLevel || SaveData.MetaGold < NextCost) return false;

        SaveData.AddMetaGold(-NextCost);
        SaveData.SetMetaLevel(_id, Level + 1);
        return true;
    }

    /// <summary>레벨 0 초기화 - 쓴 골드 환불</summary>
    public void ResetLevel()
    {
        SaveData.AddMetaGold(SpentGold);
        SaveData.SetMetaLevel(_id, 0);
    }
}

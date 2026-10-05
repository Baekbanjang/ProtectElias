using UnityEngine;

/// <summary>
/// 특성 데이터
/// </summary>
[CreateAssetMenu(fileName = "TraitData", menuName = "Scriptable Objects/TraitData")]
public class TraitData : ScriptableObject
{
    private static readonly string[] GradeNames = { "I", "II", "III" };

    [Tooltip("카드 이름")]
    [SerializeField] private string _displayName;

    [Tooltip("카드 설명")]
    [SerializeField] private string _descriptionFormat;

    [SerializeField] private StatType _statType;

    [SerializeField] private float[] _values = new float[3];

    [Tooltip("% 표시 여부")]
    [SerializeField] private bool _isPercent = true;

    [SerializeField] private Sprite _icon;

    [Tooltip("고유 특성 - 등급 없음, 한 판 1회만 후보")]
    [SerializeField] private bool _isUnique;

    [Tooltip("태그 조건 - 체크 시 보유한 총기 중 아래 타입이 있을 때만 후보")]
    [SerializeField] private bool _hasAttackTypeCondition;

    [SerializeField] private AttackType _requiredAttackType;

    public StatType StatType => _statType;
    public Sprite Icon => _icon;
    public bool IsUnique => _isUnique;
    public bool HasAttackTypeCondition => _hasAttackTypeCondition;
    public AttackType RequiredAttackType => _requiredAttackType;

    /// <summary>등급 수치</summary>
    public float GetValue(int grade) => _values[grade - 1];

    /// <summary>카드 이름 - 고유 특성은 등급 표기 없음</summary>
    public string GetTitle(int grade) => _isUnique ? _displayName : $"{_displayName} {GradeNames[grade - 1]}";

    /// <summary>카드 설명 </summary>
    public string GetDescription(int grade)
    {
        float value = GetValue(grade);
        float shown = _isPercent ? value * 100f : value;
        return string.Format(_descriptionFormat, Mathf.RoundToInt(shown));
    }
}

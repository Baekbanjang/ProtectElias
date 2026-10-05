using System.Globalization;
using UnityEngine;

/// <summary>아티팩트 데이터 - 공격 없음, 격자에 있는 동안 패시브 효과</summary>
[CreateAssetMenu(fileName = "ArtifactData", menuName = "Scriptable Objects/ArtifactData")]
public class ArtifactData : GridItemData
{
    private static readonly string[] RarityNames = { "일반", "고급", "희귀", "전설" };

    [Tooltip("원작 등급 - 상점 출현 가중치·정보 창 표기만 (인벤토리 등급은 상점 롤)")]
    [SerializeField] private ArtifactRarity _rarity = ArtifactRarity.Common;

    [Tooltip("효과 - Attack·CritChance·MaxHp·SkillCooldownOnHit·DefenseDamageReduction")]
    [SerializeField] private StatType _effectType;

    [Tooltip("1등급 효과 수치 (× 등급) - 비율은 0.04 = 4%, 최대 HP 는 정수, 스킬 감소는 초")]
    [SerializeField] private float _value;

    public ArtifactRarity Rarity => _rarity;
    public StatType EffectType => _effectType;
    public string RarityName => RarityNames[(int)_rarity - 1]; // 정보 창 표기

    /// <summary>등급 반영 효과 수치 - 기본값 × 등급</summary>
    public float GetValue(int grade) => _value * grade;

    /// <summary>정보 창 설명 - 설명의 {0} 자리에 등급 반영 수치</summary>
    public string GetDescription(int grade)
    {
        float value = GetValue(grade);
        bool isRatio = _effectType != StatType.MaxHp && _effectType != StatType.SkillCooldownOnHit;
        string text = isRatio
            ? (value * 100f).ToString("0.#", CultureInfo.InvariantCulture)
            : value.ToString("0.##", CultureInfo.InvariantCulture);
        return string.Format(Description, text);
    }
}

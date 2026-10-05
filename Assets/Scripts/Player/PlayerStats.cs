using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 스탯
/// </summary>
public class PlayerStats : MonoBehaviour
{
    private const float BaseCritMultiplier = 1.5f;    // 치명타 기본 배율
    private const float MinCooldownMultiplier = 0.5f; // 일반·타입 감소 합산 상한 50%
    private const float MaxAttackBonus = 1f;          // 메타·아티팩트 포함 공격력 상한
    private const float MaxCritChance = 0.6f;         // 아티팩트 포함 치명타 확률 상한
    private const float LastStandHpRatio = 0.3f;      // 배수진 발동 HP 비율
    private const float LastStandDamageBonus = 0.4f;  // 배수진 공격력 보너스
    private const float MinDefenseDamageMultiplier = 0.5f; // 방어선 피해 배율 하한
    private const float MaxLifeStealChance = 0.15f;   // 흡혈 발동 확률 상한

    [Tooltip("최대 HP 특성 전달처")]
    [SerializeField] private Health _health;

    // 특성 누적값 - 비율 저장 (0.08 = 8%) · 같은 특성 재선택 시 합산
    [Header("특성 합산")]
    [SerializeField] private float _attackBonus;          // 공격력 특성
    [SerializeField] private float _attackSpeedBonus;     // 공격 속도 특성 - 발사 간격 감소량
    [SerializeField] private float _projectileSpeedBonus; // 투사체 속도 특성
    [SerializeField] private float _critChance;           // 치명타 확률 특성
    [SerializeField] private float _critDamageBonus;      // 치명타 피해 특성
    [SerializeField] private float _expBonus;             // 경험치 획득 특성
    [SerializeField] private float _goldBonus;            // 골드 획득 특성
    [SerializeField] private float _lifeStealChance;      // 흡혈 - 처치 시 회복 확률
    // 최대 HP 특성은 필드 없음 - Health.IncreaseMaxHp 로 직접 전달

    [Header("고유 특성")]
    [SerializeField] private int _multishotBonus;   // 멀티샷 - 전체 무기 발사 수 +N
    [SerializeField] private bool _hasLastStand;    // 배수진 - HP 30% 이하 공격력 +40%
    [SerializeField] private bool _hasBounty;       // 현상금 - 보스 처치 골드 2배

    [Header("아티팩트 전용 합산")]
    [SerializeField] private float _skillCooldownPerHit;    // 무기 명중마다 스킬 재사용 대기 감소(초)
    [SerializeField] private float _defenseDamageReduction; // 방어선 받는 피해 감소 비율

    [Header("메타 강화 (판 시작 적용)")]
    [SerializeField] private MetaUpgradeData _metaExp;       // 경험치 획득 %
    [SerializeField] private MetaUpgradeData _metaGold;      // 골드 획득 %
    [SerializeField] private MetaUpgradeData _metaAttack;    // 공격력 %
    [SerializeField] private MetaUpgradeData _metaBarrierHp; // 바리케이드 최대 HP

    // 태그(공격 타입)별 보너스 - 특성 선택으로만 채워짐
    private readonly Dictionary<AttackType, float> _typeDamageBonus = new Dictionary<AttackType, float>();
    private readonly Dictionary<AttackType, float> _typeReloadBonus = new Dictionary<AttackType, float>();
    private readonly Dictionary<AttackType, float> _typeRadiusBonus = new Dictionary<AttackType, float>();

    // 읽기 전용 - 무기·투사체·레벨이 곱해 쓰는 최종 배율
    public float ProjectileSpeedMultiplier => 1f + _projectileSpeedBonus;                         // 투사체 속도 배율
    public float CritChance => Mathf.Clamp(_critChance, 0f, MaxCritChance);                         // 치명타 확률
    public float CritMultiplier => BaseCritMultiplier + _critDamageBonus;                         // 치명타 피해 배율
    public float ExpMultiplier => 1f + _expBonus;                                                 // 경험치 배율
    public float GoldMultiplier => 1f + _goldBonus;                                               // 골드 배율
    public int MultishotBonus => _multishotBonus;                                                 // 멀티샷 - 발사 수 가산
    public float LifeStealChance => _lifeStealChance;                                             // 흡혈 발동 확률
    public bool HasReachedLifeStealCap => _lifeStealChance >= MaxLifeStealChance || Mathf.Approximately(_lifeStealChance, MaxLifeStealChance);
    public bool HasReachedAttackCap => IsAtCap(_attackBonus, MaxAttackBonus);
    public bool HasReachedAttackSpeedCap => IsAtCap(_attackSpeedBonus, 1f - MinCooldownMultiplier);
    public bool HasReachedCritChanceCap => IsAtCap(_critChance, MaxCritChance);
    public bool HasBounty => _hasBounty;
    public float SkillCooldownPerHit => _skillCooldownPerHit;                                     // 명중당 스킬 대기 감소(초)

    /// <summary>메타 강화 적용 - 특성 합산값에 더함, 최대 HP 는 Health 로 전달</summary>
    private void Awake()
    {
        _expBonus += _metaExp.TotalValue;
        _goldBonus += _metaGold.TotalValue;
        _attackBonus += _metaAttack.TotalValue;
        if (_metaBarrierHp.TotalCount > 0) _health.IncreaseMaxHp(_metaBarrierHp.TotalCount);
    }

    /// <summary>타입별 피해 배율 - 공격력 + 태그 대미지 + 배수진 합산</summary>
    public float GetDamageMultiplier(AttackType type)
    {
        float bonus = Mathf.Clamp(_attackBonus, 0f, MaxAttackBonus) + GetTypeDamageBonus(type);
        if (_hasLastStand && _health.CurrentHp <= _health.MaxHp * LastStandHpRatio) bonus += LastStandDamageBonus;
        return 1f + bonus;
    }

    /// <summary>타입별 발사 간격 배율 - 공격속도 + 태그 재장전 감소, 하한 적용</summary>
    public float GetCooldownMultiplier(AttackType type)
    {
        return Mathf.Max(MinCooldownMultiplier, 1f - _attackSpeedBonus - GetTypeReloadBonus(type));
    }

    /// <summary>해당 타입의 일반·타입 공격 간격 합산 상한 여부</summary>
    public bool HasReachedCooldownCap(AttackType type) => IsAtCap(_attackSpeedBonus + GetTypeReloadBonus(type), 1f - MinCooldownMultiplier);

    private static bool IsAtCap(float value, float cap) => value >= cap || Mathf.Approximately(value, cap);

    /// <summary>타입별 폭발 반경 배율 - 태그 범위 증가 합산</summary>
    public float GetExplosionRadiusMultiplier(AttackType type) => 1f + GetTypeRadiusBonus(type);

    /// <summary>방어선 피해 감소 적용 - 배율 하한 0.5, 원래 피해가 있으면 최소 1</summary>
    public int ReduceDefenseDamage(int amount)
    {
        if (amount <= 0) return amount;

        float multiplier = Mathf.Max(MinDefenseDamageMultiplier, 1f - _defenseDamageReduction);
        double reduced = System.Math.Round(amount * (double)multiplier, 4); // 부동소수 오차 제거 (4.4999999 → 4.5)
        return Mathf.Max(1, (int)System.Math.Round(reduced, System.MidpointRounding.AwayFromZero));
    }

    private float GetTypeDamageBonus(AttackType type) => _typeDamageBonus.TryGetValue(type, out float value) ? value : 0f;
    private float GetTypeReloadBonus(AttackType type) => _typeReloadBonus.TryGetValue(type, out float value) ? value : 0f;
    private float GetTypeRadiusBonus(AttackType type) => _typeRadiusBonus.TryGetValue(type, out float value) ? value : 0f;

    /// <summary>특성 카드 적용 - 일반/태그/고유 분기</summary>
    public void ApplyTrait(TraitData trait, int grade)
    {
        if (trait.IsUnique)
        {
            ApplyUniqueTrait(trait.StatType);
            return;
        }

        float value = trait.GetValue(grade);
        if (trait.HasAttackTypeCondition)
        {
            AddTypeBonus(trait.RequiredAttackType, trait.StatType, value);
            return;
        }

        AddBonus(trait.StatType, value);
    }

    /// <summary>아티팩트 효과 가감 - 수치 = 기본값 × 등급 변화량(음수 = 제거), 최대 HP 는 회복 없이 최대치만</summary>
    public void ApplyArtifact(ArtifactData artifact, int gradeDelta)
    {
        float value = artifact.GetValue(gradeDelta);
        if (artifact.EffectType == StatType.MaxHp)
        {
            _health.AdjustMaxHp(Mathf.RoundToInt(value));
            return;
        }

        AddBonus(artifact.EffectType, value);
    }

    /// <summary>HP 회복 - 베개 아티팩트</summary>
    public void Heal(int amount) => _health.Heal(amount);

    /// <summary>적 처치 시 확률적으로 최대 체력의 1% 회복</summary>
    public void TryLifeStealOnKill()
    {
        if (_health.IsDead || _lifeStealChance <= 0f || Random.value >= _lifeStealChance) return;

        int amount = Mathf.Max(1, (int)System.Math.Round(_health.MaxHp * 0.01d, System.MidpointRounding.AwayFromZero));
        _health.Heal(amount);
    }

    /// <summary>일반 특성 보너스 합산. 최대 HP 는 Health 로 전달</summary>
    private void AddBonus(StatType type, float value)
    {
        switch (type)
        {
            case StatType.Attack:          _attackBonus += value; break;
            case StatType.AttackSpeed:     _attackSpeedBonus += value; break;
            case StatType.ProjectileSpeed: _projectileSpeedBonus += value; break;
            case StatType.MaxHp:           _health.IncreaseMaxHp(Mathf.RoundToInt(value)); break;
            case StatType.CritChance:      _critChance += value; break;
            case StatType.CritDamage:      _critDamageBonus += value; break;
            case StatType.Exp:             _expBonus += value; break;
            case StatType.Gold:            _goldBonus += value; break;
            case StatType.LifeSteal:       _lifeStealChance = Mathf.Clamp(_lifeStealChance + value, 0f, MaxLifeStealChance); break;
            case StatType.SkillCooldownOnHit:     _skillCooldownPerHit += value; break;
            case StatType.DefenseDamageReduction: _defenseDamageReduction += value; break;
        }
        Debug.Log($"스탯 {type} {(value >= 0f ? "+" : "")}{value}");
    }

    /// <summary>태그(공격 타입) 특성 보너스 합산</summary>
    private void AddTypeBonus(AttackType attackType, StatType statType, float value)
    {
        if (statType == StatType.TypeDamage)
        {
            _typeDamageBonus[attackType] = GetTypeDamageBonus(attackType) + value;
        }
        else if (statType == StatType.TypeReload)
        {
            _typeReloadBonus[attackType] = GetTypeReloadBonus(attackType) + value;
        }
        else if (statType == StatType.TypeRadius)
        {
            _typeRadiusBonus[attackType] = GetTypeRadiusBonus(attackType) + value;
        }
        Debug.Log($"태그 스탯 {attackType}/{statType} +{value}");
    }

    /// <summary>고유 특성 적용 - 한 판 1회</summary>
    private void ApplyUniqueTrait(StatType statType)
    {
        switch (statType)
        {
            case StatType.Multishot: _multishotBonus += 1; break;
            case StatType.LastStand: _hasLastStand = true; break;
            case StatType.Bounty:    _hasBounty = true; break;
        }
        Debug.Log($"고유 특성 {statType} 적용");
    }
}

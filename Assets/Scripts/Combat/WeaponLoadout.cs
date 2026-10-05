using System.Collections.Generic;
using UnityEngine;

/// <summary>격자 배치 무기 → Weapon 컴포넌트 생성·제거·등급 갱신, 소환 무기는 소환체 생성·배치, 아티팩트는 스탯 적용</summary>
public class WeaponLoadout : MonoBehaviour
{
    [Tooltip("무기 공용 투사체 풀")]
    [SerializeField] private ProjectilePool _pool;

    [Tooltip("등급별 피해 배율 (1~4등급)")]
    [SerializeField] private float[] _gradeDamageMultipliers = { 1f, 2f, 4f, 8f };

    [Header("소환체")]
    [Tooltip("소환체 프리팹 - 던지기 Weapon")]
    [SerializeField] private Weapon _summonPrefab;

    [Tooltip("소환 투척물 풀")]
    [SerializeField] private ProjectilePool _summonPool;

    [Tooltip("소환체 좌우 간격(유닛)")]
    [SerializeField] private float _summonSpacing = 0.75f;

    [Tooltip("소환체 배치 한계 - 비비 기준 좌우 거리, 넘으면 간격 축소")]
    [SerializeField] private float _summonMaxOffset = 2.8f;

    private readonly Dictionary<InventoryItem, Weapon> _weapons = new Dictionary<InventoryItem, Weapon>();
    private readonly List<Weapon> _summons = new List<Weapon>(); // 장착 순서 = 배치 순서
    private readonly Dictionary<InventoryItem, int> _artifacts = new Dictionary<InventoryItem, int>(); // 격자 아티팩트 → 적용 중 등급
    private PlayerStats _stats;

    public int Count => _weapons.Count;
    public IEnumerable<InventoryItem> EquippedItems => _weapons.Keys; // HUD 무기 칸 표시용

    /// <summary>본체 무기(소환체 제외) 발사 시 호출</summary>
    public event System.Action WeaponFired;

    private void Awake()
    {
        _stats = GetComponent<PlayerStats>();
    }

    /// <summary>장착 - 아티팩트는 효과 적용, 소환 무기는 소환체 생성, 나머지는 Weapon 컴포넌트 추가</summary>
    public void Equip(InventoryItem item)
    {
        if (item.Data is ArtifactData artifact)
        {
            if (_artifacts.ContainsKey(item)) return;

            _artifacts.Add(item, item.Grade);
            _stats.ApplyArtifact(artifact, item.Grade);
            HealUngrantedMaxHp(item, artifact);
            return;
        }
        if (_weapons.ContainsKey(item)) return;

        WeaponData data = (WeaponData)item.Data;
        bool isSummon = data.AttackType == AttackType.Summon;
        TargetFinder targetFinder = isSummon ? null : gameObject.AddComponent<TargetFinder>(); // 본체 무기 전용 탐색기 - 사거리 공유 방지, Weapon 보다 먼저(RequireComponent 자동 추가 방지)
        Weapon weapon = isSummon ? Instantiate(_summonPrefab, transform) : gameObject.AddComponent<Weapon>();
        weapon.Setup(data, GetGradeMultiplier(item.Grade), isSummon ? _summonPool : _pool, _stats, targetFinder);
        _weapons.Add(item, weapon);
        AssignPhases();

        if (isSummon)
        {
            _summons.Add(weapon);
            ArrangeSummons();
        }
        else
        {
            weapon.Fired += OnWeaponFired;
        }
    }

    private void OnWeaponFired() => WeaponFired?.Invoke();

    /// <summary>해제 - 아티팩트는 효과 제거, Weapon 컴포넌트 또는 소환체 제거</summary>
    public void Unequip(InventoryItem item)
    {
        if (_artifacts.TryGetValue(item, out int appliedGrade))
        {
            _artifacts.Remove(item);
            _stats.ApplyArtifact((ArtifactData)item.Data, -appliedGrade);
            return;
        }
        if (!_weapons.TryGetValue(item, out Weapon weapon)) return;

        _weapons.Remove(item);
        AssignPhases();
        if (_summons.Remove(weapon))
        {
            Destroy(weapon.gameObject);
            ArrangeSummons();
        }
        else
        {
            Destroy(weapon);
            Destroy(weapon.TargetFinder);
        }
    }

    /// <summary>머지 후 등급 배율 갱신 - 아티팩트는 오른 등급만큼 효과 추가</summary>
    public void UpdateGrade(InventoryItem item)
    {
        if (_weapons.TryGetValue(item, out Weapon weapon)) weapon.SetGradeMultiplier(GetGradeMultiplier(item.Grade));

        if (_artifacts.TryGetValue(item, out int appliedGrade))
        {
            ArtifactData artifact = (ArtifactData)item.Data;
            _artifacts[item] = item.Grade;
            _stats.ApplyArtifact(artifact, item.Grade - appliedGrade);
            HealUngrantedMaxHp(item, artifact);
        }
    }

    // 베개 회복 규칙 - 머지 계보가 한 번도 회복하지 않은 최대 HP 만 회복
    // 격자 진입·격자 안 머지 때 (현재 보너스 - HealedMaxHp) 회복, 격자 이탈은 최대 HP 만 감소(현재 HP 제한)
    private void HealUngrantedMaxHp(InventoryItem item, ArtifactData artifact)
    {
        if (artifact.EffectType != StatType.MaxHp) return;

        int bonus = Mathf.RoundToInt(artifact.GetValue(item.Grade));
        if (bonus <= item.HealedMaxHp) return;

        _stats.Heal(bonus - item.HealedMaxHp);
        item.HealedMaxHp = bonus;
    }

    /// <summary>해당 타입 무기 장착 여부 - 태그 특성 조건</summary>
    public bool HasWeaponOfType(AttackType type)
    {
        foreach (Weapon weapon in _weapons.Values)
            if (weapon.Data.AttackType == type) return true;
        return false;
    }

    public float GetGradeMultiplier(int grade) => _gradeDamageMultipliers[grade - 1];

    /// <summary>무기마다 박자 위치 균등 분배 - 첫 발 동시 발사 방지</summary>
    private void AssignPhases()
    {
        int index = 0;
        foreach (Weapon weapon in _weapons.Values)
        {
            weapon.SetPhase(index / (float)_weapons.Count);
            index++;
        }
    }

    /// <summary>소환체 배치 - 왼 → 오 → 왼 … 바깥쪽, 한계 초과 시 간격 축소</summary>
    private void ArrangeSummons()
    {
        int outermostRank = (_summons.Count + 1) / 2;
        float spacing = Mathf.Min(_summonSpacing, _summonMaxOffset / Mathf.Max(1, outermostRank));

        for (int i = 0; i < _summons.Count; i++)
        {
            float side = i % 2 == 0 ? -1f : 1f;
            int rank = i / 2 + 1;
            _summons[i].transform.localPosition = new Vector3(side * rank * spacing, 0f, 0f); // 비비 발밑 높이
        }
    }
}

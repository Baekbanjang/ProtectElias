using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>레벨업 특성 선택 팝업 - 카드 3장(메타 강화 시 4장) 롤/선택/새로고침/확정, 다중 레벨업 큐잉</summary>
public class LevelUpPanel : MonoBehaviour
{
    private const int BaseCardCount = 3;
    private static readonly int[] CategoryWeights = { 6, 10, 12, 3 }; // 핵심·보통·유틸·고유

    /// <summary>레벨 구간별 등급 확률 (합 = 1)</summary>
    [Serializable]
    private struct GradeOddsRow
    {
        public int minLevel;
        [Range(0f, 1f)] public float grade1Chance;
        [Range(0f, 1f)] public float grade2Chance;
        [Range(0f, 1f)] public float grade3Chance;
    }

    [Header("참조")]
    [SerializeField] private PlayerLevel _playerLevel;
    [SerializeField] private PlayerStats _playerStats;

    [Tooltip("닫을 때 현재 배속으로 복귀")]
    [SerializeField] private GameSpeed _gameSpeed;

    [Tooltip("특성 후보 풀 - 인스펙터에서 할당")]
    [SerializeField] private TraitData[] _traitPool;

    [Tooltip("격자 배치 무기 - 태그 조건 판정용")]
    [SerializeField] private WeaponLoadout _loadout;

    [Tooltip("카드 4장 - 4번째는 메타 강화 선택지 추가 시만 사용")]
    [SerializeField] private TraitCard[] _cards;

    [Tooltip("창 - 추가 카드만큼 세로 확장")]
    [SerializeField] private RectTransform _window;

    [Tooltip("카드 1장 추가 시 창 확장 높이")]
    [SerializeField] private float _extraCardHeight = 190f;

    [SerializeField] private Button _selectButton;
    [SerializeField] private Button _refreshButton;
    [SerializeField] private TMP_Text _refreshCountText;

    [Tooltip("한 판당 새로고침 가능 횟수")]
    [SerializeField] private int _maxRefreshes = 3;

    [Header("메타 강화")]
    [SerializeField] private MetaUpgradeData _metaRefresh; // 새로고침 +N
    [SerializeField] private MetaUpgradeData _metaChoice;  // 선택지 +N

    [Header("등급 확률 (레벨 구간별, minLevel 오름차순)")]
    [SerializeField] private GradeOddsRow[] _gradeOdds =
    {
        new GradeOddsRow { minLevel = 1, grade1Chance = 0.85f, grade2Chance = 0.14f, grade3Chance = 0.01f },
        new GradeOddsRow { minLevel = 5, grade1Chance = 0.72f, grade2Chance = 0.24f, grade3Chance = 0.04f },
        new GradeOddsRow { minLevel = 10, grade1Chance = 0.60f, grade2Chance = 0.30f, grade3Chance = 0.10f },
        new GradeOddsRow { minLevel = 20, grade1Chance = 0.50f, grade2Chance = 0.35f, grade3Chance = 0.15f },
    };

    private int _cardCount;
    private int _remainingRefreshes;
    private int _queuedRounds;
    private int _selectedIndex = -1;
    private bool _isOpen;

    /// <summary>웨이브 종료 동시 발생 대기용</summary>
    public bool IsOpen => _isOpen;

    // 고유 특성 선택 기록 - 한 판 1회만 후보
    private readonly HashSet<TraitData> _pickedUniqueTraits = new HashSet<TraitData>();

    private void Awake()
    {
        _remainingRefreshes = _maxRefreshes + _metaRefresh.TotalCount;
        SetupCardCount();
        _selectButton.onClick.AddListener(Confirm);
        _refreshButton.onClick.AddListener(Refresh);
    }

    /// <summary>키보드 단축키 - 1~4 선택, Space/Enter 확정</summary>
    private void Update()
    {
        if (!_isOpen) return;

        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb.digit1Key.wasPressedThisFrame) SelectCard(0);
        else if (kb.digit2Key.wasPressedThisFrame) SelectCard(1);
        else if (kb.digit3Key.wasPressedThisFrame) SelectCard(2);
        else if (kb.digit4Key.wasPressedThisFrame) SelectCard(3);

        if ((kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame) && _selectButton.interactable)
            Confirm();
    }

    /// <summary>레벨업 팝업 열기 - PlayerLevel 의 레벨업 이벤트로 연결</summary>
    public void Open()
    {
        if (_isOpen) { _queuedRounds++; return; }

        _isOpen = true;
        Time.timeScale = 0f;
        gameObject.SetActive(true);
        RollNewCards();
    }

    /// <summary>새로고침 - 카드 다시 뽑기, 횟수 1 차감</summary>
    public void Refresh()
    {
        if (_remainingRefreshes <= 0) return;

        _remainingRefreshes--;
        RollNewCards();
    }

    /// <summary>선택 확정 - 스탯 적용 후 닫거나 다음 큐 진행</summary>
    public void Confirm()
    {
        if (_selectedIndex < 0) return;

        TraitCard card = _cards[_selectedIndex];
        _playerStats.ApplyTrait(card.Trait, card.Grade);
        if (card.Trait.IsUnique) _pickedUniqueTraits.Add(card.Trait);

        if (_queuedRounds > 0)
        {
            _queuedRounds--;
            RollNewCards();
        }
        else
        {
            Close();
        }
    }

    private void SelectCard(int index)
    {
        if (index < 0 || index >= _cardCount) return;
        OnCardClicked(_cards[index]);
    }

    private void OnCardClicked(TraitCard card)
    {
        _selectedIndex = Array.IndexOf(_cards, card);
        for (int i = 0; i < _cards.Length; i++) _cards[i].SetHighlight(i == _selectedIndex);
        _selectButton.interactable = true;
    }

    private void RollNewCards()
    {
        List<TraitData> traits = PickDistinctTraits(_cardCount);
        _selectedIndex = -1;
        _selectButton.interactable = false;

        for (int i = 0; i < _cardCount; i++)
        {
            int grade = traits[i].IsUnique ? 1 : RollGrade(_playerLevel.Level);
            _cards[i].Setup(i + 1, traits[i], grade, OnCardClicked);
        }

        UpdateRefreshUi();
    }

    /// <summary>카드 수 = 기본 + 메타 강화, 남는 카드 숨김, 추가 카드만큼 창 확장</summary>
    private void SetupCardCount()
    {
        _cardCount = Mathf.Min(_cards.Length, BaseCardCount + _metaChoice.TotalCount);
        for (int i = 0; i < _cards.Length; i++) _cards[i].gameObject.SetActive(i < _cardCount);
        _window.sizeDelta += Vector2.up * (_extraCardHeight * (_cardCount - BaseCardCount));
    }

    private void UpdateRefreshUi()
    {
        _refreshCountText.text = $"남은 횟수: {_remainingRefreshes}";
        _refreshButton.interactable = _remainingRefreshes > 0;
    }

    private List<TraitData> PickDistinctTraits(int count)
    {
        List<TraitData> pool = BuildAvailablePool();
        List<TraitData> picked = new List<TraitData>();
        for (int i = 0; i < count && pool.Count > 0; i++)
        {
            TraitData trait = PickWeightedTrait(pool);
            picked.Add(trait);
            pool.Remove(trait);
        }
        return picked;
    }

    /// <summary>분류 가중치 → 서브 종류 가중치 → 보유 타입 중 균등 추첨</summary>
    private TraitData PickWeightedTrait(List<TraitData> pool)
    {
        bool[] categories = new bool[CategoryWeights.Length];
        foreach (TraitData trait in pool) categories[GetCategory(trait)] = true;

        int total = 0;
        for (int i = 0; i < categories.Length; i++)
            if (categories[i]) total += CategoryWeights[i];
        int roll = UnityEngine.Random.Range(0, total);
        int category = 0;
        for (int i = 0; i < categories.Length; i++)
        {
            if (!categories[i]) continue;
            roll -= CategoryWeights[i];
            if (roll < 0) { category = i; break; }
        }

        List<StatType> subTypes = new List<StatType>();
        foreach (TraitData trait in pool)
            if (GetCategory(trait) == category && !subTypes.Contains(trait.StatType)) subTypes.Add(trait.StatType);

        total = 0;
        foreach (StatType type in subTypes) total += type == StatType.LifeSteal ? 3 : 10;
        roll = UnityEngine.Random.Range(0, total);
        StatType selectedType = subTypes[0];
        foreach (StatType type in subTypes)
        {
            roll -= type == StatType.LifeSteal ? 3 : 10;
            if (roll < 0) { selectedType = type; break; }
        }

        List<TraitData> candidates = new List<TraitData>();
        foreach (TraitData trait in pool)
            if (GetCategory(trait) == category && trait.StatType == selectedType) candidates.Add(trait);
        return candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }

    private static int GetCategory(TraitData trait)
    {
        if (trait.IsUnique) return 3;
        switch (trait.StatType)
        {
            case StatType.Attack:
            case StatType.AttackSpeed:
            case StatType.CritChance: return 0;
            case StatType.CritDamage:
            case StatType.TypeDamage:
            case StatType.TypeReload:
            case StatType.MaxHp:
            case StatType.LifeSteal: return 1;
            default: return 2;
        }
    }

    /// <summary>후보 필터 - 고유는 1회 소진, 태그는 보유 무기 타입만</summary>
    private List<TraitData> BuildAvailablePool()
    {
        List<TraitData> available = new List<TraitData>();
        foreach (TraitData trait in _traitPool)
        {
            if (trait.StatType == StatType.Multishot) continue;
            if (trait.StatType == StatType.Attack && _playerStats.HasReachedAttackCap) continue;
            if (trait.StatType == StatType.AttackSpeed && !HasAvailableCooldownType()) continue;
            if (trait.StatType == StatType.CritChance && _playerStats.HasReachedCritChanceCap) continue;
            if (trait.StatType == StatType.LifeSteal && _playerStats.HasReachedLifeStealCap) continue;
            if (trait.IsUnique && _pickedUniqueTraits.Contains(trait)) continue;
            if (trait.HasAttackTypeCondition && !_loadout.HasWeaponOfType(trait.RequiredAttackType)) continue;
            if (trait.StatType == StatType.TypeReload && _playerStats.HasReachedCooldownCap(trait.RequiredAttackType)) continue;
            available.Add(trait);
        }
        return available;
    }

    private bool HasAvailableCooldownType()
    {
        bool hasWeapon = false;
        foreach (AttackType type in Enum.GetValues(typeof(AttackType)))
        {
            if (!_loadout.HasWeaponOfType(type)) continue;
            hasWeapon = true;
            if (!_playerStats.HasReachedCooldownCap(type)) return true;
        }
        return !hasWeapon && !_playerStats.HasReachedAttackSpeedCap;
    }

    /// <summary>레벨에 맞는 확률로 등급(1~3) 롤</summary>
    private int RollGrade(int level)
    {
        GradeOddsRow odds = _gradeOdds[0];
        foreach (GradeOddsRow row in _gradeOdds)
            if (level >= row.minLevel) odds = row;

        float roll = UnityEngine.Random.value;
        if (roll < odds.grade3Chance) return 3;
        if (roll < odds.grade3Chance + odds.grade2Chance) return 2;
        return 1;
    }

    private void Close()
    {
        _isOpen = false;
        gameObject.SetActive(false);

        // 게임오버 중엔 정지 유지
        Health playerHealth = _playerStats.GetComponent<Health>();
        if (playerHealth != null && playerHealth.IsDead) return;
        _gameSpeed.ResumeTime();
    }
}

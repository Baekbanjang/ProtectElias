using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>상점 - 무기 영역 뽑기·새로고침·제외 모드, 전투 시작으로 닫기</summary>
public class ShopPanel : MonoBehaviour
{
    private const int MaxGrade = 4;

    /// <summary>웨이브 구간별 무기 등급 확률 (4등급, 합 = 1)</summary>
    [Serializable]
    private struct GradeOddsRow
    {
        public int minWave;
        [Range(0f, 1f)] public float grade1Chance;
        [Range(0f, 1f)] public float grade2Chance;
        [Range(0f, 1f)] public float grade3Chance;
        [Range(0f, 1f)] public float grade4Chance;
    }

    [Tooltip("무기 영역 뽑기 풀")]
    [SerializeField] private WeaponData[] _weaponPool;

    [Tooltip("아티팩트 뽑기 풀")]
    [SerializeField] private ArtifactData[] _artifactPool;

    [Tooltip("칸마다 무기 대신 아티팩트가 나올 확률")]
    [SerializeField, Range(0f, 1f)] private float _artifactChance = 0.15f;

    [Tooltip("원작 등급별 출현 가중치 (일반·고급·희귀·전설) - 같은 등급 아티팩트끼리 나눔")]
    [SerializeField] private float[] _artifactRarityWeights = { 40f, 30f, 20f, 10f };

    [SerializeField] private InventoryBoard _board;

    [Tooltip("한 번에 뽑는 새 무기 수")]
    [SerializeField] private int _offerCount = 3;

    [Header("버튼")]
    [SerializeField] private Button _startBattleButton;
    [SerializeField] private TMP_Text _startBattleLabel;
    [SerializeField] private Button _refreshButton;
    [SerializeField] private TMP_Text _refreshLabel;
    [SerializeField] private Button _excludeButton;
    [SerializeField] private TMP_Text _excludeLabel;

    [Header("제외 모드")]
    [Tooltip("「제외 된 무기」 창 - 드롭 영역")]
    [SerializeField] private RectTransform _excludeWindow;
    [SerializeField] private RectTransform _excludedList;   // 제외한 무기 목록 부모
    [SerializeField] private GameObject _weaponAreaFrame;   // 무기 영역 강조 테두리
    [SerializeField] private Color _excludableFrameColor = new Color(0.3f, 1f, 0.3f); // 제외 가능 무기 테두리
    [SerializeField] private Color _cancelButtonColor = new Color(0.45f, 0.08f, 0.08f); // 취소 버튼 틴트 - 전투 시작 스프라이트 재사용

    [Header("제한 횟수 (판 전체, 임시)")]
    [SerializeField] private int _maxRefreshes = 5;
    [SerializeField] private int _maxExcludes = 1;

    [Header("메타 강화")]
    [SerializeField] private MetaUpgradeData _metaRefresh;   // 무료 새로고침 +N
    [SerializeField] private MetaUpgradeData _metaExclude;   // 제외 횟수 +N
    [SerializeField] private MetaUpgradeData _metaHighGrade; // 뽑은 등급 +1 확률

    [Header("골드 (임시)")]
    [SerializeField] private PlayerGold _gold;
    [SerializeField] private TMP_Text _goldText;                           // 상점 현재 골드
    [SerializeField] private int _refreshBaseCost = 10;                    // 무료 소진 후 첫 비용
    [SerializeField] private int _refreshCostStep = 5;                     // 유료 새로고침마다 비용 증가
    [SerializeField] private Color _costColor = new Color(0.75f, 0.3f, 0.02f); // 비용 숫자 진한 주황 - 크림 버튼 위 가독성

    [Tooltip("HUD 무기 칸 - 전투 시작 시 갱신")]
    [SerializeField] private WeaponSummaryView _weaponSummary;

    [Tooltip("닫을 때 현재 배속으로 복귀")]
    [SerializeField] private GameSpeed _gameSpeed;

    [Header("등급 확률 (웨이브 구간별, minWave 오름차순)")]
    [SerializeField] private GradeOddsRow[] _gradeOdds =
    {
        new GradeOddsRow { minWave = 1, grade1Chance = 0.95f, grade2Chance = 0.05f, grade3Chance = 0f, grade4Chance = 0f },
        new GradeOddsRow { minWave = 10, grade1Chance = 0.80f, grade2Chance = 0.18f, grade3Chance = 0.02f, grade4Chance = 0f },
        new GradeOddsRow { minWave = 25, grade1Chance = 0.60f, grade2Chance = 0.30f, grade3Chance = 0.09f, grade4Chance = 0.01f },
        new GradeOddsRow { minWave = 45, grade1Chance = 0.40f, grade2Chance = 0.38f, grade3Chance = 0.18f, grade4Chance = 0.04f },
        new GradeOddsRow { minWave = 70, grade1Chance = 0.25f, grade2Chance = 0.35f, grade3Chance = 0.28f, grade4Chance = 0.12f },
    };

    private readonly HashSet<GridItemData> _excludedWeapons = new HashSet<GridItemData>(); // 이번 판 출현 금지 - 무기·아티팩트
    private bool _isOpen;
    private int _waveNumber;
    private int _remainingRefreshes;
    private int _remainingExcludes;
    private int _paidRefreshes;

    private int RefreshCost => _refreshBaseCost + _refreshCostStep * _paidRefreshes;

    /// <summary>웨이브 진행 대기용</summary>
    public bool IsOpen => _isOpen;

    /// <summary>제외 모드 - 트레이 무기 드래그로 제외만 가능</summary>
    public bool IsExcludeMode { get; private set; }

    private void Awake()
    {
        _remainingRefreshes = _maxRefreshes + _metaRefresh.TotalCount;
        _remainingExcludes = _maxExcludes + _metaExclude.TotalCount;
        _startBattleButton.onClick.AddListener(OnStartBattleClicked);
        _refreshButton.onClick.AddListener(Refresh);
        _excludeButton.onClick.AddListener(EnterExcludeMode);
        ApplyExcludeMode();
    }

    /// <summary>상점 열기 - 무기 영역 뽑기 후 정지</summary>
    public void Open(int waveNumber)
    {
        _isOpen = true;
        _waveNumber = waveNumber;
        Time.timeScale = 0f;
        gameObject.SetActive(true);
        RollWeapons();
        UpdateButtons();
    }

    /// <summary>새로고침 - 무기 영역 다시 뽑기, 무료 횟수 먼저 차감 후 골드 소비(비용 증가)</summary>
    public void Refresh()
    {
        if (_remainingRefreshes > 0) _remainingRefreshes--;
        else if (_gold.TrySpend(RefreshCost)) _paidRefreshes++;
        else return;

        RollWeapons();
        UpdateButtons();
    }

    /// <summary>「제외 된 무기」 창 드롭 판정</summary>
    public bool IsExcludeDropTarget(Vector2 screenPosition, Camera eventCamera)
    {
        return IsExcludeMode && _remainingExcludes > 0
               && RectTransformUtility.RectangleContainsScreenPoint(_excludeWindow, screenPosition, eventCamera);
    }

    /// <summary>무기 제외 - 종류를 풀에서 영구 제거, 트레이 같은 종류 새 무기 제거, 목록 추가. 모드는 「취소」로만 종료</summary>
    public void ExcludeWeapon(GridItemData data)
    {
        if (!IsExcludeMode || _remainingExcludes <= 0) return;

        _remainingExcludes--;
        _excludedWeapons.Add(data);
        _board.RemoveWeaponAreaItems(data);
        AddExcludedIcon(data);
        UpdateButtons();
        ApplyExcludeMode();
        Debug.Log($"무기 제외 - {data.DisplayName}, 남은 횟수 {_remainingExcludes}");
    }

    /// <summary>제외 모드 진입 - 남은 횟수 있을 때</summary>
    public void EnterExcludeMode()
    {
        if (IsExcludeMode || _remainingExcludes <= 0) return;

        _board.CancelDrag();
        IsExcludeMode = true;
        ApplyExcludeMode();
    }

    /// <summary>제외 모드 종료 - 원래 화면</summary>
    public void ExitExcludeMode()
    {
        if (!IsExcludeMode) return;

        _board.CancelDrag();
        IsExcludeMode = false;
        ApplyExcludeMode();
    }

    /// <summary>하단 버튼 - 제외 모드면 취소, 아니면 전투 시작</summary>
    private void OnStartBattleClicked()
    {
        if (IsExcludeMode) ExitExcludeMode();
        else Close();
    }

    /// <summary>전투 시작 - 드래그 취소, 무기 영역 비우고 HUD 무기 칸 갱신 후 현재 배속으로 재개</summary>
    private void Close()
    {
        ExitExcludeMode();
        _board.CancelDrag();
        _board.ClearWeaponArea();
        _weaponSummary.Refresh();
        _isOpen = false;
        gameObject.SetActive(false);
        _gameSpeed.ResumeTime();
    }

    /// <summary>제외 모드 표시 전환 - 창·강조 테두리·새로고침·하단 버튼 라벨, 트레이 테두리는 남은 횟수 있을 때만</summary>
    private void ApplyExcludeMode()
    {
        _excludeWindow.gameObject.SetActive(IsExcludeMode);
        _weaponAreaFrame.SetActive(IsExcludeMode);
        _refreshButton.gameObject.SetActive(!IsExcludeMode);
        _startBattleLabel.text = IsExcludeMode ? "취소" : "전투 시작";
        _startBattleButton.image.color = IsExcludeMode ? _cancelButtonColor : Color.white;
        _board.SetTrayFrames(IsExcludeMode && _remainingExcludes > 0, _excludableFrameColor);
    }

    /// <summary>「제외 된 무기」 목록에 축소 아이콘 추가</summary>
    private void AddExcludedIcon(GridItemData data)
    {
        if (data.Icon == null) return;

        Image icon = new GameObject(data.name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        icon.rectTransform.SetParent(_excludedList, false);
        icon.rectTransform.sizeDelta = data.Icon.rect.size * (InventoryGridView.CellSize / 32f * WeaponTrayView.CompactScale); // 트레이 축소 배율
        icon.sprite = data.Icon;
        icon.raycastTarget = false;
    }

    /// <summary>버튼 라벨·활성, 현재 골드 - 새로고침은 무료 남으면 횟수, 소진 후 주황 비용</summary>
    private void UpdateButtons()
    {
        bool isFreeRefresh = _remainingRefreshes > 0;
        string refreshInfo = isFreeRefresh
            ? $"남은 횟수 : {_remainingRefreshes}"
            : $"비용 : <color=#{ColorUtility.ToHtmlStringRGB(_costColor)}>{RefreshCost}</color>";
        _refreshLabel.text = $"새로고침\n<size=70%><color=#6E5A46>{refreshInfo}</color></size>"; // 보조 줄 = 연한 코코아
        _refreshButton.interactable = isFreeRefresh || _gold.Gold >= RefreshCost;
        _goldText.text = _gold.Gold.ToString();
        _excludeLabel.text = $"아이템 제외\n<size=70%><color=#6E5A46>남은 횟수 : {_remainingExcludes}</color></size>";
        _excludeButton.interactable = _remainingExcludes > 0;
    }

    /// <summary>종류 중복 없이 셔플 추출(제외 종류 빼고), 칸마다 확률로 아티팩트, 등급은 무기·아티팩트 공통 독립 롤, 무기 영역 빈 자리에 자동 배치</summary>
    private void RollWeapons()
    {
        _board.ClearWeaponArea(); // 새로고침 = 트레이 전부 교체

        List<WeaponData> pool = new List<WeaponData>();
        foreach (WeaponData weapon in _weaponPool)
            if (!_excludedWeapons.Contains(weapon)) pool.Add(weapon);

        List<ArtifactData> artifactPool = new List<ArtifactData>();
        foreach (ArtifactData artifact in _artifactPool)
            if (!_excludedWeapons.Contains(artifact)) artifactPool.Add(artifact);

        for (int i = 0; i < _offerCount && (pool.Count > 0 || artifactPool.Count > 0); i++)
        {
            GridItemData data;
            bool isArtifact = artifactPool.Count > 0 && (pool.Count == 0 || UnityEngine.Random.value < _artifactChance);
            if (isArtifact)
            {
                ArtifactData artifact = PickArtifact(artifactPool);
                artifactPool.Remove(artifact);
                data = artifact;
            }
            else
            {
                int poolIndex = UnityEngine.Random.Range(0, pool.Count);
                data = pool[poolIndex];
                pool.RemoveAt(poolIndex);
            }

            int grade = RollGrade(_waveNumber);
            if (grade < MaxGrade && UnityEngine.Random.value < _metaHighGrade.TotalValue) grade++; // 메타 강화 - 등급 +1

            if (!_board.TryAddWeaponAreaItem(new InventoryItem(data, grade)))
                Debug.Log($"무기 영역 자리 부족 - {data.DisplayName} 생략");
        }
    }

    /// <summary>아티팩트 1개 - 원작 등급 가중치를 같은 등급 수로 나눠 추첨</summary>
    private ArtifactData PickArtifact(List<ArtifactData> candidates)
    {
        int[] rarityCounts = new int[_artifactRarityWeights.Length];
        foreach (ArtifactData artifact in candidates) rarityCounts[(int)artifact.Rarity - 1]++;

        float total = 0f;
        foreach (ArtifactData artifact in candidates) total += GetArtifactWeight(artifact, rarityCounts);

        float roll = UnityEngine.Random.value * total;
        foreach (ArtifactData artifact in candidates)
        {
            roll -= GetArtifactWeight(artifact, rarityCounts);
            if (roll < 0f) return artifact;
        }
        return candidates[candidates.Count - 1]; // 부동소수 오차 대비
    }

    private float GetArtifactWeight(ArtifactData artifact, int[] rarityCounts)
    {
        int rarity = (int)artifact.Rarity - 1;
        return _artifactRarityWeights[rarity] / rarityCounts[rarity];
    }

    /// <summary>웨이브 구간에 맞는 확률로 등급(1~4) 롤</summary>
    private int RollGrade(int waveNumber)
    {
        GradeOddsRow odds = _gradeOdds[0];
        foreach (GradeOddsRow row in _gradeOdds)
            if (waveNumber >= row.minWave) odds = row;

        float roll = UnityEngine.Random.value;
        if (roll < odds.grade4Chance) return 4;
        if (roll < odds.grade4Chance + odds.grade3Chance) return 3;
        if (roll < odds.grade4Chance + odds.grade3Chance + odds.grade2Chance) return 2;
        return 1;
    }
}

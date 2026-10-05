using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>메타 강화 창 - 항목 카드 격자, 선택 항목 상세·비용, 강화 구매, 전체 초기화(환불)</summary>
public class MetaUpgradePanel : MonoBehaviour
{
    [SerializeField] private MetaUpgradeData[] _upgrades;

    [Tooltip("카드 원본 - 항목 수만큼 복제")]
    [SerializeField] private MetaUpgradeCard _cardTemplate;

    [Tooltip("타이틀 코인 갱신")]
    [SerializeField] private TitleMenu _titleMenu;

    [SerializeField] private TMP_Text _goldText;

    [Header("상세")]
    [SerializeField] private Image _detailIcon;
    [SerializeField] private TMP_Text _detailNameText;
    [SerializeField] private TMP_Text _detailLevelText;
    [SerializeField] private TMP_Text _detailDescriptionText;
    [SerializeField] private TMP_Text _costText;
    [SerializeField] private Color _lackGoldColor = new Color(0.8f, 0.2f, 0.2f); // 골드 부족 비용 색

    [Header("버튼")]
    [SerializeField] private Button _upgradeButton;
    [SerializeField] private Button _resetButton;

    [Header("초기화 확인")]
    [SerializeField] private GameObject _resetConfirm;
    [SerializeField] private TMP_Text _resetConfirmText;
    [SerializeField] private Button _resetConfirmButton;
    [SerializeField] private Button _resetCancelButton;

    private readonly List<MetaUpgradeCard> _cards = new List<MetaUpgradeCard>();
    private int _selectedIndex;
    private Color _costColor;

    public bool IsOpen => gameObject.activeSelf;

    /// <summary>항목 수만큼 카드 생성, 버튼 연결</summary>
    private void Awake()
    {
        _costColor = _costText.color;
        foreach (MetaUpgradeData upgrade in _upgrades)
        {
            MetaUpgradeCard card = Instantiate(_cardTemplate, _cardTemplate.transform.parent);
            card.name = upgrade.name;
            card.gameObject.SetActive(true);
            card.Setup(upgrade, OnCardClicked);
            _cards.Add(card);
        }
        _cardTemplate.gameObject.SetActive(false);

        _upgradeButton.onClick.AddListener(Upgrade);
        _resetButton.onClick.AddListener(OpenResetConfirm);
        _resetConfirmButton.onClick.AddListener(ResetAll);
        _resetCancelButton.onClick.AddListener(() => _resetConfirm.SetActive(false));
    }

    /// <summary>첫 항목 선택 상태로 열기</summary>
    public void Open()
    {
        gameObject.SetActive(true);
        _resetConfirm.SetActive(false);
        _selectedIndex = 0;
        Refresh();
    }

    public void Close() => gameObject.SetActive(false);

    /// <summary>선택 항목 다음 레벨 구매</summary>
    public void Upgrade()
    {
        if (_upgrades[_selectedIndex].TryUpgrade()) Refresh();
    }

    /// <summary>전체 초기화 - 쓴 골드 전부 환불, 레벨 0</summary>
    public void ResetAll()
    {
        foreach (MetaUpgradeData upgrade in _upgrades) upgrade.ResetLevel();
        _resetConfirm.SetActive(false);
        Refresh();
    }

    private void OpenResetConfirm()
    {
        _resetConfirmText.text = $"모든 강화를 초기화할까요?\n사용한 골드 {GetTotalSpent():N0} 전부 돌려받습니다";
        _resetConfirm.SetActive(true);
    }

    private void OnCardClicked(MetaUpgradeCard card)
    {
        _selectedIndex = _cards.IndexOf(card);
        Refresh();
    }

    /// <summary>골드·카드·상세·비용·버튼 갱신, 타이틀 코인도 갱신</summary>
    private void Refresh()
    {
        MetaUpgradeData selected = _upgrades[_selectedIndex];
        int gold = SaveData.MetaGold;
        _goldText.text = gold.ToString("N0");
        for (int i = 0; i < _cards.Count; i++) _cards[i].Refresh(i == _selectedIndex);

        _detailIcon.sprite = selected.Icon;
        _detailIcon.enabled = selected.Icon != null;
        _detailNameText.text = selected.DisplayName;
        _detailLevelText.text = $"LV. {selected.Level} / {selected.MaxLevel}";
        _detailDescriptionText.text = selected.Description;

        bool canAfford = gold >= selected.NextCost;
        _costText.text = selected.IsMaxLevel ? "MAX" : selected.NextCost.ToString("N0");
        _costText.color = selected.IsMaxLevel || canAfford ? _costColor : _lackGoldColor;
        _upgradeButton.interactable = !selected.IsMaxLevel && canAfford;
        _resetButton.interactable = GetTotalSpent() > 0;

        _titleMenu.RefreshGold();
    }

    private int GetTotalSpent()
    {
        int total = 0;
        foreach (MetaUpgradeData upgrade in _upgrades) total += upgrade.SpentGold;
        return total;
    }
}

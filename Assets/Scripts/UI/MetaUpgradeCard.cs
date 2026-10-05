using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>메타 강화 카드 1장 - 이름·아이콘·레벨 표시, 선택 시 색 강조</summary>
public class MetaUpgradeCard : MonoBehaviour
{
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private Image _iconImage;
    [SerializeField] private TMP_Text _levelText;
    [SerializeField] private Image _background;
    [SerializeField] private Button _button;
    [SerializeField] private Color _selectedColor = new Color(1f, 0.55f, 0.45f); // 선택 강조 - 코랄

    private Action<MetaUpgradeCard> _onClicked;

    public MetaUpgradeData Upgrade { get; private set; }

    private void Awake()
    {
        _button.onClick.AddListener(() => _onClicked?.Invoke(this));
    }

    /// <summary>항목 지정</summary>
    public void Setup(MetaUpgradeData upgrade, Action<MetaUpgradeCard> onClicked)
    {
        Upgrade = upgrade;
        _onClicked = onClicked;
        _nameText.text = upgrade.DisplayName;
        _iconImage.sprite = upgrade.Icon;
        _iconImage.enabled = upgrade.Icon != null;
    }

    /// <summary>레벨·선택 표시 갱신</summary>
    public void Refresh(bool isSelected)
    {
        _levelText.text = $"LV. {Upgrade.Level}";
        _background.color = isSelected ? _selectedColor : Color.white;
    }
}

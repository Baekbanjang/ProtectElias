using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>레벨업 카드 1장 - 번호/아이콘/제목/설명 표시, 클릭 시 선택 콜백</summary>
public class TraitCard : MonoBehaviour
{
    [SerializeField] private TMP_Text _numberText;
    [SerializeField] private Image _iconImage;
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _descriptionText;
    [SerializeField] private GameObject _highlight;
    [SerializeField] private Button _button;

    [Tooltip("아이콘 뒤 등급 배경 - 무기 등급색과 같음")]
    [SerializeField] private Image _gradeBackground;

    [Tooltip("등급 배경색 (I · II · III · 고유)")]
    [SerializeField] private Color[] _gradeColors =
    {
        new Color(0.6f, 0.6f, 0.6f),
        new Color(0.3f, 0.8f, 0.3f),
        new Color(0.3f, 0.6f, 1f),
        new Color(0.65f, 0.35f, 0.9f),
    };

    private Action<TraitCard> _onClicked;

    public TraitData Trait { get; private set; }
    public int Grade { get; private set; }

    private void Awake()
    {
        _button.onClick.AddListener(() => _onClicked?.Invoke(this));
    }

    /// <summary>카드 내용 갱신</summary>
    public void Setup(int number, TraitData trait, int grade, Action<TraitCard> onClicked)
    {
        Trait = trait;
        Grade = grade;
        _onClicked = onClicked;

        _numberText.text = number.ToString();
        _titleText.text = trait.GetTitle(grade);
        _descriptionText.text = trait.GetDescription(grade);

        bool hasIcon = trait.Icon != null;
        _iconImage.gameObject.SetActive(hasIcon);
        if (hasIcon) _iconImage.sprite = trait.Icon;

        if (_gradeBackground != null)
        {
            int colorIndex = trait.IsUnique ? _gradeColors.Length - 1 : Mathf.Clamp(grade - 1, 0, _gradeColors.Length - 2);
            _gradeBackground.color = _gradeColors[colorIndex];
            if (_button.targetGraphic is Image cardBackground)
                cardBackground.color = Color.Lerp(new Color(0.88f, 0.94f, 0.98f), _gradeColors[colorIndex], 0.18f);
        }

        SetHighlight(false);
    }

    /// <summary>선택 표시 갱신</summary>
    public void SetHighlight(bool on) => _highlight.SetActive(on);
}

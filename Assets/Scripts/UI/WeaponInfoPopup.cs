using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>무기 정보 창 - 아이콘·공격 방식·이름·설명·스테이터스·스킬, X·ESC·바깥 클릭으로 닫기</summary>
public class WeaponInfoPopup : MonoBehaviour, IPointerClickHandler
{
    [Header("참조")]
    [SerializeField] private InventoryBoard _board;      // 등급색(밝은 창 배경용 어두운 색)
    [SerializeField] private WeaponLoadout _loadout;     // 등급 피해 배율
    [SerializeField] private Button _closeButton;
    [SerializeField] private TMP_Text _titleText; // 창 제목 - 무기·아티팩트 정보

    [Header("헤더")]
    [SerializeField] private RectTransform _iconBox;     // 아이콘 틀 - 정수 배율 맞춤 기준
    [SerializeField] private Image _icon;
    [SerializeField] private TMP_Text _attackTypeText;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _descriptionText;

    [Header("스테이터스")]
    [SerializeField] private TMP_Text _damageText;
    [SerializeField] private TMP_Text _fireIntervalText;
    [SerializeField] private TMP_Text _targetText;
    [SerializeField] private TMP_Text _rangeText;

    [Tooltip("무기 전용 줄 - 아티팩트면 숨김 (스테이터스·스킬)")]
    [SerializeField] private GameObject[] _weaponOnlyRows;

    [Tooltip("아이콘 틀 안쪽 여백(px)")]
    [SerializeField] private float _iconPadding = 12f;

    private void Awake()
    {
        _closeButton.onClick.AddListener(Close);
    }

    /// <summary>ESC 닫기</summary>
    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Close();
    }

    /// <summary>무기 정보 표시 - 공격력은 등급 배율만 적용(특성 제외), 아티팩트는 등급·원작 등급·등급 반영 설명</summary>
    public void Open(InventoryItem item)
    {
        ShowIcon(item.Data.Icon);

        WeaponData data = item.Data as WeaponData;
        _titleText.text = data != null ? "무기 정보" : "아티팩트 정보";
        foreach (GameObject row in _weaponOnlyRows) row.SetActive(data != null);

        string gradeColor = ColorUtility.ToHtmlStringRGB(_board.GetCellLineColor(item.Grade));
        _nameText.text = $"{item.Data.DisplayName}  <size=75%><color=#{gradeColor}>{item.Grade}등급</color></size>";
        if (data != null)
        {
            _attackTypeText.text = GetAttackTypeName(data.AttackType);
            _descriptionText.text = data.Description;

            int damage = Mathf.RoundToInt(data.Damage * _loadout.GetGradeMultiplier(item.Grade));
            _damageText.text = damage.ToString();
            _fireIntervalText.text = data.FireInterval.ToString("0.00", CultureInfo.InvariantCulture) + "초";
            _targetText.text = "방어선 근처 적 우선";
            _rangeText.text = data.Range.ToString("0.0", CultureInfo.InvariantCulture) + "m";
        }
        else
        {
            ArtifactData artifact = (ArtifactData)item.Data;
            _attackTypeText.text = $"{artifact.RarityName} 아티팩트";
            _descriptionText.text = artifact.GetDescription(item.Grade);
        }

        gameObject.SetActive(true);
        transform.SetAsLastSibling();
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    /// <summary>바깥(딤) 클릭 닫기</summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.pointerCurrentRaycast.gameObject == gameObject) Close();
    }

    /// <summary>아이콘 - 틀 안에 들어가는 최대 정수 배율(최소 1)</summary>
    private void ShowIcon(Sprite sprite)
    {
        _icon.enabled = sprite != null;
        if (sprite == null) return;

        Vector2 spriteSize = sprite.rect.size;
        Vector2 inner = _iconBox.rect.size - Vector2.one * (_iconPadding * 2f);
        float scale = Mathf.Max(1f, Mathf.Floor(Mathf.Min(inner.x / spriteSize.x, inner.y / spriteSize.y)));
        _icon.sprite = sprite;
        _icon.rectTransform.sizeDelta = spriteSize * scale;
    }

    private static string GetAttackTypeName(AttackType type)
    {
        switch (type)
        {
            case AttackType.Area: return "범위 공격";
            case AttackType.Pierce: return "관통 공격";
            case AttackType.Summon: return "소환 공격";
            default: return "일반 공격";
        }
    }
}

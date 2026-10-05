using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>HUD 무기 칸 - 격자 장착 무기를 한 칸에 하나씩 등급색 칸 + 그림으로 표시</summary>
[RequireComponent(typeof(RectTransform))]
public class WeaponSummaryView : MonoBehaviour
{
    [SerializeField] private WeaponLoadout _loadout;

    [Tooltip("등급색 참조")]
    [SerializeField] private InventoryBoard _board;

    [Tooltip("무기 있는 칸 스프라이트 - 등급색 틴트")]
    [SerializeField] private Sprite _cellSprite;
    [SerializeField] private Sprite _emptyCellSprite; // 빈 칸 스프라이트
    [SerializeField] private float _pixelScale = 2f;   // 9-slice 테두리 표시 배율
    [SerializeField] private int _columns = 8;
    [SerializeField] private int _rows = 2;
    [SerializeField] private float _iconPadding = 8f; // 칸 안 무기 그림 여백

    private Image[] _cells;
    private Image[] _icons;

    /// <summary>칸 생성 - 칸 크기 = 너비 / 열 수</summary>
    private void Awake()
    {
        RectTransform rect = (RectTransform)transform;
        float cellSize = rect.rect.width / _columns;
        _cells = new Image[_columns * _rows];
        _icons = new Image[_cells.Length];

        for (int i = 0; i < _cells.Length; i++)
        {
            Vector2 topLeftOffset = new Vector2(i % _columns + 0.5f, -(i / _columns + 0.5f)) * cellSize;
            _cells[i] = CreateImage($"Cell_{i}", rect, Vector2.one * cellSize);
            _cells[i].type = Image.Type.Sliced;
            _cells[i].pixelsPerUnitMultiplier = 1f / _pixelScale;
            _cells[i].rectTransform.anchorMin = _cells[i].rectTransform.anchorMax = new Vector2(0f, 1f);
            _cells[i].rectTransform.anchoredPosition = topLeftOffset;

            _icons[i] = CreateImage("Icon", _cells[i].rectTransform, Vector2.one * (cellSize - _iconPadding * 2f));
            _icons[i].preserveAspect = true;
        }

        Refresh();
    }

    /// <summary>장착 무기 다시 표시 - 칸 수 초과분 생략</summary>
    public void Refresh()
    {
        List<InventoryItem> items = new List<InventoryItem>(_loadout.EquippedItems);
        for (int i = 0; i < _cells.Length; i++)
        {
            bool hasItem = i < items.Count;
            _cells[i].sprite = hasItem ? _cellSprite : _emptyCellSprite;
            _cells[i].color = hasItem ? _board.GetGradeColor(items[i].Grade) : Color.white;
            _icons[i].gameObject.SetActive(hasItem);
            if (hasItem) _icons[i].sprite = items[i].Data.Icon;
        }
    }

    private static Image CreateImage(string imageName, RectTransform parent, Vector2 size)
    {
        Image image = new GameObject(imageName, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        image.rectTransform.SetParent(parent, false);
        image.rectTransform.sizeDelta = size;
        image.raycastTarget = false;
        return image;
    }
}

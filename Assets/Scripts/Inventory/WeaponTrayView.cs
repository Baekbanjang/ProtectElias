using UnityEngine;
using UnityEngine.UI;

/// <summary>무기 영역 트레이 표시 - 무기 수별 슬롯 배치(3x1·4x1·5x2)·배율, 슬롯 미리보기, 화면 좌표↔슬롯 변환</summary>
[RequireComponent(typeof(RectTransform))]
public class WeaponTrayView : MonoBehaviour
{
    public const int Capacity = 10;          // 최대 무기 수 (5x2)
    public const float CompactScale = 0.5f;  // 5개 이상 배율 - 셀 64 → 32
    private const int LongestItemCells = 4;  // 가장 긴 무기 칸 수 - 4개 배치 중간 배율 기준

    [SerializeField] private float _wallPadding = 16f; // 트레이 벽 안쪽 여백
    [SerializeField] private float _itemPadding = 12f; // 슬롯 안 무기 둘레 여백 - 무기 간격 = 2배

    [SerializeField] private Color _validColor = new Color(0.3f, 1f, 0.3f, 0.25f);   // 배치·머지 가능 슬롯
    [SerializeField] private Color _invalidColor = new Color(1f, 0.3f, 0.3f, 0.25f); // 배치 불가 슬롯

    private Image _previewImage;
    private RectTransform _rect;
    private Vector2Int _shape = new Vector2Int(3, 1); // 현재 배치 열·행
    private float _layoutScale = 1f;

    /// <summary>트레이 무기 부모</summary>
    public RectTransform ItemLayer { get; private set; }

    /// <summary>슬롯 미리보기·무기 레이어 생성 - 보드 초기화 시 1회</summary>
    public void Build()
    {
        _rect = (RectTransform)transform;

        RectTransform previewLayer = CreateLayer("Preview");
        ItemLayer = CreateLayer("Items");

        _previewImage = new GameObject("SlotPreview", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        _previewImage.rectTransform.SetParent(previewLayer, false);
        _previewImage.raycastTarget = false;
        _previewImage.gameObject.SetActive(false);
    }

    /// <summary>무기 수에 맞춰 배치 - 3 이하 3x1 원래 크기 / 4 = 4x1 중간 / 5 이상 5x2 축소</summary>
    public void SetLayout(int itemCount)
    {
        _shape = GetShape(itemCount);
        if (itemCount <= 3) _layoutScale = 1f;
        else if (itemCount == 4) _layoutScale = FloorToScreenPixel(GetItemArea(_shape).x / (LongestItemCells * InventoryGridView.CellSize));
        else _layoutScale = CompactScale;
    }

    /// <summary>무기 표시 배율 - 배치 배율, 슬롯보다 크면 슬롯에 맞춰 축소(회전 유지)</summary>
    public float GetItemScale(Vector2Int itemSize)
    {
        Vector2 area = GetItemArea(_shape);
        float fitScale = Mathf.Min(area.x / itemSize.x, area.y / itemSize.y) / InventoryGridView.CellSize;
        return fitScale < _layoutScale ? FloorToScreenPixel(fitScale) : _layoutScale;
    }

    /// <summary>현재 배치 슬롯 중심 로컬 좌표 - 0 = 왼쪽 위, 행 우선</summary>
    public Vector2 GetSlotCenter(int slot) => GetSlotCenter(slot, _shape);

    /// <summary>화면 좌표 → 현재 배치 슬롯, 트레이 밖이면 false</summary>
    public bool TryGetSlotAt(Vector2 screenPosition, Camera eventCamera, out int slot)
    {
        slot = -1;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, screenPosition, eventCamera, out Vector2 local)) return false;

        Vector2 slotSize = GetSlotSize(_shape);
        int column = Mathf.FloorToInt((local.x + _rect.rect.width * 0.5f - _wallPadding) / slotSize.x);
        int row = Mathf.FloorToInt((_rect.rect.height * 0.5f - _wallPadding - local.y) / slotSize.y);
        if (column < 0 || column >= _shape.x || row < 0 || row >= _shape.y) return false;

        slot = row * _shape.x + column;
        return true;
    }

    /// <summary>화면 좌표가 트레이 영역 안인지</summary>
    public bool ContainsScreenPoint(Vector2 screenPosition, Camera eventCamera)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(_rect, screenPosition, eventCamera);
    }

    /// <summary>슬롯 미리보기 - itemCount 개 배치 기준 슬롯, 초록 가능 / 빨강 불가</summary>
    public void ShowPreview(int slot, int itemCount, bool isValid)
    {
        Vector2Int shape = GetShape(itemCount);
        _previewImage.rectTransform.sizeDelta = GetSlotSize(shape);
        _previewImage.rectTransform.anchoredPosition = GetSlotCenter(slot, shape);
        _previewImage.color = isValid ? _validColor : _invalidColor;
        _previewImage.gameObject.SetActive(true);
    }

    /// <summary>미리보기 끄기</summary>
    public void ClearPreview()
    {
        _previewImage.gameObject.SetActive(false);
    }

    private static Vector2Int GetShape(int itemCount)
    {
        if (itemCount <= 3) return new Vector2Int(3, 1);
        return itemCount == 4 ? new Vector2Int(4, 1) : new Vector2Int(5, 2);
    }

    private Vector2 GetSlotSize(Vector2Int shape)
    {
        return new Vector2((_rect.rect.width - _wallPadding * 2f) / shape.x, (_rect.rect.height - _wallPadding * 2f) / shape.y);
    }

    /// <summary>슬롯 안 무기가 들어갈 영역 - 슬롯에서 둘레 여백 제외</summary>
    private Vector2 GetItemArea(Vector2Int shape)
    {
        return GetSlotSize(shape) - Vector2.one * (_itemPadding * 2f);
    }

    private Vector2 GetSlotCenter(int slot, Vector2Int shape)
    {
        Vector2 slotSize = GetSlotSize(shape);
        int column = slot % shape.x;
        int row = slot / shape.x;
        return new Vector2(_wallPadding + (column + 0.5f) * slotSize.x - _rect.rect.width * 0.5f,
                           _rect.rect.height * 0.5f - _wallPadding - (row + 0.5f) * slotSize.y);
    }

    /// <summary>화면 셀 크기가 정수 px 가 되도록 배율 내림</summary>
    private float FloorToScreenPixel(float scale)
    {
        float cellPixels = InventoryGridView.CellSize * _rect.lossyScale.x; // 배율 1 셀의 화면 px
        return Mathf.Floor(scale * cellPixels + 0.001f) / cellPixels;
    }

    private RectTransform CreateLayer(string layerName)
    {
        RectTransform layer = new GameObject(layerName, typeof(RectTransform)).GetComponent<RectTransform>();
        layer.SetParent(_rect, false);
        layer.anchorMin = layer.anchorMax = new Vector2(0.5f, 0.5f);
        layer.sizeDelta = _rect.rect.size;
        return layer;
    }
}

using UnityEngine;
using UnityEngine.UI;

/// <summary>격자 표시 - 개방 칸·무기 레이어·배치 미리보기, 화면 좌표↔셀 변환</summary>
[RequireComponent(typeof(RectTransform))]
public class InventoryGridView : MonoBehaviour
{
    public const float CellSize = 64f; // 원본 32 x2

    [SerializeField] private Sprite _openCellSprite;
    [SerializeField] private Sprite _validSprite;
    [SerializeField] private Sprite _invalidSprite;
    [SerializeField] private Color _cellColor = Color.white;  // 개방 칸 색 - 알파로 옅게
    [SerializeField] private Color _pushColor = new Color(1f, 0.6f, 0.15f, 0.85f); // 밀어내기 미리보기 색

    private Image[,] _cellImages;
    private Image[,] _previewImages;
    private RectTransform _rect;
    private int _width;
    private int _height;

    /// <summary>격자 위 무기 부모</summary>
    public RectTransform ItemLayer { get; private set; }

    /// <summary>셀·레이어 생성 - 보드 초기화 시 1회</summary>
    public void Build(InventoryGrid grid)
    {
        _width = grid.Width;
        _height = grid.Height;
        _cellImages = new Image[_width, _height];
        _previewImages = new Image[_width, _height];
        _rect = (RectTransform)transform;
        _rect.sizeDelta = new Vector2(_width, _height) * CellSize;

        RectTransform cellLayer = CreateLayer("Cells");
        ItemLayer = CreateLayer("Items");
        RectTransform previewLayer = CreateLayer("Preview");

        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                _cellImages[x, y] = CreateCellImage(cellLayer, cell, _openCellSprite);
                _cellImages[x, y].color = _cellColor;
                _previewImages[x, y] = CreateCellImage(previewLayer, cell, _validSprite);
                _previewImages[x, y].gameObject.SetActive(false);
            }
        }
    }

    /// <summary>개방 칸만 표시</summary>
    public void Refresh(InventoryGrid grid)
    {
        for (int x = 0; x < _width; x++)
            for (int y = 0; y < _height; y++)
                _cellImages[x, y].gameObject.SetActive(grid.IsOpen(new Vector2Int(x, y)));
    }

    /// <summary>셀 중심 로컬 좌표</summary>
    public Vector2 GetCellCenter(Vector2Int cell)
    {
        return new Vector2((cell.x - (_width - 1) * 0.5f) * CellSize, (cell.y - (_height - 1) * 0.5f) * CellSize);
    }

    /// <summary>무기 외곽 중심 로컬 좌표 - origin 좌하단 셀 기준</summary>
    public Vector2 GetItemCenter(Vector2Int origin, Vector2Int size)
    {
        return GetCellCenter(origin) + (Vector2)(size - Vector2Int.one) * (CellSize * 0.5f);
    }

    /// <summary>무기 외곽 중심(월드) → 좌하단 셀 반올림</summary>
    public Vector2Int GetOriginAt(Vector3 itemWorldCenter, Vector2Int size)
    {
        Vector2 local = _rect.InverseTransformPoint(itemWorldCenter);
        Vector2 bottomLeft = local - (Vector2)(size - Vector2Int.one) * (CellSize * 0.5f);
        return new Vector2Int(Mathf.RoundToInt(bottomLeft.x / CellSize + (_width - 1) * 0.5f),
                              Mathf.RoundToInt(bottomLeft.y / CellSize + (_height - 1) * 0.5f));
    }

    /// <summary>화면 좌표 → 셀, 격자 밖이면 false</summary>
    public bool TryGetCellAt(Vector2 screenPosition, Camera eventCamera, out Vector2Int cell)
    {
        cell = default;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, screenPosition, eventCamera, out Vector2 local)) return false;

        cell = new Vector2Int(Mathf.FloorToInt(local.x / CellSize + _width * 0.5f), Mathf.FloorToInt(local.y / CellSize + _height * 0.5f));
        return cell.x >= 0 && cell.x < _width && cell.y >= 0 && cell.y < _height;
    }

    /// <summary>화면 좌표가 격자 영역 안인지</summary>
    public bool ContainsScreenPoint(Vector2 screenPosition, Camera eventCamera)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(_rect, screenPosition, eventCamera);
    }

    /// <summary>미리보기 표시 - 초록 가능 / 주황 밀어내기 / 빨강 불가</summary>
    public void ShowPreview(Vector2Int origin, Vector2Int[] cells, bool isValid, bool isPush = false)
    {
        ClearPreview();
        Sprite sprite = isPush ? _openCellSprite : isValid ? _validSprite : _invalidSprite;
        Color color = isPush ? _pushColor : Color.white;
        foreach (Vector2Int offset in cells)
        {
            Vector2Int cell = origin + offset;
            if (cell.x < 0 || cell.x >= _width || cell.y < 0 || cell.y >= _height) continue;

            Image image = _previewImages[cell.x, cell.y];
            image.sprite = sprite;
            image.color = color;
            image.gameObject.SetActive(true);
        }
    }

    /// <summary>미리보기 끄기</summary>
    public void ClearPreview()
    {
        foreach (Image image in _previewImages) image.gameObject.SetActive(false);
    }

    private RectTransform CreateLayer(string layerName)
    {
        RectTransform layer = new GameObject(layerName, typeof(RectTransform)).GetComponent<RectTransform>();
        layer.SetParent(_rect, false);
        layer.anchorMin = layer.anchorMax = new Vector2(0.5f, 0.5f);
        layer.sizeDelta = _rect.sizeDelta;
        return layer;
    }

    private Image CreateCellImage(RectTransform parent, Vector2Int cell, Sprite sprite)
    {
        Image image = new GameObject($"Cell_{cell.x}_{cell.y}", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        RectTransform rect = image.rectTransform;
        rect.SetParent(parent, false);
        rect.sizeDelta = new Vector2(CellSize, CellSize);
        rect.anchoredPosition = GetCellCenter(cell);
        image.sprite = sprite;
        image.raycastTarget = false;
        return image;
    }
}

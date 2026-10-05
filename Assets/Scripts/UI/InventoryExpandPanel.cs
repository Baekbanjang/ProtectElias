using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>인벤토리 확장 창 - + 칸 클릭 임시 개방·취소·초기화, 확장 완료로 격자에 확정, 무기 우클릭 정보</summary>
public class InventoryExpandPanel : MonoBehaviour, IPointerClickHandler
{
    [Header("참조")]
    [SerializeField] private InventoryBoard _board;

    [Tooltip("상점 격자 - 무기 표시 복제 원본")]
    [SerializeField] private InventoryGridView _shopGrid;

    [Tooltip("9x9 칸 부모 - 상점 격자와 같은 576x576")]
    [SerializeField] private RectTransform _gridRoot;
    [SerializeField] private TMP_Text _pointText;
    [SerializeField] private Button _resetButton;
    [SerializeField] private Button _confirmButton;

    [Header("칸 표시")]
    [SerializeField] private Sprite _openCellSprite;
    [SerializeField] private Color _openCellColor = new Color(0.18f, 0.18f, 0.188f);
    [SerializeField] private Sprite _lockedCellSprite;   // + 칸
    [SerializeField] private Sprite _pendingCellSprite;  // 임시 개방 칸
    [SerializeField] private Color _pendingCellColor = new Color(0.45f, 1f, 0.45f);

    private readonly List<Vector2Int> _pendingCells = new List<Vector2Int>(); // 이번 창에서 임시 개방한 칸
    private Image[,] _cellImages;
    private RectTransform _itemSnapshot; // 격자 무기 복제 - 보기 전용
    private int _points;
    private bool _isOpen;

    /// <summary>웨이브 진행 대기용</summary>
    public bool IsOpen => _isOpen;

    private int RemainingPoints => _points - _pendingCells.Count;

    /// <summary>9x9 칸 버튼 생성, 버튼 연결</summary>
    private void Awake()
    {
        InventoryGrid grid = _board.Grid;
        _cellImages = new Image[grid.Width, grid.Height];
        for (int x = 0; x < grid.Width; x++)
            for (int y = 0; y < grid.Height; y++)
                _cellImages[x, y] = CreateCell(new Vector2Int(x, y), grid);

        _resetButton.onClick.AddListener(ResetPending);
        _confirmButton.onClick.AddListener(Confirm);
    }

    /// <summary>확장 창 열기 - 포인트 지정, 격자 무기 복제 후 정지</summary>
    public void Open(int points)
    {
        _board.EnsureInitialized(); // 시작 확장 창은 상점(격자 소유)보다 먼저 열림 - 기본 3x5 개방 보장
        _isOpen = true;
        Time.timeScale = 0f;
        gameObject.SetActive(true);

        _points = points;
        _pendingCells.Clear();
        CopyGridItems();
        Refresh();
    }

    /// <summary>우클릭 - 격자 무기 정보 창</summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_gridRoot, eventData.position, eventData.pressEventCamera, out Vector2 local)) return;

        InventoryGrid grid = _board.Grid;
        Vector2 cellPosition = local / InventoryGridView.CellSize + new Vector2(grid.Width - 1, grid.Height - 1) * 0.5f;
        _board.ShowWeaponInfo(grid.GetItemAt(Vector2Int.RoundToInt(cellPosition)));
    }

    /// <summary>+ 칸 임시 개방(포인트 -1), 임시 개방 칸은 취소(포인트 +1)</summary>
    private void ToggleCell(Vector2Int cell)
    {
        if (_board.Grid.IsOpen(cell)) return;

        if (!_pendingCells.Remove(cell))
        {
            if (RemainingPoints <= 0) return;
            _pendingCells.Add(cell);
        }
        Refresh();
    }

    /// <summary>초기화 - 임시 개방 전부 취소</summary>
    private void ResetPending()
    {
        _pendingCells.Clear();
        Refresh();
    }

    /// <summary>확장 완료 - 임시 개방 칸 확정 후 닫기(상점이 이어서 열림 - 정지 유지)</summary>
    private void Confirm()
    {
        if (!CanConfirm()) return;

        _board.OpenCells(_pendingCells);
        Debug.Log($"인벤토리 확장 - {_pendingCells.Count}칸 개방, 남은 포인트 {RemainingPoints} 소멸");
        _pendingCells.Clear();
        _isOpen = false;
        gameObject.SetActive(false);
    }

    /// <summary>포인트 전부 사용 또는 더 열 칸 없음</summary>
    private bool CanConfirm()
    {
        InventoryGrid grid = _board.Grid;
        return RemainingPoints <= 0 || CountOpenCells() + _pendingCells.Count >= grid.Width * grid.Height;
    }

    /// <summary>칸 표시·포인트·완료 버튼 갱신</summary>
    private void Refresh()
    {
        InventoryGrid grid = _board.Grid;
        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                bool isOpen = grid.IsOpen(cell);
                bool isPending = _pendingCells.Contains(cell);
                Image image = _cellImages[x, y];
                image.sprite = isOpen ? _openCellSprite : isPending ? _pendingCellSprite : _lockedCellSprite;
                image.color = isOpen ? _openCellColor : isPending ? _pendingCellColor : Color.white;
                image.raycastTarget = !isOpen; // 열린 칸은 클릭 불가
            }
        }

        _pointText.text = $"인벤토리 확장 포인트 : {RemainingPoints}";
        _confirmButton.interactable = CanConfirm();
    }

    /// <summary>상점 격자 무기 복제 - 드래그 불가</summary>
    private void CopyGridItems()
    {
        if (_itemSnapshot != null) Destroy(_itemSnapshot.gameObject);

        _itemSnapshot = Instantiate(_shopGrid.ItemLayer, _gridRoot);
        _itemSnapshot.name = "ItemSnapshot";
        _itemSnapshot.anchoredPosition = Vector2.zero;
        _itemSnapshot.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;
        foreach (InventoryItemView view in _itemSnapshot.GetComponentsInChildren<InventoryItemView>()) Destroy(view);
    }

    private int CountOpenCells()
    {
        InventoryGrid grid = _board.Grid;
        int count = 0;
        for (int x = 0; x < grid.Width; x++)
            for (int y = 0; y < grid.Height; y++)
                if (grid.IsOpen(new Vector2Int(x, y))) count++;
        return count;
    }

    private Image CreateCell(Vector2Int cell, InventoryGrid grid)
    {
        Image image = new GameObject($"Cell_{cell.x}_{cell.y}", typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<Image>();
        RectTransform rect = image.rectTransform;
        rect.SetParent(_gridRoot, false);
        rect.sizeDelta = new Vector2(InventoryGridView.CellSize, InventoryGridView.CellSize);
        rect.anchoredPosition = new Vector2(cell.x - (grid.Width - 1) * 0.5f, cell.y - (grid.Height - 1) * 0.5f) * InventoryGridView.CellSize;
        image.GetComponent<Button>().onClick.AddListener(() => ToggleCell(cell));
        return image;
    }
}

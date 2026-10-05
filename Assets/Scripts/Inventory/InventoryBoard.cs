using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>인벤토리 보드 - 격자·무기 영역(트레이) 소유, 드래그 배치·회전·머지·밀어내기·제외 처리</summary>
public class InventoryBoard : MonoBehaviour
{
    private const int GridSize = 9;

    [Header("참조")]
    [SerializeField] private InventoryGridView _gridView;
    [SerializeField] private WeaponTrayView _trayView;
    [SerializeField] private ShopPanel _shop;
    [SerializeField] private WeaponLoadout _loadout;

    [Tooltip("드래그 중 무기 부모 - 최상단 표시")]
    [SerializeField] private RectTransform _dragLayer;

    [Tooltip("무기 정보 창 - 우클릭")]
    [SerializeField] private WeaponInfoPopup _infoPopup;

    [Header("시작 상태")]
    [SerializeField] private Vector2Int _startOpenSize = new Vector2Int(3, 5);
    [Tooltip("선택 캐릭터 없을 때 시작 무기")]
    [SerializeField] private WeaponData _startWeapon;
    [SerializeField] private int _startWeaponGrade = 1;

    [Tooltip("캐릭터 목록 - 선택 캐릭터의 시작 무기 사용")]
    [SerializeField] private CharacterData[] _characters;

    [Tooltip("메타 강화 - 시작 확장 포인트")]
    [SerializeField] private MetaUpgradeData _metaInventory;

    [Header("표시")]
    [SerializeField] private Sprite _occupiedCellSprite;

    [Tooltip("회전 애니메이션 시간(초)")]
    [SerializeField] private float _rotateDuration = 0.2f;

    [Tooltip("트레이 재배치 애니메이션 시간(초)")]
    [SerializeField] private float _trayLayoutDuration = 0.15f;

    [Tooltip("칸 경계선 밝기 - 등급색에 곱함")]
    [SerializeField, Range(0f, 1f)] private float _cellLineShade = 0.65f;

    [Tooltip("등급 배경색 (1~4등급)")]
    [SerializeField] private Color[] _gradeColors =
    {
        new Color(0.6f, 0.6f, 0.6f),
        new Color(0.3f, 0.8f, 0.3f),
        new Color(0.3f, 0.6f, 1f),
        new Color(0.65f, 0.35f, 0.9f),
    };

    private readonly InventoryGrid _grid = new InventoryGrid(GridSize, GridSize);
    private readonly List<InventoryItem> _tray = new List<InventoryItem>(); // 트레이 순서 = 슬롯 순서
    private readonly Dictionary<InventoryItem, InventoryItemView> _views = new Dictionary<InventoryItem, InventoryItemView>();
    private readonly List<InventoryItem> _overlaps = new List<InventoryItem>();

    // 드래그 상태
    private InventoryItemView _dragged;
    private RectTransform _dragOriginParent;
    private Vector2 _dragOriginPosition;
    private float _dragOriginScale;
    private int _dragOriginRotation;
    private Vector2 _grabOffset;
    private Vector2 _pointerPosition;
    private Camera _pointerCamera;
    private InventoryItemView _pendingReturn; // 부모 비활성화 중 취소 - 다음 활성 프레임에 원위치

    public InventoryGrid Grid => _grid;
    public int StartExpansionPoints => _metaInventory.TotalCount;
    public IReadOnlyList<InventoryItem> TraySlots => _tray;
    public Sprite OccupiedCellSprite => _occupiedCellSprite;
    public float RotateDuration => _rotateDuration;
    public Color GetGradeColor(int grade) => _gradeColors[grade - 1];

    /// <summary>칸 경계선 색 - 등급색을 어둡게</summary>
    public Color GetCellLineColor(int grade)
    {
        Color color = GetGradeColor(grade) * _cellLineShade;
        color.a = 1f;
        return color;
    }

    private bool _isInitialized;

    private void Awake() => EnsureInitialized();

    /// <summary>격자·트레이 생성, 시작 칸 개방·시작 무기 배치 - 상점보다 먼저 열리는 시작 확장 창에서도 호출</summary>
    public void EnsureInitialized()
    {
        if (_isInitialized) return;
        _isInitialized = true;

        _gridView.Build(_grid);

        Vector2Int openSize = _startOpenSize;
        Vector2Int openOrigin = (Vector2Int.one * GridSize - openSize) / 2; // 9x9 가운데
        _grid.OpenArea(openOrigin, openSize);
        _gridView.Refresh(_grid);

        _trayView.Build();

        CharacterData character = CharacterData.FindSelected(_characters);
        WeaponData startWeapon = character != null ? character.StartingWeapon : _startWeapon;
        InventoryItem startItem = new InventoryItem(startWeapon, _startWeaponGrade);
        Vector2Int startOrigin = openOrigin + (openSize - startItem.GetSize()) / 2;
        if (_grid.CanPlace(startItem, startOrigin)) MoveToGrid(CreateView(startItem), startOrigin);
        else Debug.LogError("InventoryBoard: 시작 무기를 시작 칸에 놓을 수 없다");
    }

    /// <summary>비활성화 시 드래그 원위치</summary>
    private void OnDisable()
    {
        CancelDrag();
    }

    /// <summary>보류된 원위치 처리</summary>
    private void Update()
    {
        if (_pendingReturn == null) return;

        ReturnToOrigin(_pendingReturn);
        _pendingReturn = null;
    }

    /// <summary>트레이 끝에 새 무기 배치 - 가득 차면 false</summary>
    public bool TryAddWeaponAreaItem(InventoryItem item)
    {
        if (IsTrayFull()) return false;

        MoveToTray(CreateView(item));
        return true;
    }

    /// <summary>칸 개방 - 인벤토리 확장</summary>
    public void OpenCells(IEnumerable<Vector2Int> cells)
    {
        foreach (Vector2Int cell in cells) _grid.OpenArea(cell, Vector2Int.one);
        _gridView.Refresh(_grid);
    }

    /// <summary>트레이 전부 비우기 - 전투 시작</summary>
    public void ClearWeaponArea()
    {
        RemoveWeaponAreaItems(item => true);
    }

    /// <summary>트레이 새 무기 중 해당 종류 제거 - 제외 처리</summary>
    public void RemoveWeaponAreaItems(GridItemData data)
    {
        RemoveWeaponAreaItems(item => !item.HasEnteredGrid && item.Data == data);
    }

    /// <summary>트레이 무기 테두리 표시 - 제외 모드</summary>
    public void SetTrayFrames(bool isVisible, Color color)
    {
        foreach (InventoryItem item in _tray)
            if (item != null) _views[item].SetFrame(isVisible, color);
    }

    /// <summary>무기 정보 창 열기 - 드래그 중 무시</summary>
    public void ShowWeaponInfo(InventoryItem item)
    {
        if (_dragged != null || item == null) return;
        _infoPopup.Open(item);
    }

    /// <summary>격자 무기 클릭 - 트레이 끝으로 되돌림(트레이 드롭과 같은 경로), 가득 차면 무시</summary>
    public void ReturnToTray(InventoryItemView view)
    {
        if (_dragged != null || _shop.IsExcludeMode || !_grid.Contains(view.Item) || IsTrayFull()) return;

        DetachFromSource(view);
        MoveToTray(view);
    }

    /// <summary>드래그 가능 - 제외 모드 중에는 트레이 무기만</summary>
    public bool CanDrag(InventoryItem item)
    {
        return !_shop.IsExcludeMode || IsInTray(item);
    }

    /// <summary>드래그 시작 - 최상단 레이어로 이동, 트레이 무기는 원래 크기로</summary>
    public void BeginDrag(InventoryItemView view, PointerEventData eventData)
    {
        _dragged = view;
        _dragOriginParent = (RectTransform)view.Rect.parent;
        _dragOriginPosition = view.Rect.anchoredPosition;
        _dragOriginScale = view.Rect.localScale.x;
        _dragOriginRotation = view.Item.Rotation;

        view.Rect.SetParent(_dragLayer, true);
        view.Rect.SetAsLastSibling();
        _grabOffset = ((Vector2)view.Rect.localPosition - ToDragLayer(eventData.position, eventData.pressEventCamera)) / view.Rect.localScale.x; // 잡은 지점 유지
        view.Rect.localScale = Vector3.one;

        Drag(eventData);
    }

    /// <summary>드래그 중 - 포인터 추적·미리보기</summary>
    public void Drag(PointerEventData eventData)
    {
        if (_dragged == null) return;

        _pointerPosition = eventData.position;
        _pointerCamera = eventData.pressEventCamera;
        FollowPointer();
        UpdatePreview();
    }

    /// <summary>드래그 중 회전 - 셀 즉시 90도, 그림은 잡은 지점 기준 애니메이션</summary>
    public void RotateDragged()
    {
        if (_dragged == null) return;

        _grabOffset = new Vector2(_grabOffset.y, -_grabOffset.x); // 잡은 지점 유지
        _dragged.RotateClockwise(-_grabOffset);
        FollowPointer();
        UpdatePreview();
    }

    /// <summary>드래그 강제 취소 - 원위치·미리보기 끔</summary>
    public void CancelDrag()
    {
        if (_dragged == null) return;

        ClearPreviews();
        InventoryItemView view = _dragged;
        _dragged = null;
        view.StopDrag();

        if (_dragLayer.gameObject.activeInHierarchy)
        {
            ReturnToOrigin(view);
            return;
        }

        // 드래그 레이어 비활성화 중에는 SetParent 불가 - 회전만 즉시 복원
        view.Item.SetRotation(_dragOriginRotation);
        view.SnapRotation();
        _pendingReturn = view;
    }

    /// <summary>드롭 - 제외 모드면 제외 창만, 아니면 격자 → 트레이 순 판정, 실패 시 원위치</summary>
    public void EndDrag(PointerEventData eventData)
    {
        if (_dragged == null) return;

        _pointerPosition = eventData.position;
        _pointerCamera = eventData.pressEventCamera;
        ClearPreviews();

        InventoryItemView view = _dragged;
        _dragged = null;

        if (_shop.IsExcludeMode)
        {
            if (_shop.IsExcludeDropTarget(_pointerPosition, _pointerCamera)) DropOnExclude(view);
            else ReturnToOrigin(view);
            return;
        }

        if (_gridView.ContainsScreenPoint(_pointerPosition, _pointerCamera))
        {
            DropOnGrid(view);
            return;
        }

        if (_trayView.ContainsScreenPoint(_pointerPosition, _pointerCamera)) DropOnTray(view);
        else ReturnToOrigin(view);
    }

    /// <summary>제외 - 트레이 무기 소멸, 종류 제외는 상점 처리</summary>
    private void DropOnExclude(InventoryItemView view)
    {
        GridItemData data = view.Item.Data;
        DetachFromSource(view);
        view.gameObject.SetActive(false); // Destroy 처리 전 프레임에 원본 배율로 잔상 노출 방지
        Destroy(view.gameObject);
        _shop.ExcludeWeapon(data);
    }

    /// <summary>격자 드롭 - 머지 → 밀어내기 교체(걸친 무기는 트레이 빈 슬롯으로) → 불가</summary>
    private void DropOnGrid(InventoryItemView view)
    {
        InventoryItem item = view.Item;
        InventoryItem target = GetGridItemUnderPointer();
        if (item.CanMergeInto(target))
        {
            Merge(view, _views[target]);
            return;
        }

        Vector2Int origin = _gridView.GetOriginAt(view.Rect.position, item.GetSize());
        if (!CanPush(item, origin))
        {
            ReturnToOrigin(view);
            return;
        }

        InventoryItem[] pushed = _overlaps.ToArray();
        if (!_grid.Contains(item)) DetachFromSource(view);
        foreach (InventoryItem pushedItem in pushed)
        {
            InventoryItemView pushedView = _views[pushedItem];
            DetachFromSource(pushedView);
            MoveToTray(pushedView);
            Debug.Log($"밀어내기 - {pushedItem.Data.DisplayName} → 무기 영역");
        }
        MoveToGrid(view, origin);
    }

    /// <summary>트레이 드롭 - 머지 → 트레이 끝(트레이 무기는 원위치) → 불가</summary>
    private void DropOnTray(InventoryItemView view)
    {
        InventoryItem item = view.Item;
        InventoryItem target = GetTrayItemUnderPointer();
        if (item.CanMergeInto(target))
        {
            Merge(view, _views[target]);
            return;
        }

        if (IsInTray(item))
        {
            LayoutTray(); // 트레이 안 재배치 - 드래그 중 회전 유지
            return;
        }

        if (IsTrayFull())
        {
            ReturnToOrigin(view);
            return;
        }

        DetachFromSource(view);
        MoveToTray(view);
    }

    /// <summary>머지 - 대상 1등급 상승 후 끌던 무기 소멸 (상승 먼저 - 베개 최대 HP 감소로 현재 HP 깎임 방지)</summary>
    private void Merge(InventoryItemView dragged, InventoryItemView target)
    {
        if (dragged.Item.HasEnteredGrid) target.Item.HasEnteredGrid = true; // 내 무기 합친 결과는 새로고침 보존
        target.Item.HealedMaxHp += dragged.Item.HealedMaxHp; // 베개 회복 계보 합산
        target.Item.Upgrade();
        target.Redraw();
        _loadout.UpdateGrade(target.Item);
        Debug.Log($"머지 - {target.Item.Data.DisplayName} {target.Item.Grade}등급");

        DetachFromSource(dragged);
        dragged.gameObject.SetActive(false); // Destroy 처리 전 프레임에 원본 배율로 잔상 노출 방지
        Destroy(dragged.gameObject);
    }

    /// <summary>격자 배치 - 처음 들어오면 장착</summary>
    private void MoveToGrid(InventoryItemView view, Vector2Int origin)
    {
        InventoryItem item = view.Item;
        bool wasInGrid = _grid.Contains(item);

        _grid.Place(item, origin);
        item.HasEnteredGrid = true;
        _views[item] = view;
        view.AttachTo(_gridView.ItemLayer, _gridView.GetItemCenter(origin, item.GetSize()), 1f);

        if (!wasInGrid) _loadout.Equip(item);
    }

    /// <summary>트레이 끝에 배치 - 무기 수에 맞춰 재배치</summary>
    private void MoveToTray(InventoryItemView view)
    {
        _tray.Add(view.Item);
        _views[view.Item] = view;
        LayoutTray();
    }

    /// <summary>트레이 재배치 - 무기 수별 슬롯·배율, 새로 들어온 무기는 즉시, 기존 무기는 애니메이션</summary>
    private void LayoutTray()
    {
        _trayView.SetLayout(_tray.Count);
        for (int slot = 0; slot < _tray.Count; slot++)
        {
            InventoryItemView view = _views[_tray[slot]];
            Vector2 center = _trayView.GetSlotCenter(slot);
            float scale = _trayView.GetItemScale(view.Item.GetSize());

            if (view.Rect.parent == _trayView.ItemLayer) view.MoveTo(center, scale, _trayLayoutDuration);
            else view.AttachTo(_trayView.ItemLayer, center, scale);
        }
    }

    /// <summary>현재 위치에서 제거 - 격자면 장착 해제</summary>
    private void DetachFromSource(InventoryItemView view)
    {
        InventoryItem item = view.Item;
        if (_grid.Contains(item))
        {
            _grid.Remove(item);
            _loadout.Unequip(item);
        }
        else if (_tray.Remove(item))
        {
            LayoutTray();
        }
        _views.Remove(item);
    }

    /// <summary>원위치 복귀 - 회전·크기도 즉시 되돌림, 트레이 무기는 현재 배치로</summary>
    private void ReturnToOrigin(InventoryItemView view)
    {
        view.Item.SetRotation(_dragOriginRotation);
        view.SnapRotation();
        view.AttachTo(_dragOriginParent, _dragOriginPosition, _dragOriginScale);
        if (IsInTray(view.Item)) LayoutTray();
    }

    /// <summary>밀어내기 가능 - 걸친 무기(_overlaps) 수 ≤ 트레이 빈 자리(최대 10, 끌던 무기 자리 포함)</summary>
    private bool CanPush(InventoryItem item, Vector2Int origin)
    {
        if (!_grid.TryGetOverlaps(item, origin, _overlaps)) return false;

        int freeSlots = WeaponTrayView.Capacity - _tray.Count + (IsInTray(item) ? 1 : 0);
        return _overlaps.Count <= freeSlots;
    }

    private bool IsInTray(InventoryItem item) => _tray.Contains(item);

    private bool IsTrayFull() => _tray.Count >= WeaponTrayView.Capacity;

    private void RemoveWeaponAreaItems(Predicate<InventoryItem> match)
    {
        for (int slot = _tray.Count - 1; slot >= 0; slot--)
        {
            InventoryItem item = _tray[slot];
            if (!match(item)) continue;

            Destroy(_views[item].gameObject);
            _views.Remove(item);
            _tray.RemoveAt(slot);
        }
        LayoutTray();
    }

    private void UpdatePreview()
    {
        ClearPreviews();
        if (_shop.IsExcludeMode) return;

        InventoryItem item = _dragged.Item;
        if (_gridView.ContainsScreenPoint(_pointerPosition, _pointerCamera))
        {
            InventoryItem target = GetGridItemUnderPointer();
            if (item.CanMergeInto(target))
            {
                _gridView.ShowPreview(target.GridOrigin, target.GetCells(), true);
                return;
            }

            Vector2Int origin = _gridView.GetOriginAt(_dragged.Rect.position, item.GetSize());
            bool canPlace = CanPush(item, origin);
            _gridView.ShowPreview(origin, item.GetCells(), canPlace, canPlace && _overlaps.Count > 0);
            return;
        }

        if (!_trayView.TryGetSlotAt(_pointerPosition, _pointerCamera, out int hoveredSlot)) return;

        InventoryItem trayTarget = GetTrayItemUnderPointer();
        if (item.CanMergeInto(trayTarget))
        {
            _trayView.ShowPreview(hoveredSlot, _tray.Count, true);
            return;
        }
        if (IsInTray(item)) return;

        if (IsTrayFull()) _trayView.ShowPreview(hoveredSlot, _tray.Count, false);
        else _trayView.ShowPreview(_tray.Count, _tray.Count + 1, true); // 들어갈 자리 - 1개 늘어난 배치 기준
    }

    private void ClearPreviews()
    {
        _gridView.ClearPreview();
        _trayView.ClearPreview();
    }

    private void FollowPointer()
    {
        _dragged.Rect.localPosition = ToDragLayer(_pointerPosition, _pointerCamera) + _grabOffset;
    }

    /// <summary>포인터 아래 격자 무기 - 끌던 무기 자신은 제외</summary>
    private InventoryItem GetGridItemUnderPointer()
    {
        if (!_gridView.TryGetCellAt(_pointerPosition, _pointerCamera, out Vector2Int cell)) return null;

        return ExcludeDragged(_grid.GetItemAt(cell));
    }

    /// <summary>포인터 아래 트레이 무기 - 끌던 무기 자신은 제외</summary>
    private InventoryItem GetTrayItemUnderPointer()
    {
        if (!_trayView.TryGetSlotAt(_pointerPosition, _pointerCamera, out int slot) || slot >= _tray.Count) return null;

        return ExcludeDragged(_tray[slot]);
    }

    private InventoryItem ExcludeDragged(InventoryItem item)
    {
        return _dragged != null && item == _dragged.Item ? null : item;
    }

    private Vector2 ToDragLayer(Vector2 screenPosition, Camera eventCamera)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_dragLayer, screenPosition, eventCamera, out Vector2 local);
        return local;
    }

    private InventoryItemView CreateView(InventoryItem item)
    {
        GameObject viewObject = new GameObject($"Item_{item.Data.name}", typeof(RectTransform), typeof(CanvasGroup), typeof(InventoryItemView));
        InventoryItemView view = viewObject.GetComponent<InventoryItemView>();
        viewObject.transform.SetParent(_dragLayer, false);
        view.Setup(item, this);
        return view;
    }
}

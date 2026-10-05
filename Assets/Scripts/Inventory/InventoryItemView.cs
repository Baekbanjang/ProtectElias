using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>무기 1개 표시 - 등급색 점유 셀·칸 경계선 + 무기 그림, 드래그·회전·우클릭 정보 입력을 보드로 전달</summary>
[RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
public class InventoryItemView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    private const float FrameThickness = 6f; // 테두리 두께(로컬) - 트레이 축소 시 3
    private const float CellLineWidth = 3f;  // 칸 경계선 두께(로컬) - 원본 1.5px (2px 은 날카롭다는 피드백으로 1.5배)

    private InventoryBoard _board;
    private CanvasGroup _canvasGroup;
    private RectTransform _visual;  // 셀·그림 부모 - 회전 애니메이션 대상
    private Image[] _frameBars;      // 외곽 테두리 - 제외 모드 표시
    private readonly List<Image> _cellLines = new List<Image>();
    private float _lineWidth;        // 현재 칸 경계선 두께(로컬)
    private float _lineScale;        // 두께 계산 기준 화면 배율
    private bool _isDragging;

    // 회전 애니메이션 - 각도는 누적(도, 시계방향 음수)
    private bool _isRotating;
    private float _fromAngle;
    private float _targetAngle;
    private float _visualAngle;
    private float _rotateElapsed;
    private Vector2 _rotatePivot;

    // 이동 애니메이션 - 트레이 재배치
    private bool _isMoving;
    private Vector2 _moveFromPosition;
    private Vector2 _moveToPosition;
    private float _moveFromScale;
    private float _moveToScale;
    private float _moveDuration;
    private float _moveElapsed;

    public InventoryItem Item { get; private set; }
    public RectTransform Rect => (RectTransform)transform;

    /// <summary>생성 직후 초기화</summary>
    public void Setup(InventoryItem item, InventoryBoard board)
    {
        Item = item;
        _board = board;
        _canvasGroup = GetComponent<CanvasGroup>();
        Rect.anchorMin = Rect.anchorMax = Rect.pivot = new Vector2(0.5f, 0.5f);

        _visual = new GameObject("Visual", typeof(RectTransform)).GetComponent<RectTransform>();
        _visual.SetParent(transform, false);
        SnapRotation();
    }

    /// <summary>부모 변경 + 로컬 위치·배율 지정</summary>
    public void AttachTo(RectTransform parent, Vector2 localPosition, float scale)
    {
        _isMoving = false;
        Rect.SetParent(parent, false);
        Rect.anchoredPosition = localPosition;
        Rect.localScale = Vector3.one * scale;
    }

    /// <summary>같은 부모 안에서 위치·배율 이동 - ease-out 애니메이션(unscaled)</summary>
    public void MoveTo(Vector2 localPosition, float scale, float duration)
    {
        _moveFromPosition = Rect.anchoredPosition;
        _moveFromScale = Rect.localScale.x;
        _moveToPosition = localPosition;
        _moveToScale = scale;
        _moveDuration = duration;
        _moveElapsed = 0f;
        _isMoving = true;
    }

    /// <summary>외곽 테두리 표시·색 지정</summary>
    public void SetFrame(bool isVisible, Color color)
    {
        if (_frameBars == null) CreateFrame();

        foreach (Image bar in _frameBars)
        {
            bar.color = color;
            bar.gameObject.SetActive(isVisible);
        }
    }

    /// <summary>회전·등급 반영해 다시 그림</summary>
    public void Redraw()
    {
        for (int i = _visual.childCount - 1; i >= 0; i--) Destroy(_visual.GetChild(i).gameObject);
        _cellLines.Clear();

        float cellSize = InventoryGridView.CellSize;
        Vector2Int size = Item.GetSize();
        Rect.sizeDelta = (Vector2)size * cellSize;
        Vector2 half = (Vector2)(size - Vector2Int.one) * (cellSize * 0.5f);

        Vector2Int[] cells = Item.GetCells();
        Color gradeColor = _board.GetGradeColor(Item.Grade);
        foreach (Vector2Int cell in cells)
        {
            Image cellImage = CreateImage("Cell", _board.OccupiedCellSprite, new Vector2(cellSize, cellSize));
            cellImage.rectTransform.anchoredPosition = (Vector2)cell * cellSize - half;
            cellImage.color = gradeColor;
        }

        // 칸 경계선 - 경계마다 1번만(오른쪽·위는 항상, 왼쪽·아래는 바깥일 때만) → 내부·외곽 두께 같음
        HashSet<Vector2Int> cellSet = new HashSet<Vector2Int>(cells);
        Color lineColor = _board.GetCellLineColor(Item.Grade);
        foreach (Vector2Int cell in cells)
        {
            Vector2 center = (Vector2)cell * cellSize - half;
            CreateCellLine(center + new Vector2(cellSize * 0.5f, 0f), false, lineColor);
            CreateCellLine(center + new Vector2(0f, cellSize * 0.5f), true, lineColor);
            if (!cellSet.Contains(cell + Vector2Int.left)) CreateCellLine(center - new Vector2(cellSize * 0.5f, 0f), false, lineColor);
            if (!cellSet.Contains(cell + Vector2Int.down)) CreateCellLine(center - new Vector2(0f, cellSize * 0.5f), true, lineColor);
        }
        FitLineWidth();

        if (Item.Data.Icon == null) return;

        Image icon = CreateImage("Icon", Item.Data.Icon, Item.Data.Icon.rect.size * (cellSize / 32f)); // 원본 32px 셀 기준 x2
        icon.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -90f * Item.Rotation);
        icon.raycastTarget = false;
    }

    /// <summary>시계방향 90도 회전 - 데이터 즉시, 그림은 pivot(로컬) 기준 애니메이션</summary>
    public void RotateClockwise(Vector2 pivot)
    {
        Item.Rotate();
        _fromAngle = _visualAngle;
        _targetAngle -= 90f;
        _rotateElapsed = 0f;
        _rotatePivot = pivot;
        _isRotating = true;
        Redraw();
        ApplyVisualRotation();
    }

    /// <summary>현재 회전 상태로 즉시 맞춤 - 애니메이션 없음</summary>
    public void SnapRotation()
    {
        _targetAngle = _visualAngle = -90f * Item.Rotation;
        _isRotating = false;
        Redraw();
        ApplyVisualRotation();
    }

    /// <summary>드래그 강제 종료 - 입력 상태만 해제</summary>
    public void StopDrag()
    {
        _isDragging = false;
        _canvasGroup.blocksRaycasts = true;
    }

    /// <summary>드래그 중 R 키·우클릭 회전, 회전·이동 애니메이션</summary>
    private void Update()
    {
        if (_isDragging && IsRotatePressed()) _board.RotateDragged();
        if (_isRotating) AnimateRotation();
        if (_isMoving) AnimateMove();
    }

    /// <summary>화면 배율 바뀌면 칸 경계선 두께 다시 맞춤</summary>
    private void LateUpdate()
    {
        if (!Mathf.Approximately(Rect.lossyScale.x, _lineScale)) FitLineWidth();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || !_board.CanDrag(Item)) return;

        _isDragging = true;
        _isMoving = false;
        _canvasGroup.blocksRaycasts = false;
        _board.BeginDrag(this, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging) return;
        _board.Drag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_isDragging) return;

        StopDrag();
        _board.EndDrag(eventData);
    }

    /// <summary>우클릭 - 무기 정보 창, 좌클릭(드래그 아님) - 격자 무기 트레이로</summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right) _board.ShowWeaponInfo(Item);
        else if (eventData.button == PointerEventData.InputButton.Left && !eventData.dragging) _board.ReturnToTray(this);
    }

    private static bool IsRotatePressed()
    {
        return (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
               || (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame);
    }

    /// <summary>ease-out 보간 - 상점 정지 중에도 동작(unscaled)</summary>
    private void AnimateRotation()
    {
        _rotateElapsed += Time.unscaledDeltaTime;
        float duration = _board.RotateDuration;
        float t = duration > 0f ? Mathf.Clamp01(_rotateElapsed / duration) : 1f;

        _visualAngle = Mathf.LerpUnclamped(_fromAngle, _targetAngle, EaseOut(t));
        if (t >= 1f)
        {
            _visualAngle = _targetAngle;
            _isRotating = false;
        }
        ApplyVisualRotation();
    }

    /// <summary>ease-out 이동·배율 보간(unscaled)</summary>
    private void AnimateMove()
    {
        _moveElapsed += Time.unscaledDeltaTime;
        float t = _moveDuration > 0f ? Mathf.Clamp01(_moveElapsed / _moveDuration) : 1f;
        float eased = EaseOut(t);

        Rect.anchoredPosition = Vector2.LerpUnclamped(_moveFromPosition, _moveToPosition, eased);
        Rect.localScale = Vector3.one * Mathf.LerpUnclamped(_moveFromScale, _moveToScale, eased);
        if (t >= 1f) _isMoving = false;
    }

    private static float EaseOut(float t) => 1f - (1f - t) * (1f - t) * (1f - t);

    /// <summary>칸 경계선 두께 - 화면 정수 px(원본 1px 반올림, 최소 1px)</summary>
    private void FitLineWidth()
    {
        _lineScale = Rect.lossyScale.x;
        float screenPixels = Mathf.Max(1f, Mathf.Round(CellLineWidth * _lineScale));
        float width = screenPixels / _lineScale;
        foreach (Image line in _cellLines) line.rectTransform.sizeDelta += Vector2.one * (width - _lineWidth); // 길이 = 칸 + 두께(모서리 채움)
        _lineWidth = width;
    }

    /// <summary>목표 대비 남은 각도만큼 pivot 기준 역회전</summary>
    private void ApplyVisualRotation()
    {
        Quaternion offset = Quaternion.Euler(0f, 0f, _visualAngle - _targetAngle);
        _visual.localRotation = offset;
        _visual.anchoredPosition = _rotatePivot - (Vector2)(offset * _rotatePivot);
    }

    /// <summary>외곽 안쪽 테두리 4변 생성 - 외곽 크기 변경 자동 추종</summary>
    private void CreateFrame()
    {
        Vector2[] anchorMins = { new Vector2(0f, 1f), Vector2.zero, Vector2.zero, new Vector2(1f, 0f) };
        Vector2[] anchorMaxs = { Vector2.one, new Vector2(1f, 0f), new Vector2(0f, 1f), Vector2.one };
        Vector2[] offsetMins = { new Vector2(0f, -FrameThickness), Vector2.zero, Vector2.zero, new Vector2(-FrameThickness, 0f) };
        Vector2[] offsetMaxs = { Vector2.zero, new Vector2(0f, FrameThickness), new Vector2(FrameThickness, 0f), Vector2.zero };

        _frameBars = new Image[4];
        for (int i = 0; i < _frameBars.Length; i++)
        {
            Image bar = new GameObject("Frame", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            RectTransform rect = bar.rectTransform;
            rect.SetParent(transform, false);
            rect.anchorMin = anchorMins[i];
            rect.anchorMax = anchorMaxs[i];
            rect.offsetMin = offsetMins[i];
            rect.offsetMax = offsetMaxs[i];
            bar.raycastTarget = false;
            _frameBars[i] = bar;
        }
    }

    /// <summary>칸 경계선 1개 - 경계 중심에 걸침, 흰 단색 틴트</summary>
    private void CreateCellLine(Vector2 center, bool isHorizontal, Color color)
    {
        float cellSize = InventoryGridView.CellSize;
        Vector2 length = isHorizontal ? new Vector2(cellSize, 0f) : new Vector2(0f, cellSize);
        Image line = CreateImage("CellLine", null, length + Vector2.one * _lineWidth);
        line.rectTransform.anchoredPosition = center;
        line.color = color;
        line.raycastTarget = false;
        _cellLines.Add(line);
    }

    private Image CreateImage(string imageName, Sprite sprite, Vector2 size)
    {
        Image image = new GameObject(imageName, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        image.rectTransform.SetParent(_visual, false);
        image.rectTransform.sizeDelta = size;
        image.sprite = sprite;
        return image;
    }
}

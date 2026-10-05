using System.Collections.Generic;
using UnityEngine;

/// <summary>격자 모델 - 개방 여부·점유 무기, 배치 판정</summary>
public class InventoryGrid
{
    private readonly bool[,] _isOpen;
    private readonly InventoryItem[,] _occupants;
    private readonly List<InventoryItem> _items = new List<InventoryItem>();

    public int Width { get; }
    public int Height { get; }
    public IReadOnlyList<InventoryItem> Items => _items;

    public InventoryGrid(int width, int height)
    {
        Width = width;
        Height = height;
        _isOpen = new bool[width, height];
        _occupants = new InventoryItem[width, height];
    }

    /// <summary>직사각형 영역 개방</summary>
    public void OpenArea(Vector2Int origin, Vector2Int size)
    {
        for (int x = origin.x; x < origin.x + size.x; x++)
            for (int y = origin.y; y < origin.y + size.y; y++)
                if (IsInside(new Vector2Int(x, y))) _isOpen[x, y] = true;
    }

    public bool IsInside(Vector2Int cell) => cell.x >= 0 && cell.x < Width && cell.y >= 0 && cell.y < Height;

    public bool IsOpen(Vector2Int cell) => IsInside(cell) && _isOpen[cell.x, cell.y];

    public bool Contains(InventoryItem item) => _items.Contains(item);

    /// <summary>셀 점유 무기 - 없으면 null</summary>
    public InventoryItem GetItemAt(Vector2Int cell) => IsInside(cell) ? _occupants[cell.x, cell.y] : null;

    /// <summary>배치 가능 - 점유 셀 전부 개방 && 비어 있음(자기 자신 칸은 빈 칸)</summary>
    public bool CanPlace(InventoryItem item, Vector2Int origin)
    {
        foreach (Vector2Int offset in item.GetCells())
        {
            Vector2Int cell = origin + offset;
            if (!IsOpen(cell)) return false;

            InventoryItem occupant = _occupants[cell.x, cell.y];
            if (occupant != null && occupant != item) return false;
        }
        return true;
    }

    /// <summary>배치 칸에 걸친 다른 무기 수집 - 격자 밖·미개방 칸이면 false</summary>
    public bool TryGetOverlaps(InventoryItem item, Vector2Int origin, List<InventoryItem> overlaps)
    {
        overlaps.Clear();
        foreach (Vector2Int offset in item.GetCells())
        {
            Vector2Int cell = origin + offset;
            if (!IsOpen(cell)) return false;

            InventoryItem occupant = _occupants[cell.x, cell.y];
            if (occupant != null && occupant != item && !overlaps.Contains(occupant)) overlaps.Add(occupant);
        }
        return true;
    }

    /// <summary>배치 - 이미 격자에 있으면 이동</summary>
    public void Place(InventoryItem item, Vector2Int origin)
    {
        Remove(item);
        foreach (Vector2Int offset in item.GetCells())
        {
            Vector2Int cell = origin + offset;
            _occupants[cell.x, cell.y] = item;
        }
        item.GridOrigin = origin;
        _items.Add(item);
    }

    /// <summary>제거 - 점유 칸 비움</summary>
    public void Remove(InventoryItem item)
    {
        if (!_items.Remove(item)) return;

        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                if (_occupants[x, y] == item) _occupants[x, y] = null;
    }
}

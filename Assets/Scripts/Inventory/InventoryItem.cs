using UnityEngine;

/// <summary>인벤토리 아이템 1개(무기·아티팩트) - 종류·등급·회전·격자 위치</summary>
public class InventoryItem
{
    public const int MaxGrade = 4;

    public GridItemData Data { get; }
    public int Grade { get; private set; }
    public int Rotation { get; private set; }     // 시계방향 90도 단위 0~3
    public Vector2Int GridOrigin { get; set; }    // 격자 배치 시 좌하단 셀
    public bool HasEnteredGrid { get; set; }      // 격자 진입 이력 - 새로고침 보존 기준
    public int HealedMaxHp { get; set; }          // 베개 아티팩트 - 머지 계보가 이미 회복한 최대 HP

    public InventoryItem(GridItemData data, int grade)
    {
        Data = data;
        Grade = grade;
    }

    /// <summary>시계방향 90도 회전</summary>
    public void Rotate() => Rotation = (Rotation + 1) % 4;

    /// <summary>회전 상태 지정 - 드래그 취소 복원용</summary>
    public void SetRotation(int rotation) => Rotation = ((rotation % 4) + 4) % 4;

    /// <summary>머지 가능 - 같은 종류·같은 등급·최고 등급 미만</summary>
    public bool CanMergeInto(InventoryItem target)
    {
        return target != null && target != this && target.Data == Data && target.Grade == Grade && Grade < MaxGrade;
    }

    /// <summary>등급 1 상승</summary>
    public void Upgrade() => Grade = Mathf.Min(Grade + 1, MaxGrade);

    /// <summary>회전 적용 점유 셀 - 좌하단 (0,0) 기준</summary>
    public Vector2Int[] GetCells()
    {
        Vector2Int[] source = Data.OccupiedCells;
        Vector2Int[] cells = new Vector2Int[source.Length];
        int minX = int.MaxValue, minY = int.MaxValue;

        for (int i = 0; i < source.Length; i++)
        {
            Vector2Int cell = source[i];
            for (int r = 0; r < Rotation; r++) cell = new Vector2Int(cell.y, -cell.x); // 시계방향 90도
            cells[i] = cell;
            minX = Mathf.Min(minX, cell.x);
            minY = Mathf.Min(minY, cell.y);
        }

        for (int i = 0; i < cells.Length; i++) cells[i] -= new Vector2Int(minX, minY);
        return cells;
    }

    /// <summary>회전 적용 외곽 크기(셀)</summary>
    public Vector2Int GetSize()
    {
        Vector2Int size = Vector2Int.zero;
        foreach (Vector2Int cell in GetCells()) size = Vector2Int.Max(size, cell + Vector2Int.one);
        return size;
    }
}

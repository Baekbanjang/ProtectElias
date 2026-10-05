using UnityEngine;

/// <summary>격자 아이템 공용 데이터 - 무기·아티팩트</summary>
public abstract class GridItemData : ScriptableObject
{
    [Tooltip("표시 이름")]
    [SerializeField] private string _displayName;

    [Tooltip("설명 - 정보 창")]
    [TextArea(2, 4)]
    [SerializeField] private string _description;

    [Tooltip("점유 셀 - 좌하단 (0,0), 아이콘 방향과 일치")]
    [SerializeField] private Vector2Int[] _occupiedCells;

    [Tooltip("인벤토리 아이콘 - 원본 32px 셀 기준")]
    [SerializeField] private Sprite _icon;

    public string DisplayName => _displayName;
    public string Description => _description;
    public Vector2Int[] OccupiedCells => _occupiedCells;
    public Sprite Icon => _icon;
}

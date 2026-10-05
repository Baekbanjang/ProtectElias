using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    /// <summary>스폰 후보 1종 - 프리팹 · 가중치 · 등장 웨이브</summary>
    [System.Serializable]
    private struct EnemyEntry
    {
        public GameObject prefab;
        [Min(0f)] public float weight;
        [Tooltip("30웨이브부터 적용할 가중치")]
        [Min(0f)] public float lateWeight;
        [Tooltip("이 웨이브부터 등장")]
        public int minWave;
        [Tooltip("묶음 최소 마릿수 (0·1 = 1마리)")]
        public int groupMin;
        [Tooltip("묶음 최대 마릿수")]
        public int groupMax;
    }

    [Tooltip("스폰 후보 목록 - 가중치 무작위")]
    [SerializeField] private EnemyEntry[] _enemies;
    [SerializeField] private int _columnCount = 7;
    [SerializeField] private float _spawnMargin = 1.0f;
    
    [Tooltip("전투 영역 가로폭(유닛)")]
    [SerializeField] private float _areaWidth = 5.625f;

    [Header("묶음 스폰 - 격자로 동시 등장")]
    [Tooltip("묶음 안 가로 간격(유닛)")]
    [SerializeField] private float _groupSpacingX = 0.3f;
    [Tooltip("묶음 안 세로 간격(유닛)")]
    [SerializeField] private float _groupSpacingY = 0.3f;

    [Tooltip("스폰 비움 거리(유닛) - 스폰 높이에서 이 거리 안에 적이 있는 열은 피함")]
    [SerializeField] private float _spawnClearance = 1.5f;

    [Header("테스트용")]
    [Tooltip("테스트·영상용: 등장 웨이브 무시 - 1웨이브부터 전 종류 같은 확률로 등장")]
    [SerializeField] private bool _isIgnoringWaveGates;

    private float _areaLeft;
    private float _columnWidth;
    private float _spawnY;
    private readonly List<int> _freeColumns = new List<int>(); // 열 고르기 재사용 버퍼

    /// <summary>
    /// 전투 영역 고정 폭으로 열 좌표, 카메라 높이로 스폰 높이를 미리 계산
    /// </summary>
    private void Awake()
    {
        // 스폰 높이 계산용
        Camera cam = Camera.main;
        
        _areaLeft = -_areaWidth * 0.5f; // 화면 중앙 기준 왼쪽 끝
        _columnWidth = _areaWidth / _columnCount;
        _spawnY = cam.orthographicSize + _spawnMargin;
    }

    /// <summary>
    /// 무작위 열 하나를 골라 그 열의 가운데, 화면 위 밖에 적 1종을 생성 - 묶음 적은 1회 = 1묶음
    /// </summary>
    public void SpawnOne(int waveNumber)
    {
        int index = PickEnemy(waveNumber);
        if (index < 0)
        {
            Debug.LogWarning($"EnemySpawner: 웨이브 {waveNumber} 스폰 후보 없음");
            return;
        }

        EnemyEntry entry = _enemies[index];
        int column = PickColumn();
        float x = _areaLeft + _columnWidth * (column + 0.5f);

        int groupSize = Random.Range(Mathf.Max(1, entry.groupMin), Mathf.Max(1, entry.groupMin, entry.groupMax) + 1);
        if (groupSize <= 1)
        {
            SpawnEnemy(entry.prefab, new Vector3(x, _spawnY, 0f), waveNumber);
            return;
        }

        SpawnGroup(entry.prefab, x, groupSize, waveNumber);
    }

    /// <summary>보스 생성 - 동시 보스는 가로 위치 분리</summary>
    public EnemyMover SpawnBoss(GameObject prefab, int waveNumber, float offsetX = 0f)
    {
        return SpawnEnemy(prefab, new Vector3(offsetX, _spawnY, 0f), waveNumber);
    }

    private EnemyMover SpawnEnemy(GameObject prefab, Vector3 position, int waveNumber)
    {
        EnemyMover enemy = Instantiate(prefab, position, Quaternion.identity).GetComponent<EnemyMover>();
        enemy.InitializeForWave(waveNumber);
        return enemy;
    }

    /// <summary>스폰 지점 근처에 적이 없는 열 중 무작위 - 전부 막혔으면 아무 열</summary>
    private int PickColumn()
    {
        _freeColumns.Clear();
        for (int column = 0; column < _columnCount; column++)
        {
            if (!IsColumnBlocked(column)) _freeColumns.Add(column);
        }

        if (_freeColumns.Count == 0) return Random.Range(0, _columnCount);
        return _freeColumns[Random.Range(0, _freeColumns.Count)];
    }

    /// <summary>열 가운데 반폭 안 + 스폰 높이 근처에 적이 있으면 막힘</summary>
    private bool IsColumnBlocked(int column)
    {
        float centerX = _areaLeft + _columnWidth * (column + 0.5f);
        foreach (EnemyMover enemy in EnemyMover.Active)
        {
            Vector3 position = enemy.transform.position;
            if (Mathf.Abs(position.x - centerX) < _columnWidth * 0.5f && position.y > _spawnY - _spawnClearance) return true;
        }
        return false;
    }

    /// <summary>묶음 스폰 - 열 가운데 기준 격자(4마리 = 2×2)로 동시 생성, 같은 속도로 함께 내려옴</summary>
    private void SpawnGroup(GameObject prefab, float centerX, int groupSize, int waveNumber)
    {
        int columns = Mathf.CeilToInt(Mathf.Sqrt(groupSize));
        int rows = Mathf.CeilToInt(groupSize / (float)columns);

        for (int i = 0; i < groupSize; i++)
        {
            int col = i % columns;
            int row = i / columns;
            float offsetX = (col - (columns - 1) * 0.5f) * _groupSpacingX;
            float offsetY = (row - (rows - 1) * 0.5f) * _groupSpacingY;
            SpawnEnemy(prefab, new Vector3(centerX + offsetX, _spawnY + offsetY, 0f), waveNumber);
        }
    }

    /// <summary>등장 웨이브를 넘긴 후보 중 가중치 무작위 선택 - 없으면 -1</summary>
    private int PickEnemy(int waveNumber)
    {
        float totalWeight = 0f;
        foreach (EnemyEntry entry in _enemies)
        {
            if (IsAvailable(entry, waveNumber)) totalWeight += GetWeight(entry, waveNumber);
        }

        if (totalWeight <= 0f) return -1;

        float roll = Random.Range(0f, totalWeight);
        int last = -1;
        for (int i = 0; i < _enemies.Length; i++)
        {
            if (!IsAvailable(_enemies[i], waveNumber)) continue;

            last = i;
            roll -= GetWeight(_enemies[i], waveNumber);
            if (roll < 0f) return i;
        }

        return last; // 부동소수 오차 대비
    }

    /// <summary>스폰 후보 여부 - 프리팹 · 가중치 · 등장 웨이브(테스트 토글 시 무시)</summary>
    private bool IsAvailable(EnemyEntry entry, int waveNumber) =>
        entry.prefab != null && GetWeight(entry, waveNumber) > 0f && (_isIgnoringWaveGates || waveNumber >= entry.minWave);

    private float GetWeight(EnemyEntry entry, int waveNumber)
    {
        float weight = waveNumber >= 30 ? entry.lateWeight : entry.weight;
        return _isIgnoringWaveGates && weight > 0f ? 1f : weight; // 테스트 모드 = 균등
    }
}

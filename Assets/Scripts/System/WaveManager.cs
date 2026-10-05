using System.Collections;
using UnityEngine;

/// <summary>
/// 웨이브 1개의 설정값
/// </summary>

[System.Serializable]
public struct WaveData
{
    [Tooltip("웨이브에 보낼 적 수")] 
    public int totalCount;

    [Tooltip("생성 간격 최소")] 
    public float spawnIntervalMin;
    
    [Tooltip("생성 간격 최대")] 
    public float spawnIntervalMax;
}

/// <summary>
/// 웨이브 진행
/// 시작 확장·상점 → 스폰 완료제 - 마지막 적을 내보내면 상점을 열고, 전투 시작으로 다음 웨이브 진행(남은 적 유지)
/// </summary>

public class WaveManager : MonoBehaviour
{
    // IEnumerator   "중단·재개가 가능한 함수"의 반환형
    // yield         "여기서 멈추고 실행권을 넘긴다"

    [SerializeField] private EnemySpawner _spawner;
    [SerializeField] private WaveData[] _waves;

    [Header("보스 웨이브")]
    [SerializeField] private GameObject _lil1riBossPrefab;
    [SerializeField] private GameObject _youngchunBossPrefab;

    [Tooltip("첫 영춘(20웨이브) 체력 배율 - 초보 장벽 완화, 이후 반복 등장은 그대로")]
    [SerializeField] [Range(0.1f, 1f)] private float _firstYoungchunHpMultiplier = 0.6f;

    [SerializeField] private PlayerHitFeedback _playerHitFeedback;

    [Tooltip("보스 등장 경고 시간(초) - 게임 시간 기준")]
    [SerializeField] [Min(0.1f)] private float _bossWarningDuration = 1.2f;

    [Tooltip("웨이브 종료 동시 레벨업 대기용")]
    [SerializeField] private LevelUpPanel _levelUpPanel;

    [SerializeField] private ShopPanel _shopPanel;

    [Header("1웨이브 카운트다운")]
    [SerializeField] private PopupText _countdownText;

    [Tooltip("카운트다운 시작 숫자 (3 → 3·2·1)")]
    [SerializeField] private int _countdownFrom = 3;

    [Tooltip("숫자 1개 표시 시간(초) - 게임 시간 기준")]
    [SerializeField] private float _countdownStepDuration = 0.65f;

    [Tooltip("게임오버 시 상점 생략 판정용")]
    [SerializeField] private Health _playerHealth;

    [Header("인벤토리 확장")]
    [SerializeField] private InventoryExpandPanel _expandPanel;
    [SerializeField] private InventoryBoard _inventoryBoard;

    [Tooltip("확장 포인트 지급 간격(웨이브) - 이 배수 웨이브 클리어 시 지급")]
    [SerializeField] private int _expandRewardInterval = 5;

    [Tooltip("1회 지급 확장 포인트")]
    [SerializeField] private int _expandRewardPoints = 4;

    [Tooltip("테스트용 시작 확장 포인트")]
    [SerializeField] private int _startExpandPoints;

    [Tooltip("테스트용 시작 웨이브 - 0 = 끔 (예: 10 = 릴1리, 20 = 영춘)")]
    [SerializeField] [Min(0)] private int _debugStartWave;

    [Tooltip("테스트용 보스만 - 켜면 보스 웨이브에서 일반 적 없이 보스만 등장")]
    [SerializeField] private bool _debugBossOnly;

    [Tooltip("테스트·영상용 조기 보스 - 릴1리 4웨이브, 영춘 5웨이브")]
    [SerializeField] private bool _debugEarlyBossWaves;

    private int _expandPoints; // 다음 확장 창에서 쓸 포인트
    private bool _hasWaveError;
    private bool _isBossWarningActive;
    private PlayerScore _playerScore;

    public int CurrentWaveNumber { get; private set; }

    private int Lil1riWaveNumber => _debugEarlyBossWaves ? 4 : 10;
    private int YoungchunWaveNumber => _debugEarlyBossWaves ? 5 : 20;

    /// <summary>첫 카운트다운 종료 - 스킬 사용 가능 기준</summary>
    public bool IsBattleStarted { get; private set; }

    /// <summary>정지 창(확장·상점·레벨업) 열림 여부 - 닫힐 때 각 창이 재개</summary>
    public bool IsPausePanelOpen => _expandPanel.IsOpen || _shopPanel.IsOpen || _levelUpPanel.IsOpen;

    /// <summary>웨이브 루프 시작 </summary>
    private void Start()
    {
        _expandPoints = _startExpandPoints + _inventoryBoard.StartExpansionPoints;
        _playerScore = _playerHealth.GetComponent<PlayerScore>();
        StartCoroutine(RunWaves()); // 등록만 하고 즉시 리턴
    }

    private void OnDisable()
    {
        if (!_isBossWarningActive) return;
        StopAllCoroutines();
        StopBossWarning();
    }

    /// <summary>웨이브를 끝없이 진행 - 구간별 스폰 설정 적용</summary>
    private IEnumerator RunWaves()
    {
        if (_waves.Length == 0)
        {
            Debug.LogError("WaveManager: 웨이브 배열이 비었다. 인스펙터에서 채워라");
            yield break; // 코루틴 즉시 종료
        }

        if (!IsBossConfigured(_lil1riBossPrefab, Lil1riWaveNumber) || !IsBossConfigured(_youngchunBossPrefab, YoungchunWaveNumber))
            yield break;

        // 시작 확장 포인트 소모 → 상점 → 첫 전투
        int index = Mathf.Max(_debugStartWave - 1, 0);
        CurrentWaveNumber = index + 1;
        if (_expandPoints > 0)
        {
            _expandPanel.Open(_expandPoints);
            _expandPoints = 0;
            yield return new WaitUntil(() => !_expandPanel.IsOpen);
        }
        _shopPanel.Open(CurrentWaveNumber);
        yield return new WaitUntil(() => !_shopPanel.IsOpen);

        // 첫 전투 시작만 3·2·1 - 끝나야 스폰 시작
        for (int n = _countdownFrom; n > 0; n--)
        {
            yield return _countdownText.Play(n.ToString(), _countdownStepDuration);
        }
        IsBattleStarted = true;

        while (true)
        {
            CurrentWaveNumber = index + 1;
            WaveData wave = GetWaveData(CurrentWaveNumber);
            yield return RunWave(wave); // 스폰 완료 + 상점 종료까지 대기

            if (_playerHealth.IsDead || _hasWaveError) yield break; // 게임오버·설정 오류 시 진행 중단

            index++;
        }
    }

    /// <summary>웨이브 1개 - 스폰 완료 → (레벨업 대기) → (확장 창) → 상점</summary>
    private IEnumerator RunWave(WaveData wave)
    {
        Debug.Log($"WAVE {CurrentWaveNumber} 시작 - 적 {wave.totalCount}마리");

        bool hasLil1ri = ShouldSpawnLil1ri(CurrentWaveNumber);
        bool hasYoungchun = ShouldSpawnYoungchun(CurrentWaveNumber);
        bool isBossWave = hasLil1ri || hasYoungchun;
        int spawnCount = _debugBossOnly && isBossWave ? 0 : wave.totalCount;

        for (int i = 0; i < spawnCount; i++)
        {
            if (_playerHealth.IsDead) yield break;
            _spawner.SpawnOne(CurrentWaveNumber);
            float nextSpawnTime = Time.time + Random.Range(wave.spawnIntervalMin, wave.spawnIntervalMax);
            yield return new WaitUntil(() => _playerHealth.IsDead || Time.time >= nextSpawnTime);
        }

        if (_playerHealth.IsDead) yield break;

        if (isBossWave)
        {
            yield return RunBosses(hasLil1ri, hasYoungchun);
            if (_playerHealth.IsDead || _hasWaveError) yield break;
        }

        // 일반 웨이브는 스폰 완료, 보스 웨이브는 보스 사망 연출 완료 후 전환
        yield return new WaitUntil(() => _playerHealth.IsDead || !_levelUpPanel.IsOpen); // 동시 레벨업 대기

        if (_playerHealth.IsDead) yield break; // 게임오버 - 상점 생략

        _playerScore.AddScore(CurrentWaveNumber * 500); // 웨이브 완료 점수

        if (_expandRewardInterval > 0 && CurrentWaveNumber % _expandRewardInterval == 0)
        {
            _expandPoints += _expandRewardPoints;
            Debug.Log($"WAVE {CurrentWaveNumber} 클리어 보상 - 확장 포인트 +{_expandRewardPoints}");
        }

        if (_expandPoints > 0)
        {
            Debug.Log($"WAVE {CurrentWaveNumber} 스폰 완료 - 확장 창 오픈 (포인트 {_expandPoints})");
            _expandPanel.Open(_expandPoints);
            _expandPoints = 0; // 확장 창에서 전부 소모
            yield return new WaitUntil(() => !_expandPanel.IsOpen); // 확장 완료까지 대기
        }

        Debug.Log($"WAVE {CurrentWaveNumber} 스폰 완료 - 상점 오픈");
        _shopPanel.Open(CurrentWaveNumber);
        yield return new WaitUntil(() => !_shopPanel.IsOpen); // 전투 시작까지 대기
    }

    /// <summary>필수 보스 구성 확인</summary>
    private bool IsBossConfigured(GameObject prefab, int waveNumber)
    {
        if (prefab != null && prefab.GetComponent<EnemyMover>() != null &&
            prefab.GetComponent<Health>() != null && prefab.GetComponent<BossController>() != null)
            return true;

        Debug.LogError($"WaveManager: W{waveNumber} 보스 프리팹·EnemyMover·Health·BossController 설정 누락", this);
        return false;
    }

    private WaveData GetWaveData(int waveNumber)
    {
        if (waveNumber >= 60) return new WaveData { totalCount = 38, spawnIntervalMin = 0.20f, spawnIntervalMax = 0.40f };
        if (waveNumber >= 30) return new WaveData { totalCount = 30, spawnIntervalMin = 0.20f, spawnIntervalMax = 0.45f };
        if (waveNumber >= 10) return new WaveData { totalCount = 24, spawnIntervalMin = 0.22f, spawnIntervalMax = 0.50f };
        return _waves[Mathf.Min(waveNumber - 1, _waves.Length - 1)];
    }

    private bool ShouldSpawnLil1ri(int waveNumber) => _debugEarlyBossWaves
        ? waveNumber == Lil1riWaveNumber
        : waveNumber % 20 == 10 || waveNumber % 100 == 0;

    private bool ShouldSpawnYoungchun(int waveNumber) => _debugEarlyBossWaves
        ? waveNumber == YoungchunWaveNumber
        : waveNumber % 20 == 0;

    /// <summary>일반 적 스폰 종료 후 모든 보스 생성·사망 연출 대기</summary>
    private IEnumerator RunBosses(bool hasLil1ri, bool hasYoungchun)
    {
        yield return RunBossWarning();
        if (_playerHealth.IsDead) yield break;

        int count = hasLil1ri && hasYoungchun ? 2 : 1;
        EnemyMover[] bosses = new EnemyMover[count];
        Health[] health = new Health[count];
        bool[] hasDied = new bool[count];
        int index = 0;
        if (hasLil1ri) bosses[index++] = _spawner.SpawnBoss(_lil1riBossPrefab, CurrentWaveNumber, count == 2 ? -1.125f : 0f);
        if (hasYoungchun)
        {
            bosses[index] = _spawner.SpawnBoss(_youngchunBossPrefab, CurrentWaveNumber, count == 2 ? 1.125f : 0f);
            if (CurrentWaveNumber == YoungchunWaveNumber) ApplyFirstYoungchunHp(bosses[index]);
        }
        for (int i = 0; i < count; i++) health[i] = bosses[i].GetComponent<Health>();

        while (!_playerHealth.IsDead)
        {
            bool allFinished = true;
            for (int i = 0; i < count; i++)
            {
                if (!hasDied[i])
                {
                    if (bosses[i] == null || health[i] == null)
                    {
                        _hasWaveError = true;
                        Debug.LogError($"WaveManager: W{CurrentWaveNumber} 보스가 사망 확인 전에 제거됨", this);
                        yield break;
                    }
                    hasDied[i] = health[i].IsDead || bosses[i].IsDying;
                }
                if (bosses[i] != null) allFinished = false;
            }
            if (allFinished) yield break;
            yield return null;
        }
    }

    /// <summary>보스 등장 직전 WARNING · 붉은 비네트</summary>
    /// <summary>첫 영춘만 체력 감소 - 현재 체력도 새 최대치로</summary>
    private void ApplyFirstYoungchunHp(EnemyMover boss)
    {
        Health health = boss.GetComponent<Health>();
        int reduce = Mathf.RoundToInt(health.MaxHp * (1f - _firstYoungchunHpMultiplier));
        health.AdjustMaxHp(-reduce);
    }

    private IEnumerator RunBossWarning()
    {
        _isBossWarningActive = true;
        _playerHitFeedback.PlayBossWarning(_bossWarningDuration);
        IEnumerator popup = _countdownText.Play("<color=#FF3333><size=64>WARNING</size></color>", _bossWarningDuration);
        while (!_playerHealth.IsDead && popup.MoveNext())
            yield return popup.Current;
        StopBossWarning();
    }

    private void StopBossWarning()
    {
        _countdownText.Hide();
        _playerHitFeedback.StopBossWarning();
        _isBossWarningActive = false;
    }
}

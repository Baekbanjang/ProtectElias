using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMover : MonoBehaviour
{
    private const double EarlyHpGrowthPerWave = 1.04d; // 1~30웨이브 - 초반 완만
    private const double LateHpGrowthPerWave = 1.08d;  // 31웨이브 이후 - 후반 가파름
    private const int HpGrowthBreakWave = 30;
    private const double RewardGrowthPerWave = 0.05d;
    private const float StunReapplyCooldown = 1.5f;
    // 적 리스트 - static을 활용하여 클래스 전체에 하나 - readonly는 const 포인터 역할 - 가리키는 대상 고정해서 변경 불가
    public static readonly List<EnemyMover> Active = new List<EnemyMover>();

    [SerializeField] private float _speed = 1.0f;
    
    [Tooltip("디펜스 라인")] 
    [SerializeField] private float _defenseLineY = -1.719f;
    
    [SerializeField] private int _breachDamage = 5;
    
    [Tooltip("처치 시 EXP")] [SerializeField] private int _expDrop = 1;

    [Tooltip("처치 시 골드")] [SerializeField] private int _goldDrop = 1;

    [Tooltip("처치 시 점수")] [SerializeField] private int _scoreDrop = 10;

    [Header("방어선 공격")] [Tooltip("체크 = 멈춰서 때리기 / 해제 = 닿으면 소멸")] 
    [SerializeField] private bool _stopsAtLine = true;
    [Tooltip("돌파 피해 후 사망 애니메이션 재생")] [SerializeField] private bool _hasBreachDeathAnimation;

    [Tooltip("방어선 위 공격 정지 거리(유닛)")] [SerializeField] [Min(0f)] private float _attackStopDistance;
    [Tooltip("원거리 공격 탄 — 비어 있으면 직접 피해")] [SerializeField] private EnemyProjectile _attackProjectile;
    [Tooltip("적 탄속(유닛/초)")] [SerializeField] [Min(0.01f)] private float _projectileSpeed = 6f;
    [Tooltip("탄 발사 위치 — 월드 좌표 오프셋")] [SerializeField] private Vector2 _projectileSpawnOffset;
    
    [Tooltip("사망 애니메이션 길이(초)")] [SerializeField]
    private float _deathDuration = 0.44f;
    [Tooltip("사망 연출 크기 배율")] [SerializeField] [Min(0.01f)] private float _deathAnimationScale = 1f;

    [Tooltip("사망 히트스톱(초) - 이 적의 애니메이션만 정지")] [SerializeField]
    private float _hitStopDuration = 0.04f;

    [Header("상태 색")]
    [Tooltip("화상 중 몸 색")] [SerializeField] private Color _burnTint = new Color(1f, 0.55f, 0.2f);
    [Tooltip("화상 피해 틱 번쩍 색")] [SerializeField] private Color _burnFlashColor = new Color(1f, 0.3f, 0f);
    [Tooltip("감전 중 몸 색 - 화상보다 우선")] [SerializeField] private Color _stunTint = new Color(0.55f, 0.9f, 1f);
    [Tooltip("상태 색 섞는 정도(0~1)")] [SerializeField] [Range(0f, 1f)] private float _tintStrength = 0.5f;
    [Tooltip("틱 번쩍 섞는 정도(0~1)")] [SerializeField] [Range(0f, 1f)] private float _flashStrength = 0.85f;
    [Tooltip("틱 번쩍 시간(초)")] [SerializeField] private float _burnFlashDuration = 0.1f;

    private Health _playerHealth;
    private PlayerLevel _playerLevel;
    private PlayerStats _playerStats;
    private PlayerGold _playerGold;
    private PlayerScore _playerScore;
    private Animator _animator;
    private BossController _bossController;
    private Collider2D _collider;
    private bool _isDying;

    private bool _isAttacking;

    // 화상·감전 상태
    private Health _health;
    private float _burnRemaining;
    private float _burnTickTimer;
    private int _burnDamagePerSecond;
    private float _stunRemaining;
    private float _nextStunAllowedTime;
    private bool _isSpawnInitialized;
    private float _burnFlashRemaining;
    private SpriteRenderer _spriteRenderer;
    private Color _baseColor; // 프리팹 원래 색
    private bool _isTinted;

    // 사망 연출 중 여부
    public bool IsDying => _isDying;
    public bool IsAttacking => _isAttacking;
    public bool IsStunned => _stunRemaining > 0f;
    public int SpawnWaveNumber { get; private set; } = 1;

    /// <summary>피격 판정 범위 - 스킬 창 줄 판정용</summary>
    public Bounds HitBounds => _collider.bounds;


    /// 적을 껏다 켜서 재사용
    /// <summary>리스트 등록 - 활성화 시점 </summary>
    private void OnEnable() // Awake 직후 + SetActive(true) 시점
    {
        Active.Add(this);
    }

    /// <summary>리스트 해제 - 비활성/파괴 시점</summary>
    private void OnDisable() //SetActive(false) + Destroy 직전
    {
        Active.Remove(this);
    }

    /// <summary>컴포넌트 캐싱</summary>
    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _bossController = GetComponent<BossController>();
        _collider = GetComponent<Collider2D>();
        _health = GetComponent<Health>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_spriteRenderer != null) _baseColor = _spriteRenderer.color;
    }

    /// <summary>생성 웨이브 기준 체력·골드·점수 적용 - EXP 유지</summary>
    public void InitializeForWave(int waveNumber)
    {
        if (_isSpawnInitialized) return;
        _isSpawnInitialized = true;
        SpawnWaveNumber = Mathf.Max(1, waveNumber);
        int waveIndex = SpawnWaveNumber - 1;
        int earlySteps = Mathf.Min(SpawnWaveNumber, HpGrowthBreakWave) - 1;
        int lateSteps = Mathf.Max(SpawnWaveNumber - HpGrowthBreakWave, 0);
        double hp = _health.MaxHp * System.Math.Pow(EarlyHpGrowthPerWave, earlySteps) * System.Math.Pow(LateHpGrowthPerWave, lateSteps);
        int maxHp = (int)System.Math.Min(int.MaxValue, System.Math.Round(hp, System.MidpointRounding.AwayFromZero));
        _health.IncreaseMaxHp(maxHp - _health.MaxHp);
        double rewardMultiplier = 1d + RewardGrowthPerWave * waveIndex;
        _goldDrop = (int)System.Math.Round(_goldDrop * rewardMultiplier, System.MidpointRounding.AwayFromZero);
        _scoreDrop = (int)System.Math.Round(_scoreDrop * rewardMultiplier, System.MidpointRounding.AwayFromZero);
    }

    /// <summary>플레이어 Health 캐싱</summary>
    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        _playerHealth = player.GetComponent<Health>();
        _playerLevel = player.GetComponent<PlayerLevel>();
        _playerStats = player.GetComponent<PlayerStats>();
        _playerGold = player.GetComponent<PlayerGold>();
        _playerScore = player.GetComponent<PlayerScore>();
    }

    /// <summary>방어선 도달 확인</summary>
    void Update()
    {
        if (_isDying) return;

        UpdateBurn();
        if (_isDying) return; // 화상으로 사망
        bool isStunned = UpdateStun();
        UpdateTint();
        if (isStunned) return;

        if (_isAttacking) return;

        // GameObject의 좌표를 아래방향(0,-1,0) 으로 델타 타임마다
        transform.position += Vector3.down * (_speed * Time.deltaTime);

        float attackLineY = _defenseLineY + (_stopsAtLine ? _attackStopDistance : 0f);
        if (transform.position.y <= attackLineY)
        {
            if (_stopsAtLine)
            {
                if (_attackStopDistance > 0f)
                {
                    Vector3 position = transform.position;
                    position.y = attackLineY;
                    transform.position = position;
                }
                StartAttacking();
            }
            else Breach();
        }
    }

    /// <summary>화상 - 지속 시간 갱신(중첩 없음), 1초마다 피해</summary>
    public void ApplyBurn(float duration, int damagePerSecond)
    {
        if (_isDying || _health == null) return;

        if (_burnRemaining <= 0f) _burnTickTimer = 1f; // 새로 걸릴 때만 틱 타이머 시작
        _burnRemaining = Mathf.Max(_burnRemaining, duration);
        _burnDamagePerSecond = Mathf.Max(_burnDamagePerSecond, damagePerSecond);
    }

    /// <summary>감전 - 재적용 대기, 보스 지속 시간 절반</summary>
    public void ApplyStun(float duration)
    {
        if (_isDying || duration <= 0f || Time.time < _nextStunAllowedTime) return;

        if (_bossController != null) duration *= 0.5f;
        _stunRemaining = Mathf.Max(_stunRemaining, duration);
        _nextStunAllowedTime = Time.time + StunReapplyCooldown;
        _animator.speed = 0f;
    }

    /// <summary>화상 틱 처리</summary>
    private void UpdateBurn()
    {
        if (_burnRemaining <= 0f) return;

        _burnRemaining -= Time.deltaTime;
        _burnTickTimer -= Time.deltaTime;

        if (_burnTickTimer <= 0f)
        {
            _burnTickTimer += 1f;
            _burnFlashRemaining = _burnFlashDuration;
            _health.TakeDamage(_burnDamagePerSecond);
        }

        if (_burnRemaining <= 0f) _burnDamagePerSecond = 0;
    }

    /// <summary>감전 처리 - 정지 중이면 true</summary>
    private bool UpdateStun()
    {
        if (_stunRemaining <= 0f) return false;

        _stunRemaining -= Time.deltaTime;
        if (_stunRemaining > 0f) return true;

        _animator.speed = 1f; // 감전 해제
        return false;
    }

    /// <summary>상태 색 - 감전 > 화상 틱 번쩍 > 화상, 끝나면 원래 색</summary>
    private void UpdateTint()
    {
        if (_spriteRenderer == null) return;

        _burnFlashRemaining -= Time.deltaTime;

        Color color;
        if (_stunRemaining > 0f) color = Color.Lerp(_baseColor, _stunTint, _tintStrength);
        else if (_burnFlashRemaining > 0f) color = Color.Lerp(_baseColor, _burnFlashColor, _flashStrength);
        else if (_burnRemaining > 0f) color = Color.Lerp(_baseColor, _burnTint, _tintStrength);
        else
        {
            if (_isTinted) ResetTint();
            return;
        }

        color.a = _baseColor.a;
        _spriteRenderer.color = color;
        _isTinted = true;
    }

    /// <summary>원래 색 복구</summary>
    private void ResetTint()
    {
        _spriteRenderer.color = _baseColor;
        _isTinted = false;
    }

    /// <summary>방어선 정지</summary>
    private void StartAttacking()
    {
        _isAttacking = true;
        if (_bossController == null) _animator.Play("attack");
    }

    /// <summary>공격 프레임</summary>
    private void OnAttackHit()
    {
        if (_isDying || _stunRemaining > 0f || _bossController != null) return;
        if (_attackProjectile != null)
        {
            EnemyProjectile projectile = Instantiate(_attackProjectile, transform.position + (Vector3)_projectileSpawnOffset, Quaternion.identity);
            projectile.Initialize(_playerHealth, _playerStats, _defenseLineY, _projectileSpeed, _breachDamage);
            return;
        }
        _playerHealth.TakeDamage(_playerStats.ReduceDefenseDamage(_breachDamage));
    }
    
    /// <summary>방어선 돌파 - 1회 피해 후 소멸 또는 사망 연출</summary>
    private void Breach()
    {
        if (_isDying) return;
        if (_hasBreachDeathAnimation) PrepareDeath();
        _playerHealth.TakeDamage(_playerStats.ReduceDefenseDamage(_breachDamage));
        if (_hasBreachDeathAnimation) StartCoroutine(DeathRoutine(false));
        else Despawn();
    }
    
    /// <summary>HP 0 진입 - Health.OnDied 에 인스펙터로 연결</summary>
    public void StartDying()
    {
        if (_isDying) return;
        PrepareDeath();
        
        _playerLevel.AddExp(_expDrop); // 처치 보상
        int gold = _bossController != null && _playerStats.HasBounty ? _goldDrop * 2 : _goldDrop;
        _playerGold.AddGold(gold);
        _playerScore.AddScore(_scoreDrop);
        _playerScore.AddKill();
        _playerStats.TryLifeStealOnKill(); // 흡혈

        StartCoroutine(DeathRoutine(true));
    }

    private void PrepareDeath()
    {
        _isDying = true;
        if (_bossController != null) _bossController.StopAttacks();
        Active.Remove(this);
        _collider.enabled = false;
        if (_isTinted) ResetTint(); // 사망 연출은 원래 색
    }

    /// <summary>히트스톱 → 사망 연출 → 소멸</summary>
    private IEnumerator DeathRoutine(bool hasHitStop)
    {
        if (hasHitStop)
        {
            _animator.speed = 0f; // 히트스톱 - 현재 프레임 정지
            yield return new WaitForSeconds(_hitStopDuration);
        }
        _animator.speed = 1f;

        _animator.Play("death");
        if (!Mathf.Approximately(_deathAnimationScale, 1f))
        {
            transform.localScale *= _deathAnimationScale;
            Transform shadow = transform.Find("Shadow");
            if (shadow != null)
            {
                shadow.localScale /= _deathAnimationScale;
                shadow.localPosition /= _deathAnimationScale;
            }
        }
        yield return new WaitForSeconds(_deathDuration);
        Despawn();
    }

    public void Despawn()
    {
       Destroy(gameObject);
    }
}

using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Pool;

/// <summary>액티브 스킬 - Space·SPC 버튼 발동, 컷인 동안 정지 → 줄마다 창 투척, 재사용 대기</summary>
[DefaultExecutionOrder(-50)] // 레벨업 창 Space 확정·재개보다 먼저 판정
public class SkillController : MonoBehaviour
{
    [Tooltip("캐릭터 목록 - 저장된 선택 id, 없으면 첫 번째")]
    [SerializeField] private CharacterData[] _characters;

    [SerializeField] private WaveManager _waveManager;
    [SerializeField] private GameOverHandler _gameOverHandler;
    [SerializeField] private GameSpeed _gameSpeed; // 컷인 후 현재 배속으로 재개
    [SerializeField] private SkillCutin _cutin;

    [Tooltip("창·잔상·명중 섬광 부모")]
    [SerializeField] private Transform _effectRoot;

    [Tooltip("전투 영역 반폭(유닛) - 줄 균등 분할 기준")]
    [SerializeField] private float _playAreaHalfWidth = 2.8125f;

    [Tooltip("창 출발 높이 - 본체 발밑 기준(유닛)")]
    [SerializeField] private float _launchOffsetY = 0.8f;

    [Tooltip("피해 태그 - 공격력·해당 태그 특성 적용")]
    [SerializeField] private AttackType _damageType = AttackType.Pierce;

    private PlayerStats _stats;
    private Health _health;
    private float _cooldownRemaining;

    private ObjectPool<SkillSpear> _spearPool;
    private ObjectPool<OneShotSpriteFx> _trailPool;
    private ObjectPool<OneShotSpriteFx> _impactPool;

    public CharacterData Character { get; private set; }
    public float Cooldown => Character.SkillCooldown;
    public float CooldownRemaining => Mathf.Max(0f, _cooldownRemaining);
    public bool IsCasting { get; private set; } // 컷인 재생 중

    /// <summary>사용 불가 상태 - 카운트다운·게임오버·정지(상점·레벨업·일시정지 등). 컷인 중은 제외</summary>
    public bool IsBlocked => !_waveManager.IsBattleStarted || _gameOverHandler.IsGameOver || _health.IsDead
                             || (!IsCasting && Mathf.Approximately(Time.timeScale, 0f));

    public bool CanActivate => !IsCasting && _cooldownRemaining <= 0f && !IsBlocked;

    /// <summary>활성 창·잔상·명중 섬광 수 - 검증용</summary>
    public int ActiveEffectCount => _spearPool.CountActive + _trailPool.CountActive + _impactPool.CountActive;

    /// <summary>선택 캐릭터 결정, 풀 구성</summary>
    private void Awake()
    {
        _stats = GetComponent<PlayerStats>();
        _health = GetComponent<Health>();
        Character = CharacterData.FindSelected(_characters);

        SkillSpear spearPrefab = Character.SkillSpearPrefab;
        _spearPool = new ObjectPool<SkillSpear>(
            () => { SkillSpear spear = Instantiate(spearPrefab, _effectRoot); spear.SetPool(_spearPool); return spear; },
            spear => spear.gameObject.SetActive(true),
            spear => spear.gameObject.SetActive(false));
        _trailPool = CreateFxPool(spearPrefab.TrailPrefab);
        _impactPool = CreateFxPool(spearPrefab.ImpactPrefab);
    }

    private void OnEnable() => Projectile.EnemyDamaged += OnEnemyDamaged;
    private void OnDisable() => Projectile.EnemyDamaged -= OnEnemyDamaged;

    /// <summary>무기 명중 - 성배 아티팩트만큼 재사용 대기 감소, 0 미만 불가</summary>
    private void OnEnemyDamaged()
    {
        if (_stats.SkillCooldownPerHit <= 0f || _cooldownRemaining <= 0f) return;

        _cooldownRemaining = Mathf.Max(0f, _cooldownRemaining - _stats.SkillCooldownPerHit);
    }

    /// <summary>재사용 대기 감산, Space 입력</summary>
    private void Update()
    {
        _cooldownRemaining -= Time.deltaTime;

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) TryActivate();
    }

    /// <summary>발동 - SPC 버튼·Space</summary>
    public void TryActivate()
    {
        if (!CanActivate) return;
        StartCoroutine(CastRoutine());
    }

    /// <summary>정지 → 컷인(실제 시간) → 배속 복귀 → 정지 창 닫힘 대기 → 투척</summary>
    private IEnumerator CastRoutine()
    {
        IsCasting = true;
        _cooldownRemaining = Cooldown;
        Time.timeScale = 0f;

        yield return _cutin.Play(Character.SkillCutinFrames);

        if (!_waveManager.IsPausePanelOpen) _gameSpeed.ResumeTime(); // 컷인 중 열린 창은 닫힐 때 재개
        IsCasting = false;

        yield return new WaitUntil(() => !_waveManager.IsPausePanelOpen); // 정지 창 중 창 판정 방지 - 닫힌 뒤 투척
        LaunchSpears();
    }

    /// <summary>줄마다 창 1자루 - 전투 영역 균등 분할 중심</summary>
    private void LaunchSpears()
    {
        int laneCount = Character.SkillLaneCount;
        float laneWidth = _playAreaHalfWidth * 2f / laneCount;
        float startY = transform.position.y + _launchOffsetY;
        int damage = Mathf.RoundToInt(Character.SkillDamage * _stats.GetDamageMultiplier(_damageType));

        for (int i = 0; i < laneCount; i++)
        {
            float laneX = -_playAreaHalfWidth + laneWidth * (i + 0.5f);
            _spearPool.Get().Launch(this, new Vector2(laneX, startY), damage);
        }
        Debug.Log($"스킬 {Character.SkillName} - 창 {laneCount}자루, 피해 {damage}");
    }

    /// <summary>이동 잔상 1회</summary>
    public void SpawnTrail(Vector2 position) => _trailPool.Get().Play(position);

    /// <summary>명중 섬광 1회</summary>
    public void SpawnImpact(Vector2 position) => _impactPool.Get().Play(position);

    private ObjectPool<OneShotSpriteFx> CreateFxPool(OneShotSpriteFx prefab)
    {
        ObjectPool<OneShotSpriteFx> pool = null;
        pool = new ObjectPool<OneShotSpriteFx>(
            () => { OneShotSpriteFx fx = Instantiate(prefab, _effectRoot); fx.SetPool(pool); return fx; },
            fx => fx.gameObject.SetActive(true),
            fx => fx.gameObject.SetActive(false));
        return pool;
    }
}

using UnityEngine;

/// <summary>
/// 무기 - 쿨다운마다 타겟 방향으로 자동 발사, 던지기 애니가 있으면 릴리스 이벤트에서 발사(소환체)
/// </summary>
[RequireComponent(typeof(TargetFinder))]
public class Weapon : MonoBehaviour
{
    [SerializeField] private ProjectilePool _pool;

    [SerializeField] private WeaponData _data;

    [Tooltip("총구 위치 - 플레이어 기준 오프셋")]
    [SerializeField] private Vector2 _muzzleOffset = new Vector2(0f, 0.8f);

    [Tooltip("던지기 애니 - 지정 시 OnThrowRelease 이벤트에서 발사 (소환체 전용)")]
    [SerializeField] private Animator _throwAnimator;

    [Tooltip("throw 애니 길이(초) - 발사 간격이 더 짧으면 재생 가속")]
    [SerializeField] private float _throwDuration = 0.73f;

    private static readonly int s_throwStateHash = Animator.StringToHash("throw");
    private static readonly int s_throwSpeedHash = Animator.StringToHash("ThrowSpeed");

    private TargetFinder _targetFinder;
    private PlayerStats _stats;
    private float _cooldownRemaining;
    private float _gradeMultiplier = 1f; // 등급 피해 배율
    private int _burstRemaining; // 점사 남은 발 수
    private float _burstTimer;
    private float _phase; // 박자 위치(0~1) - 무기끼리 첫 발 분산
    private bool _isWaitingTarget = true;

    public WeaponData Data => _data;
    public TargetFinder TargetFinder => _targetFinder;

    /// <summary>실제 발사 시 호출 - 본체 공격 애니 연결</summary>
    public event System.Action Fired;

    /// <summary>런타임 장착 - 데이터·등급 배율·풀·스탯·전용 탐색기 지정, 소환체면 애니·출발 위치 적용</summary>
    public void Setup(WeaponData data, float gradeMultiplier, ProjectilePool pool, PlayerStats stats, TargetFinder targetFinder = null)
    {
        _data = data;
        _gradeMultiplier = gradeMultiplier;
        _pool = pool;
        _stats = stats;
        _burstRemaining = 0;
        if (targetFinder != null) _targetFinder = targetFinder; // 본체 무기 - 무기마다 사거리 분리
        if (_targetFinder == null) _targetFinder = GetComponent<TargetFinder>();
        _targetFinder.Range = data.Range;

        if (_throwAnimator != null)
        {
            _throwAnimator.runtimeAnimatorController = data.SummonAnimator;
            _muzzleOffset = data.ReleaseOffset;
        }
    }

    /// <summary>박자 위치 지정(0~1) - 대기 후 첫 발을 주기 × 위치만큼 늦춤</summary>
    public void SetPhase(float phase) => _phase = phase;

    /// <summary>머지 등급 배율 갱신</summary>
    public void SetGradeMultiplier(float gradeMultiplier) => _gradeMultiplier = gradeMultiplier;

    private void Awake()
    {
        _targetFinder = GetComponent<TargetFinder>();
        if (_data != null) _targetFinder.Range = _data.Range;
    }

    /// <summary>쿨다운 감산 후 발사 판정</summary>
    private void Update()
    {
        _cooldownRemaining -= Time.deltaTime;
        if (_burstRemaining > 0)
        {
            UpdateBurst();
            return;
        }
        if (_cooldownRemaining > 0f) return;

        // 타켓 없을 시 쿨다운 소비 X
        EnemyMover target = _targetFinder.CurrentTarget;
        if (target == null)
        {
            _isWaitingTarget = true;
            return;
        }

        float cooldown = _data.FireInterval * _stats.GetCooldownMultiplier(_data.AttackType);

        // 대기 후 첫 발 - 무기마다 박자 위치만큼 늦춰 동시 발사 방지
        if (_isWaitingTarget)
        {
            _isWaitingTarget = false;
            _cooldownRemaining = cooldown * _phase;
            if (_cooldownRemaining > 0f) return;
        }

        if (_throwAnimator != null)
        {
            PlayThrow(cooldown);
        }
        else
        {
            Fire(target);
            StartBurst();
        }
        _cooldownRemaining = cooldown;
    }

    /// <summary>멀티샷 추가 발 수 - 소환 무기 제외</summary>
    private int GetMultishotBonus() => _data.AttackType == AttackType.Summon ? 0 : _stats.MultishotBonus;

    /// <summary>점사 발 수 - 투사체+1 특성은 점사 무기면 여기에 더함</summary>
    private int GetBurstCount() => _data.BurstCount > 1 ? _data.BurstCount + GetMultishotBonus() : 1;

    /// <summary>갈래 수 - 점사 무기가 아니면 투사체+1 특성을 여기에 더함</summary>
    private int GetSpreadCount() => _data.BurstCount > 1 ? _data.ProjectileCount : _data.ProjectileCount + GetMultishotBonus();

    /// <summary>첫 발 이후 남은 점사 예약</summary>
    private void StartBurst()
    {
        _burstRemaining = GetBurstCount() - 1;
        _burstTimer = _data.BurstInterval;
    }

    /// <summary>점사 남은 발 발사 - 타겟 없으면 취소</summary>
    private void UpdateBurst()
    {
        _burstTimer -= Time.deltaTime;
        if (_burstTimer > 0f) return;

        EnemyMover target = _targetFinder.CurrentTarget;
        if (target == null)
        {
            _burstRemaining = 0;
            return;
        }

        Fire(target);
        _burstRemaining--;
        _burstTimer += _data.BurstInterval;
    }

    /// <summary>throw 애니 재생 - 발사 간격 안에 끝나도록 가속</summary>
    private void PlayThrow(float cooldown)
    {
        _throwAnimator.SetFloat(s_throwSpeedHash, Mathf.Max(1f, _throwDuration / cooldown));
        _throwAnimator.Play(s_throwStateHash, 0, 0f);
    }

    /// <summary>애니 이벤트 - 투척물 발사</summary>
    public void OnThrowRelease()
    {
        EnemyMover target = _targetFinder.CurrentTarget;
        if (target != null) Fire(target);
    }

    /// <summary>피해·탄속 계산 후 총구에서 발사 - 수 N&gt;1 이면 부채꼴 분산, 착지형이면 포물선 발사</summary>
    private void Fire(EnemyMover target)
    {
        Fired?.Invoke();
        Vector2 muzzle = transform.TransformPoint(_muzzleOffset); // 소환체 축소 배율 반영

        // 피해 = 기본 x 등급 배율 x (1 + 공격력 + 태그 대미지 + 배수진), 치명타 시 x 치명타 배율
        float nonCritDamage = _data.Damage * _gradeMultiplier * _stats.GetDamageMultiplier(_data.AttackType);
        float damage = nonCritDamage;
        bool isCrit = Random.value < _stats.CritChance;
        if (isCrit) damage *= _stats.CritMultiplier;
        int damageAmount = Mathf.RoundToInt(damage);

        if (_data.IsLandingType)
        {
            FireLanding(target, muzzle, damageAmount, nonCritDamage);
            return;
        }

        Vector2 direction = (Vector2)target.transform.position - muzzle;
        float speed = _data.ProjectileSpeed * _stats.ProjectileSpeedMultiplier;
        int count = GetSpreadCount();
        float explosionRadius = _data.ExplosionRadius * _stats.GetExplosionRadiusMultiplier(_data.AttackType);

        // 화상 초당 피해 - 등급·공격 특성을 반영한 비치명 1발 데미지 비율
        int burnDamageAmount = Mathf.RoundToInt(nonCritDamage * _data.BurnDamageRatio);

        for (int i = 0; i < count; i++)
        {
            Vector2 shotDirection = ApplySpread(direction, i, count);
            Projectile projectile = _pool.Get(muzzle);
            projectile.Shoot(shotDirection, speed, damageAmount, _data.PierceCount, explosionRadius, _data.ProjectileColor, _data.ProjectileSprite);
            projectile.SetHitEffects(_data.BurnChance, _data.BurnDuration, burnDamageAmount, _data.StunChance, _data.StunDuration);
            projectile.SetVisual(_data, shotDirection);
        }
    }

    /// <summary>착지형(수류탄식) 발사 - 목표 발밑으로 포물선 이동 후 착지 폭발 + 웅덩이</summary>
    private void FireLanding(EnemyMover target, Vector2 muzzle, int damageAmount, float nonCritDamage)
    {
        Vector2 landingPosition = target.transform.position; // 적 발밑 기준(피벗)
        float explosionRadius = _data.ExplosionRadius * _stats.GetExplosionRadiusMultiplier(_data.AttackType);

        // 웅덩이 틱 피해 - 등급·공격 특성을 반영한 비치명 1발 데미지 비율
        int puddleDamageAmount = Mathf.RoundToInt(nonCritDamage * _data.PuddleTickDamageRatio);

        // 웅덩이 지속 0 = 웅덩이 없는 착지 폭발 (머핀)
        PuddlePool puddlePool = _data.PuddleDuration > 0f ? _pool.PuddlePool : null;

        int count = GetSpreadCount();
        for (int i = 0; i < count; i++)
        {
            Projectile projectile = _pool.Get(muzzle);
            projectile.ShootLanding(landingPosition, _data.FlightDuration, damageAmount, explosionRadius,
                puddlePool, _data.PuddleDuration, _data.PuddleTickInterval, puddleDamageAmount,
                _data.ProjectileColor, _data.ProjectileSprite);
            projectile.SetVisual(_data, landingPosition - muzzle);
        }
    }

    /// <summary>N발이면 확산각 안에서 균등 분배, 1발이면 직선</summary>
    private Vector2 ApplySpread(Vector2 direction, int index, int count)
    {
        if (count <= 1) return direction;

        float step = _data.SpreadAngle / (count - 1);
        float angle = -_data.SpreadAngle * 0.5f + step * index;
        return Quaternion.Euler(0f, 0f, angle) * direction;
    }
}

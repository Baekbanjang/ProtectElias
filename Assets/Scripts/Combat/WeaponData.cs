using UnityEngine;

/// <summary>
/// 무기 데이터
/// </summary>
[CreateAssetMenu(fileName = "WeaponData", menuName = "Scriptable Objects/WeaponData")]
public class WeaponData : GridItemData
{
    [SerializeField] private AttackType _attackType;

    [Tooltip("기본 피해")]
    [SerializeField] private int _damage = 10;

    [Tooltip("발사 간격(초)")]
    [SerializeField] private float _fireInterval = 0.5f;

    [Tooltip("사정거리(유닛) - 무기 위치에서 이 거리 안의 적만 겨냥·발사")]
    [SerializeField] private float _range = 6f;

    [Tooltip("기본 탄속")]
    [SerializeField] private float _projectileSpeed = 8f;

    [Tooltip("갈래 수 - 부채꼴로 동시 발사")]
    [SerializeField] private int _projectileCount = 1;

    [Tooltip("점사 발 수 - 1 이면 단발")]
    [SerializeField] [Min(1)] private int _burstCount = 1;

    [Tooltip("점사 발 사이 간격(초)")]
    [SerializeField] private float _burstInterval = 0.08f;

    [Tooltip("확산각(도) - 투사체 2개 이상일 때 적용")]
    [SerializeField] private float _spreadAngle = 15f;

    [Tooltip("관통 횟수")]
    [SerializeField] private int _pierceCount;

    [Tooltip("폭발 반경 - 0이면 폭발 없음")]
    [SerializeField] private float _explosionRadius;

    [Tooltip("투사체 색 - 무기 구분용 임시 틴트")]
    [SerializeField] private Color _projectileColor = Color.white;

    [Tooltip("투사체 스프라이트 - 비우면 프리팹 기본")]
    [SerializeField] private Sprite _projectileSprite;

    [Header("투사체 표시")]
    [Tooltip("투사체 배율 - 0 이면 프리팹 기본")]
    [SerializeField] private float _projectileScale;

    [Tooltip("진행 방향으로 회전 - 그림 위쪽 = 진행 방향")]
    [SerializeField] private bool _isFacingDirection;

    [Tooltip("비행 중 회전 속도(도/초) - 0 이면 프리팹 기본")]
    [SerializeField] private float _projectileSpinSpeed;

    [Tooltip("반복 프레임 - 2장 이상이면 순환 재생")]
    [SerializeField] private Sprite[] _projectileFrames;

    [Tooltip("반복 프레임 간격(초)")]
    [SerializeField] private float _projectileFrameInterval = 0.12f;

    [Tooltip("적 직접 명중 효과 - 비우면 없음")]
    [SerializeField] private OneShotSpriteFx _hitFx;

    [Tooltip("폭발 효과 - 비우면 없음")]
    [SerializeField] private OneShotSpriteFx _explosionFx;

    [Tooltip("폭발 효과 배율 - 피해 반경과 별개")]
    [SerializeField] private float _explosionFxScale = 1f;

    [Header("소환 전용")]
    [Tooltip("소환체 애니 - idle·throw 상태 컨트롤러")]
    [SerializeField] private RuntimeAnimatorController _summonAnimator;

    [Tooltip("투척물 출발 위치 - 소환체 발밑 기준, 축소 전 유닛")]
    [SerializeField] private Vector2 _releaseOffset;

    [Header("착지형 전용 (수류탄식 - 포물선 착지 후 웅덩이)")]
    [Tooltip("착지형 여부 - 켜면 목표 발밑에 포물선 착지 후 폭발+웅덩이 생성")]
    [SerializeField] private bool _isLandingType;

    [Tooltip("착지까지 비행 시간(초)")]
    [SerializeField] private float _flightDuration = 0.6f;

    [Tooltip("웅덩이 지속 시간(초)")]
    [SerializeField] private float _puddleDuration = 2f;

    [Tooltip("웅덩이 피해 틱 간격(초)")]
    [SerializeField] private float _puddleTickInterval = 0.5f;

    [Tooltip("웅덩이 틱 피해 - 비치명 1발 데미지 대비 비율")]
    [SerializeField] private float _puddleTickDamageRatio = 0.25f;

    [Header("명중 효과")]
    [Tooltip("화상 확률(0~1) - 0 이면 없음")]
    [SerializeField] [Range(0f, 1f)] private float _burnChance;

    [Tooltip("화상 지속(초)")]
    [SerializeField] private float _burnDuration = 3f;

    [Tooltip("화상 초당 피해 - 비치명 1발 데미지 대비 비율")]
    [SerializeField] private float _burnDamageRatio = 0.2f;

    [Tooltip("감전 확률(0~1) - 0 이면 없음")]
    [SerializeField] [Range(0f, 1f)] private float _stunChance;

    [Tooltip("감전 정지(초)")]
    [SerializeField] private float _stunDuration = 0.5f;

    public AttackType AttackType => _attackType;
    public int Damage => _damage;
    public float FireInterval => _fireInterval;
    public float Range => _range;
    public float ProjectileSpeed => _projectileSpeed;
    public int ProjectileCount => _projectileCount;
    public int BurstCount => _burstCount;
    public float BurstInterval => _burstInterval;
    public float SpreadAngle => _spreadAngle;
    public int PierceCount => _pierceCount;
    public float ExplosionRadius => _explosionRadius;
    public Color ProjectileColor => _projectileColor;
    public Sprite ProjectileSprite => _projectileSprite;
    public float ProjectileScale => _projectileScale;
    public bool IsFacingDirection => _isFacingDirection;
    public float ProjectileSpinSpeed => _projectileSpinSpeed;
    public Sprite[] ProjectileFrames => _projectileFrames;
    public float ProjectileFrameInterval => _projectileFrameInterval;
    public OneShotSpriteFx HitFx => _hitFx;
    public OneShotSpriteFx ExplosionFx => _explosionFx;
    public float ExplosionFxScale => _explosionFxScale;
    public RuntimeAnimatorController SummonAnimator => _summonAnimator;
    public Vector2 ReleaseOffset => _releaseOffset;
    public bool IsLandingType => _isLandingType;
    public float FlightDuration => _flightDuration;
    public float PuddleDuration => _puddleDuration;
    public float PuddleTickInterval => _puddleTickInterval;
    public float PuddleTickDamageRatio => _puddleTickDamageRatio;
    public float BurnChance => _burnChance;
    public float BurnDuration => _burnDuration;
    public float BurnDamageRatio => _burnDamageRatio;
    public float StunChance => _stunChance;
    public float StunDuration => _stunDuration;
}

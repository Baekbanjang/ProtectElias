using UnityEngine;

/// <summary>보스 단일 공격 — 릴1리 내려찍기·영춘 방망이 후려치기</summary>
[RequireComponent(typeof(EnemyMover), typeof(Health), typeof(Animator))]
public class BossController : MonoBehaviour
{
    public enum BossKind { Lil1ri, Youngchun }

    [SerializeField] private BossKind _kind;

    [Header("릴1리 — 임시 수치")]
    [SerializeField] [Min(1)] private int _hammerDamage = 20;
    [SerializeField] [Min(0.1f)] private float _hammerInterval = 3.5f;
    [SerializeField] [Min(0f)] private float _hammerWindup = 0.8f;
    [SerializeField] [Range(0.01f, 1f)] private float _enrageHpRatio = 0.3f;
    [SerializeField] [Range(0.1f, 1f)] private float _enrageIntervalMultiplier = 0.65f;

    [Header("흑화 영춘 — 임시 수치")]
    [SerializeField] [Min(1)] private int _batDamage = 15;
    [SerializeField] [Min(0.1f)] private float _batInterval = 3f;
    [SerializeField] [Min(0f)] private float _batWindup = 0.6f;

    private EnemyMover _mover;
    private Health _health;
    private Health _playerHealth;
    private PlayerStats _playerStats;
    private Animator _animator;
    private bool _isWindingUp;
    private float _cooldown;
    private float _windupRemaining;

    private void Awake()
    {
        _mover = GetComponent<EnemyMover>();
        _health = GetComponent<Health>();
        _animator = GetComponent<Animator>();
    }

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        _playerHealth = player.GetComponent<Health>();
        _playerStats = player.GetComponent<PlayerStats>();
    }

    private void Update()
    {
        if (_mover.IsDying || _health.IsDead || _playerHealth.IsDead)
        {
            StopAttacks();
            return;
        }
        if (!_mover.IsAttacking || _mover.IsStunned || Time.deltaTime <= 0f) return;

        float intervalMultiplier = _kind == BossKind.Lil1ri && _health.CurrentHp <= _health.MaxHp * _enrageHpRatio ? _enrageIntervalMultiplier : 1f;
        _cooldown -= Time.deltaTime / intervalMultiplier;
        if (_isWindingUp)
        {
            _windupRemaining -= Time.deltaTime;
            if (_windupRemaining > 0f) return;
            _isWindingUp = false;
            DealDamage(_kind == BossKind.Lil1ri ? _hammerDamage : _batDamage); // 공격 → 대기 복귀는 애니메이터 전환
            return;
        }

        if (_cooldown <= 0f)
        {
            _cooldown = _kind == BossKind.Lil1ri ? _hammerInterval : _batInterval;
            _isWindingUp = true;
            _windupRemaining = _kind == BossKind.Lil1ri ? _hammerWindup : _batWindup;
            PlayAnimation(_kind == BossKind.Lil1ri ? "hammer" : "bat", "attack");
        }
    }

    private void DealDamage(int damage)
    {
        if (_mover.IsDying || _health.IsDead || _mover.IsStunned || _playerHealth.IsDead) return;
        _playerHealth.TakeDamage(_playerStats.ReduceDefenseDamage(damage));
    }

    private void PlayAnimation(string state, string fallback)
    {
        int stateHash = Animator.StringToHash(state);
        if (!_animator.HasState(0, stateHash)) stateHash = Animator.StringToHash(fallback);
        if (_animator.HasState(0, stateHash)) _animator.Play(stateHash, 0, 0f);
    }

    /// <summary>예고 중인 보스 공격 취소</summary>
    public void StopAttacks()
    {
        _isWindingUp = false;
        _windupRemaining = 0f;
    }

    private void OnDisable() => StopAttacks();
}

using UnityEngine;

/// <summary>본체 애니 - 무기 발사 시 attack 재생, 끝나면 컨트롤러가 idle 로 복귀</summary>
[RequireComponent(typeof(Animator))]
public class PlayerBodyAnimator : MonoBehaviour
{
    [Tooltip("attack 재시작 허용 시점(초) - 발사·반동 자세 이후. 그 전 발사는 진행 중 모션으로 처리")]
    [SerializeField] private float _restartAfter = 0.27f;

    [SerializeField] private WeaponLoadout _loadout;

    private static readonly int s_attackHash = Animator.StringToHash("attack");

    private Animator _animator;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    private void OnEnable() => _loadout.WeaponFired += OnWeaponFired;
    private void OnDisable() => _loadout.WeaponFired -= OnWeaponFired;

    /// <summary>발사 반응 - idle 이거나 발사 자세가 지난 attack 이면 처음부터</summary>
    private void OnWeaponFired()
    {
        AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);
        bool isAttacking = state.shortNameHash == s_attackHash;
        if (isAttacking && state.normalizedTime * state.length < _restartAfter) return;

        _animator.Play(s_attackHash, 0, 0f);
    }
}

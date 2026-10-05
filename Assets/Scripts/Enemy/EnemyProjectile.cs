using UnityEngine;

/// <summary>적 직선탄 — 방어선 도달 시 플레이어 피해</summary>
public class EnemyProjectile : MonoBehaviour
{
    private Health _target;
    private PlayerStats _playerStats;
    private float _defenseLineY;
    private float _speed;
    private int _damage;

    /// <summary>발사 시 피해 대상·도착선·탄속 설정</summary>
    public void Initialize(Health target, PlayerStats playerStats, float defenseLineY, float speed, int damage)
    {
        _target = target;
        _playerStats = playerStats;
        _defenseLineY = defenseLineY;
        _speed = speed;
        _damage = damage;
    }

    private void Update()
    {
        transform.position += Vector3.down * (_speed * Time.deltaTime);
        if (transform.position.y > _defenseLineY) return;

        if (_target != null) _target.TakeDamage(_playerStats.ReduceDefenseDamage(_damage));
        Destroy(gameObject);
    }
}

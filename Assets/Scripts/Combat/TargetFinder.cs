using UnityEngine;

public class TargetFinder : MonoBehaviour
{
    // 현재 타겟
    public EnemyMover CurrentTarget { get; private set; } // 겟터 세터 느낌

    /// <summary>사정거리(유닛) - 무기가 지정. 이 거리 밖의 적은 겨냥하지 않음</summary>
    public float Range { get; set; } = float.MaxValue;

    /// <summary>
    /// 타겟 갱신. 현재 타겟이 살아있으면 유지하고, 사라졌을 때만 재탐색 
    /// </summary>

    private void Update()
    {
        if (CurrentTarget == null || CurrentTarget.IsDying || !IsInRange(CurrentTarget))
        {
            CurrentTarget = FindLowestEnemy();
        }

        if (CurrentTarget != null)
        {
            Debug.DrawLine(transform.position, CurrentTarget.transform.position, Color.red);
        }
    }

    /// <summary>
    /// 리스트에서 Y좌표 최소인 적을 선택. 방어선에 가장 근접한 적
    /// </summary>

    private EnemyMover FindLowestEnemy()
    {
        EnemyMover lowest =  null;
        float lowestY = float.MaxValue;

        foreach (EnemyMover enemy in EnemyMover.Active)
        {
            if (enemy.IsDying || !IsInRange(enemy)) continue;
            float y = enemy.transform.position.y; // EnemyMover타입의 적의 Y값
            if (y < lowestY) 
            {
                lowestY = y;
                lowest = enemy;
            }
        }

        return lowest;
    }

    private bool IsInRange(EnemyMover enemy) =>
        (enemy.transform.position - transform.position).sqrMagnitude <= Range * Range;
}

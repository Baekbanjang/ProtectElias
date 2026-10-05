using UnityEngine;

/// <summary>플레이어 점수 - 한 판 처치 점수·처치 수 누적</summary>
public class PlayerScore : MonoBehaviour
{
    public int Score { get; private set; }
    public int Kills { get; private set; } // 처치한 적 수

    /// <summary>점수 가산</summary>
    public void AddScore(int amount) => Score += amount;

    /// <summary>처치 수 +1</summary>
    public void AddKill() => Kills++;
}

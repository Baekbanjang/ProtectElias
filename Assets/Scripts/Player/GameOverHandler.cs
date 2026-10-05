using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// 플레이어 사망 반응 - GAME OVER 표시 후 결과창
/// </summary>
public class GameOverHandler : MonoBehaviour
{
    private const string NumberFormat = "N0"; // 천 단위 쉼표

    [SerializeField] private GameObject _resultPanel;
    [SerializeField] private WaveManager _waveManager;
    [SerializeField] private PlayerGold _playerGold;
    [SerializeField] private PlayerScore _playerScore;

    [Header("결과창 - BATTLE SUMMARY")]
    [SerializeField] private TMP_Text _waveText;
    [SerializeField] private TMP_Text _killText;
    [SerializeField] private TMP_Text _earnedGoldText;    // 총 획득 골드
    [SerializeField] private TMP_Text _spentGoldText;     // 사용한 골드
    [SerializeField] private TMP_Text _remainingGoldText; // 남은 골드

    [Header("결과창 - TOTAL POINT")]
    [SerializeField] private TMP_Text _scoreText;
    [SerializeField] private GameObject _newRecordBadge; // 최고 점수 갱신 시

    [Header("GAME OVER 표시")]
    [SerializeField] private PopupText _gameOverText;

    [Tooltip("GAME OVER 표시 후 결과창까지(초) - 실제 시간 기준")]
    [SerializeField] private float _gameOverDuration = 1.2f;

    /// <summary>게임오버 진행 중 - 일시정지 차단용</summary>
    public bool IsGameOver { get; private set; }

    /// <summary>UnityEvent 대상이므로 public 필수</summary>
    public void HandleGameOver()
    {
        if (IsGameOver) return; // 판당 1회 (저장 포함)
        IsGameOver = true;

        Time.timeScale = 0f; // 전투 정지

        int score = _playerScore.Score;
        bool isNewRecord = score > SaveData.BestScore; // 저장 전 최고 점수와 비교
        ShowSummary(score, isNewRecord);

        // 메타 누적은 번 골드가 아니라 판 종료 시 남은 골드 기준 (기획 결정)
        SaveData.SaveRun(score, _waveManager.CurrentWaveNumber, _playerGold.Gold);
        SubmitOnlineScore(score, _waveManager.CurrentWaveNumber);

        StartCoroutine(ShowResultRoutine());
    }

    /// <summary>온라인 랭킹 제출 - 실패해도 게임오버 진행</summary>
    private async void SubmitOnlineScore(int score, int wave)
    {
        RankingService ranking = RankingService.Instance;
        if (ranking == null || !ranking.IsReady || !ranking.HasNickname) return; // 오프라인·닉네임 없음 = 제출 안 함

        try
        {
            RankingService.SubmitResult result = await ranking.SubmitScoreAsync(score, wave);
            Debug.Log($"온라인 랭킹 제출 - ok={result.ok}, reason={result.reason}");
        }
        catch (System.Exception e) { Debug.LogWarning($"온라인 랭킹 제출 실패: {e.Message}"); }
    }

    /// <summary>결과창 수치 채우기</summary>
    private void ShowSummary(int score, bool isNewRecord)
    {
        int earned = _playerGold.TotalEarned;
        int remaining = _playerGold.Gold;

        _waveText.text = _waveManager.CurrentWaveNumber.ToString(NumberFormat);
        _killText.text = _playerScore.Kills.ToString(NumberFormat);
        _earnedGoldText.text = earned.ToString(NumberFormat);
        _spentGoldText.text = (earned - remaining).ToString(NumberFormat); // 골드 소비처 = 상점 새로고침뿐
        _remainingGoldText.text = remaining.ToString(NumberFormat);
        _scoreText.text = score.ToString(NumberFormat);
        _newRecordBadge.SetActive(isNewRecord);
    }

    /// <summary>GAME OVER 표시 → 결과창</summary>
    private IEnumerator ShowResultRoutine()
    {
        yield return _gameOverText.Play("GAME OVER", _gameOverDuration, false);
        _gameOverText.Hide();
        _resultPanel.SetActive(true);
    }
}

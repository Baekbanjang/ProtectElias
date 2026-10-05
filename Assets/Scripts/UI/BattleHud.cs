using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>HUD - 웨이브, 레벨, EXP바, 골드, 점수</summary>
public class BattleHud : MonoBehaviour
{
    [SerializeField] private WaveManager _waveManager;
    [SerializeField] private PlayerLevel _playerLevel;
    [SerializeField] private PlayerGold _playerGold;
    [SerializeField] private PlayerScore _playerScore;
    [SerializeField] private TMP_Text _waveText;
    [SerializeField] private TMP_Text _levelText;
    [SerializeField] private Image _expFill;
    [SerializeField] private TMP_Text _goldText;
    [SerializeField] private TMP_Text _scoreText;

    /// <summary>매 프레임 현재 값 반영</summary>
    private void Update()
    {
        _waveText.text = _waveManager.CurrentWaveNumber.ToString();
        _levelText.text = $"Lv.{_playerLevel.Level}";
        _expFill.fillAmount = (float)_playerLevel.CurrentExp / _playerLevel.ExpToNext;
        _goldText.text = _playerGold.Gold.ToString();
        _scoreText.text = _playerScore.Score.ToString(); // × 자리 = 점수 아이콘
    }
}

using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>일시정지 - ESC 버튼 / 재개 또는 포기하고 정산</summary>
public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject _pausePanel;
    [SerializeField] private GameOverHandler _gameOverHandler;
    [SerializeField] private GameSpeed _gameSpeed; // 재개 시 현재 배속
    [SerializeField] private SettingsPanel _settingsPanel;
    [SerializeField] private GuidePanel _guidePanel;
    
    private bool _isPaused;

    /// <summary>ESC 입력 감지 - 설정·가이드 창 열려 있으면 그 창만 닫기</summary>
    private void Update()
    {
        if (!Keyboard.current.escapeKey.wasPressedThisFrame) return;

        if (_settingsPanel.IsOpen) _settingsPanel.Close();
        else if (_guidePanel.IsOpen) _guidePanel.Close();
        else TogglePause();
    }

    public void TogglePause()
    {
        if (_gameOverHandler.IsGameOver) return; // 게임오버 연출·결과창 중 차단

        if (_isPaused) Resume();
        else Pause();
    }

    private void Pause()
    {
        // 레벨업, 게임오버로 이미 멈춘 상태면 무시
        if (Mathf.Approximately(Time.timeScale, 0f)) return; // 타임스케일이 0이면 반환

        _isPaused = true;
        Time.timeScale = 0f;
        _pausePanel.SetActive(true);
    }
    
    public void Resume()
    {
        _isPaused = false;
        _gameSpeed.ResumeTime();
        _pausePanel.SetActive(false);
    }

    /// <summary>일시정지 창 위에 설정 창 열기</summary>
    public void OpenSettings() => _settingsPanel.Open();

    /// <summary>일시정지 창 위에 가이드 창 열기</summary>
    public void OpenGuide() => _guidePanel.Open();

    public void GiveUp()
    {
        _isPaused = false;
        _pausePanel.SetActive(false);
        _gameOverHandler.HandleGameOver();
    }
}

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>타이틀 화면 - 보유 골드·최고 점수 표시, Enter/Space 캐릭터 선택(시작 = 첫 실행 가이드 먼저), 게임 종료</summary>
public class TitleMenu : MonoBehaviour
{
    private const float NicknameWaitSeconds = 15f; // 로그인 대기 상한 - 넘으면 오프라인

    [SerializeField] private SceneLoader _sceneLoader;
    [SerializeField] private RankingPanel _rankingPanel;
    [SerializeField] private MetaUpgradePanel _upgradePanel;
    [SerializeField] private SettingsPanel _settingsPanel;
    [SerializeField] private GuidePanel _guidePanel;
    [SerializeField] private CharacterSelectPanel _characterSelectPanel;
    [SerializeField] private NicknamePanel _nicknamePanel;
    [SerializeField] private TMP_Text _goldText;      // 누적 골드
    [SerializeField] private TMP_Text _bestScoreText; // 최고 점수

    private void Start()
    {
        RefreshGold();
        _bestScoreText.text = SaveData.BestScore.ToString("N0");
        StartCoroutine(OpenNicknameIfMissing());
    }

    /// <summary>로그인 완료 대기 - 닉네임 없으면 등록 창 열기, 오프라인이면 안 염</summary>
    private IEnumerator OpenNicknameIfMissing()
    {
        float giveUpTime = Time.unscaledTime + NicknameWaitSeconds;
        yield return new WaitUntil(() => IsRankingReady() || Time.unscaledTime > giveUpTime);

        if (IsRankingReady() && !RankingService.Instance.HasNickname) _nicknamePanel.Open();
    }

    private static bool IsRankingReady() => RankingService.Instance != null && RankingService.Instance.IsReady;

    /// <summary>누적 골드 표시 갱신 - 강화 구매·초기화 후</summary>
    public void RefreshGold() => _goldText.text = SaveData.MetaGold.ToString("N0");

    /// <summary>Enter/Space 입력 감지 - 닉네임·랭킹·강화·설정·가이드·캐릭터 선택 창 열려 있으면 무시, ESC = 설정·가이드·캐릭터 선택 닫기</summary>
    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (_nicknamePanel.IsOpen) return;
        if (_settingsPanel.IsOpen)
        {
            if (kb.escapeKey.wasPressedThisFrame) _settingsPanel.Close();
            return;
        }
        if (_guidePanel.IsOpen)
        {
            if (kb.escapeKey.wasPressedThisFrame) _guidePanel.Close();
            return;
        }
        if (_characterSelectPanel.IsOpen)
        {
            if (kb.escapeKey.wasPressedThisFrame) _characterSelectPanel.Close();
            return;
        }
        if (_rankingPanel.IsOpen || _upgradePanel.IsOpen) return;

        if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
        {
            StartGame();
        }
    }

    /// <summary>게임 시작 - 캐릭터 선택 창 열기</summary>
    public void StartGame() => _characterSelectPanel.Open(StartBattle);

    /// <summary>전투 시작 - 가이드 안 봤으면 가이드 먼저, 닫으면 전투</summary>
    private void StartBattle()
    {
        if (SaveData.IsGuideSeen)
        {
            _sceneLoader.LoadBattle();
            return;
        }

        _guidePanel.OpenFirstRun(() =>
        {
            SaveData.MarkGuideSeen();
            _sceneLoader.LoadBattle();
        });
    }

    /// <summary>게임 종료 - 에디터에선 Play 종료</summary>
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

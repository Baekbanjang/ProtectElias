using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Leaderboards.Models;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>랭킹 창 - 전체 랭킹(온라인 상위 10명·내 순위) / 내 기록(이 기기 1~10위) 탭</summary>
public class RankingPanel : MonoBehaviour
{
    private const string EmptyValue = "-"; // 기록 없는 행
    private const int OnlineRowCount = 10;
    private const string DateFormat = "MM-dd HH:mm";
    private const string LoadingMessage = "불러오는 중...";
    private const string FailedMessage = "랭킹을 불러오지 못했어요";
    private const string NoRecordMessage = "아직 기록이 없어요";
    private const string NeedNicknameMessage = "닉네임 등록 후 기록돼요";

    private static readonly Color MyRowColor = new Color32(255, 214, 120, 255); // 내 행 강조
    private static readonly Color ActiveTabLabelColor = new Color32(0x41, 0x29, 0x29, 255);
    private static readonly Color InactiveTabLabelColor = new Color32(0x6E, 0x5A, 0x46, 255);

    [Header("내 기록")]
    [SerializeField] private TMP_Text[] _scoreTexts; // 1~10위 점수 칸
    [SerializeField] private TMP_Text[] _waveTexts;  // 1~10위 웨이브 칸
    [SerializeField] private GameObject _emptyText; // "기록 없음"

    [Header("탭")]
    [SerializeField] private Button _onlineTabButton;
    [SerializeField] private Button _localTabButton;
    [SerializeField] private GameObject _onlineView;
    [SerializeField] private GameObject _localView;

    [Header("전체 랭킹")]
    [SerializeField] private GameObject _onlineRows; // 10행 묶음
    [SerializeField] private Image[] _onlineRowImages;
    [SerializeField] private TMP_Text[] _onlineNameTexts;
    [SerializeField] private TMP_Text[] _onlineScoreTexts;
    [SerializeField] private TMP_Text[] _onlineWaveTexts;
    [SerializeField] private TMP_Text[] _onlineDateTexts;
    [SerializeField] private TMP_Text _onlineStatusText; // 불러오는 중·실패·기록 없음
    [SerializeField] private Image _myRankRow;
    [SerializeField] private TMP_Text _myRankText;

    private TMP_Text _onlineTabLabel;
    private TMP_Text _localTabLabel;
    private Sprite _tabNormalSprite;
    private Sprite _tabActiveSprite;
    private bool _isOnlineTab;
    private int _requestId; // 늦게 온 응답 무시용

    public bool IsOpen => gameObject.activeSelf;

    /// <summary>서버 메타데이터 - SubmitScore 가 저장</summary>
    [System.Serializable]
    private class EntryMetadata { public string nickname; public int wave; public string time; } // time = 서버 기록 시각(UTC, ISO 8601)

    private void Awake()
    {
        _onlineTabLabel = _onlineTabButton.GetComponentInChildren<TMP_Text>(true);
        _localTabLabel = _localTabButton.GetComponentInChildren<TMP_Text>(true);
        _tabNormalSprite = _onlineTabButton.image.sprite;
        _tabActiveSprite = _onlineTabButton.spriteState.pressedSprite;
        _onlineTabButton.onClick.AddListener(() => SelectTab(true));
        _localTabButton.onClick.AddListener(() => SelectTab(false));
    }

    private void OnDisable() => _requestId++; // 닫히면 대기 중 응답 버림

    /// <summary>←→ 탭 전환</summary>
    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb.leftArrowKey.wasPressedThisFrame) SelectTab(true);
        else if (kb.rightArrowKey.wasPressedThisFrame) SelectTab(false);
    }

    /// <summary>내 기록 갱신 후 전체 랭킹 탭으로 열기</summary>
    public void Open()
    {
        gameObject.SetActive(true);
        Refresh();
        ShowTab(true);
    }

    public void Close() => gameObject.SetActive(false);

    private void Refresh()
    {
        List<RunRecord> records = SaveData.LoadRecords();
        for (int i = 0; i < _scoreTexts.Length; i++)
        {
            bool hasRecord = i < records.Count;
            _scoreTexts[i].text = hasRecord ? records[i].Score.ToString("N0") : EmptyValue;
            _waveTexts[i].text = hasRecord ? records[i].Wave.ToString() : EmptyValue;
        }

        _emptyText.SetActive(records.Count == 0);
    }

    /// <summary>탭 선택 - 이미 보는 탭이면 무시</summary>
    private void SelectTab(bool isOnline)
    {
        if (isOnline == _isOnlineTab) return;
        ShowTab(isOnline);
    }

    /// <summary>탭 표시 전환 - 전체 랭킹이면 매번 새로 조회</summary>
    private void ShowTab(bool isOnline)
    {
        _isOnlineTab = isOnline;
        _requestId++;
        _onlineView.SetActive(isOnline);
        _localView.SetActive(!isOnline);
        SetTabStyle(_onlineTabButton, _onlineTabLabel, isOnline);
        SetTabStyle(_localTabButton, _localTabLabel, !isOnline);

        if (isOnline) LoadOnlineScores();
    }

    private void SetTabStyle(Button tab, TMP_Text label, bool isActive)
    {
        tab.image.sprite = isActive ? _tabActiveSprite : _tabNormalSprite;
        label.color = isActive ? ActiveTabLabelColor : InactiveTabLabelColor;
    }

    /// <summary>상위 10명 + 내 순위 조회 - 창 닫힘·탭 전환 뒤 온 응답은 버림</summary>
    private async void LoadOnlineScores()
    {
        int requestId = _requestId;
        ShowOnlineMessage(LoadingMessage);

        RankingService service = RankingService.Instance;
        if (service == null || !service.IsReady)
        {
            ShowOnlineMessage(FailedMessage);
            return;
        }

        bool hasNickname = service.HasNickname;
        Task<LeaderboardScoresPage> topTask = service.GetTopScoresAsync(OnlineRowCount);
        Task<LeaderboardEntry> myTask = hasNickname ? service.GetMyScoreAsync() : Task.FromResult<LeaderboardEntry>(null);
        try
        {
            await Task.WhenAll(topTask, myTask);
        }
        catch (System.Exception e)
        {
            if (this == null || requestId != _requestId) return;
            Debug.LogWarning($"랭킹 조회 실패: {e.Message}");
            ShowOnlineMessage(FailedMessage);
            return;
        }

        if (this == null || requestId != _requestId) return; // 대기 중 씬 전환·창 닫힘·탭 전환
        ShowOnlineScores(topTask.Result.Results, myTask.Result, hasNickname);
    }

    /// <summary>행 숨기고 안내 문구만</summary>
    private void ShowOnlineMessage(string message)
    {
        _onlineRows.SetActive(false);
        _myRankRow.gameObject.SetActive(false);
        _onlineStatusText.gameObject.SetActive(true);
        _onlineStatusText.text = message;
    }

    /// <summary>상위 10행 + 내 순위 줄 채우기</summary>
    private void ShowOnlineScores(List<LeaderboardEntry> entries, LeaderboardEntry myEntry, bool hasNickname)
    {
        bool isEmpty = entries.Count == 0;
        _onlineStatusText.gameObject.SetActive(isEmpty);
        _onlineStatusText.text = NoRecordMessage;
        _onlineRows.SetActive(!isEmpty);

        string myPlayerId = AuthenticationService.Instance.PlayerId;
        for (int i = 0; i < _onlineNameTexts.Length; i++)
        {
            LeaderboardEntry entry = i < entries.Count ? entries[i] : null;
            EntryMetadata metadata = entry != null ? ParseMetadata(entry.Metadata) : null;
            _onlineNameTexts[i].text = GetNickname(metadata);
            _onlineScoreTexts[i].text = entry != null ? entry.Score.ToString("N0") : EmptyValue;
            _onlineWaveTexts[i].text = GetWave(metadata);
            _onlineDateTexts[i].text = GetDate(metadata);
            _onlineRowImages[i].color = entry != null && entry.PlayerId == myPlayerId ? MyRowColor : Color.white;
        }

        _myRankRow.gameObject.SetActive(!isEmpty); // 빈 랭킹 = 내 기록도 없음, 안내 중복 방지
        _myRankText.text = !hasNickname ? NeedNicknameMessage
            : myEntry == null ? NoRecordMessage
            : $"내 순위  {myEntry.Rank + 1}위   점수 {myEntry.Score:N0}   웨이브 {GetWave(ParseMetadata(myEntry.Metadata))}";
    }

    /// <summary>메타데이터 JSON 해석 - 없거나 깨지면 null</summary>
    private static EntryMetadata ParseMetadata(string json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        try
        {
            return JsonUtility.FromJson<EntryMetadata>(json);
        }
        catch (System.ArgumentException)
        {
            return null;
        }
    }

    private static string GetNickname(EntryMetadata metadata) =>
        metadata != null && !string.IsNullOrEmpty(metadata.nickname) ? metadata.nickname : EmptyValue;

    /// <summary>기록 시각 - 서버 UTC → PC 지역 시간, 없으면 "-"</summary>
    private static string GetDate(EntryMetadata metadata) =>
        metadata != null && System.DateTime.TryParse(metadata.time, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.RoundtripKind, out System.DateTime time)
            ? time.ToLocalTime().ToString(DateFormat)
            : EmptyValue;

    private static string GetWave(EntryMetadata metadata) =>
        metadata != null && metadata.wave > 0 ? metadata.wave.ToString() : EmptyValue;
}

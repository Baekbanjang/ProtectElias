using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;
using Unity.Services.Leaderboards.Exceptions;
using System.Collections.Generic;
using Unity.Services.CloudCode;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models.Data.Player;

/// <summary>온라인 랭킹 - UGS 로그인, 닉네임, 점수, 제출,조회</summary>
public class RankingService : MonoBehaviour
{
    public const string LeaderboardId = "global_best"; // Cloud Code SubmitScore 의 LEADERBOARD_ID 와 반드시 같게

    public static RankingService Instance { get; private set; }
    public bool IsReady { get; private set; }
    public string Nickname { get; private set; } // 없으면 null
    public bool HasNickname => !string.IsNullOrEmpty(Nickname);

   private void Awake()
   {
       if (Instance != null) { Destroy(gameObject); return; }
       Instance = this;
       DontDestroyOnLoad(gameObject);
   }
   
   private async void Start() => await SignInAsync(); 
   
   /// <summary>UGS 초기화 + 익명 로그인</summary>
   private async Task SignInAsync()
   {
       try
       {
           await UnityServices.InitializeAsync();
           if (!AuthenticationService.Instance.IsSignedIn)
               await AuthenticationService.Instance.SignInAnonymouslyAsync();

           await LoadNicknameAsync();
           IsReady = true;
           Debug.Log($"UGS 로그인 - PlayerId {AuthenticationService.Instance.PlayerId}, 닉네임 {Nickname ?? "(없음)"}");
       }
       catch (System.Exception e)
       {
           Debug.LogWarning($"UGS 로그인 실패 - 오프라인으로 진행: {e.Message}");
       }
   }

   /// <summary>내 닉네임 불러오기 - 서버만 쓰는 Protected 영역</summary>
   private async Task LoadNicknameAsync()
   {
       var data = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { "nickname" },
           new LoadOptions(new ProtectedReadAccessClassOptions()));
       Nickname = data.TryGetValue("nickname", out var item) ? item.Value.GetAs<string>() : null;
   }

   /// <summary>상위 점수 조회 - 메타데이터(닉네임·웨이브) 포함</summary>
   public async Task<LeaderboardScoresPage> GetTopScoresAsync(int limit)
   {
       return await LeaderboardsService.Instance.GetScoresAsync(LeaderboardId,
           new GetScoresOptions { Limit = limit, IncludeMetadata = true });
   }

   /// <summary>내 점수·순위 조회 - 기록 없으면 null</summary>
   public async Task<LeaderboardEntry> GetMyScoreAsync()
   {
       try
       {
           return await LeaderboardsService.Instance.GetPlayerScoreAsync(LeaderboardId,
               new GetPlayerScoreOptions { IncludeMetadata = true });
       }
       catch (LeaderboardsException e) when (e.Reason == LeaderboardsExceptionReason.EntryNotFound)
       {
           return null;
       }
   }

   [SerializeField] private int _testScore = 1000;

   [ContextMenu("테스트 - 상위 조회")]
   private async void TestGetScores()
   {
       if (!IsReady) { Debug.LogWarning("아직 로그인 전 - Play 후 'UGS 로그인' 로그를 보고 다시"); return; }
       
       try
       {
           LeaderboardScoresPage page = await LeaderboardsService.Instance.GetScoresAsync(LeaderboardId,
               new GetScoresOptions { Limit = 50, IncludeMetadata = true });
           foreach (LeaderboardEntry entry in page.Results)
               Debug.Log(
                   $"Rank {entry.Rank} | 점수 {entry.Score} | 일시 {entry.UpdatedTime.ToLocalTime()} | {entry.Metadata}");
       }
       catch (System.Exception e)
       {
           Debug.LogWarning($"조회 실패: {e.Message}");
       }
   }
   
   /// <summary>서버 응답 - 닉네임 등록 결과</summary>
   [System.Serializable]
   public class NicknameResult { public bool ok; public string reason; }

   /// <summary>닉네임 등록 - 서버에서 규칙, 중복 검사</summary>
   public async Task<NicknameResult> RegisterNicknameAsync(string nickname)
   {
       var args = new Dictionary<string, object> { { "nickname", nickname } };
       NicknameResult result = await CloudCodeService.Instance.CallEndpointAsync<NicknameResult>("RegisterNickname", args);
       if (result.ok) Nickname = nickname;
       return result;
   }
   
   [SerializeField] private string _testNickname = "비비";

   [ContextMenu("테스트 - 닉네임 등록")]
   private async void TestRegisterNickname()
   {
       if (!IsReady) { Debug.LogWarning("아직 로그인 전"); return; }
       try
       {
           NicknameResult result = await RegisterNicknameAsync(_testNickname);
           Debug.Log($"닉네임 '{_testNickname}' -> ok={result.ok}, reason={result.reason}");
       }
       catch (System.Exception e) { Debug.LogWarning($"닉네임 등록 실패: {e.Message}"); }
   }

   [ContextMenu("테스트 - 새 계정으로 다시 로그인")]
   private async void TestNewAccount()
   {
       AuthenticationService.Instance.SignOut();
       AuthenticationService.Instance.ClearSessionToken(); // 저장된 로그인 정보 삭제 -> 다음 로그인은 새 계정
       await AuthenticationService.Instance.SignInAnonymouslyAsync();
       await LoadNicknameAsync(); // 새 계정 = 닉네임 없음
       Debug.Log($"새 계정 - PlayerId {AuthenticationService.Instance.PlayerId}, 닉네임 {Nickname ?? "(없음)"}");
   }
   
   /// <summary>서버 응답 - 점수 제출 결과</summary>
   [System.Serializable]
   public class SubmitResult { public bool ok; public string reason; }

   /// <summary>점수 제출 - 서버에서 범위,닉네임 검사 후 기록</summary>
   public async Task<SubmitResult> SubmitScoreAsync(int score, int wave)
   {
       var args = new Dictionary<string, object> { { "score", score }, { "wave", wave } };
       return await CloudCodeService.Instance.CallEndpointAsync<SubmitResult>("SubmitScore", args);
   }

   // ---- 6단계 연습용 ----
   [SerializeField] private int _testWave = 5;

   [ContextMenu("테스트 - 서버로 점수 제출")]
   private async void TestSubmitScore()
   {
       if (!IsReady) { Debug.LogWarning("아직 로그인 전"); return; }
       try
       {
           SubmitResult result = await SubmitScoreAsync(_testScore, _testWave);
           Debug.Log($"점수 {_testScore} / 웨이브 {_testWave} -> ok={result.ok}, reason={result.reason}");
       }
       catch (System.Exception e) { Debug.LogWarning($"점수 제출 실패: {e.Message}"); }
   }
}

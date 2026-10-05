using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>로컬 저장 - 누적 골드, 최고 점수, 기록 10개, 메타 강화 레벨, 가이드 확인, 선택 캐릭터 (PlayerPrefs)</summary>
public static class SaveData
{
    public const int MaxRecords = 10;

    private const string MetaGoldKey = "DUT_MetaGold";
    private const string BestScoreKey = "DUT_BestScore";
    private const string RecordsKey = "DUT_Records"; // JSON
    private const string MetaLevelKeyPrefix = "DUT_Meta_"; // + 강화 항목 id
    private const string GuideSeenKey = "DUT_GuideSeen"; // 0·1
    private const string SelectedCharacterKey = "DUT_SelectedCharacter"; // 캐릭터 id

    /// <summary>JsonUtility 용 목록 포장</summary>
    [Serializable]
    private class RecordList
    {
        public List<RunRecord> Records = new List<RunRecord>();
    }

    public static int MetaGold => PlayerPrefs.GetInt(MetaGoldKey, 0);
    public static int BestScore => PlayerPrefs.GetInt(BestScoreKey, 0);
    public static bool IsGuideSeen => PlayerPrefs.GetInt(GuideSeenKey, 0) == 1;
    public static string SelectedCharacter => PlayerPrefs.GetString(SelectedCharacterKey, "");

    /// <summary>가이드 확인 기록 - 첫 시작 가이드 닫을 때</summary>
    public static void MarkGuideSeen()
    {
        PlayerPrefs.SetInt(GuideSeenKey, 1);
        PlayerPrefs.Save();
    }

    /// <summary>선택 캐릭터 저장 - 캐릭터 선택 창 START</summary>
    public static void SetSelectedCharacter(string id)
    {
        PlayerPrefs.SetString(SelectedCharacterKey, id);
        PlayerPrefs.Save();
    }

    /// <summary>메타 강화 레벨</summary>
    public static int GetMetaLevel(string id) => PlayerPrefs.GetInt(MetaLevelKeyPrefix + id, 0);

    /// <summary>메타 강화 레벨 저장</summary>
    public static void SetMetaLevel(string id, int level)
    {
        PlayerPrefs.SetInt(MetaLevelKeyPrefix + id, level);
        PlayerPrefs.Save();
    }

    /// <summary>누적 골드 증감 - 강화 구매(-)·초기화 환불(+)</summary>
    public static void AddMetaGold(int amount)
    {
        PlayerPrefs.SetInt(MetaGoldKey, MetaGold + amount);
        PlayerPrefs.Save();
    }

    /// <summary>기록 목록 - 점수 높은 순</summary>
    public static List<RunRecord> LoadRecords()
    {
        string json = PlayerPrefs.GetString(RecordsKey, "");
        if (string.IsNullOrEmpty(json)) return new List<RunRecord>();

        RecordList list = JsonUtility.FromJson<RecordList>(json);
        return list?.Records ?? new List<RunRecord>();
    }

    /// <summary>판 결과 저장 - 골드 누적, 최고 점수, 기록 추가</summary>
    public static void SaveRun(int score, int wave, int remainingGold) // remainingGold = 판 종료 시 남은 골드
    {
        PlayerPrefs.SetInt(MetaGoldKey, MetaGold + remainingGold);
        if (score > BestScore) PlayerPrefs.SetInt(BestScoreKey, score);

        List<RunRecord> records = LoadRecords();
        var record = new RunRecord { Score = score, Wave = wave, Date = DateTime.Now.ToString("yyyy-MM-dd") };

        // 같은 점수면 먼저 세운 기록이 위
        int index = records.FindIndex(r => r.Score < score);
        if (index < 0) index = records.Count;
        records.Insert(index, record);
        if (records.Count > MaxRecords) records.RemoveRange(MaxRecords, records.Count - MaxRecords);

        PlayerPrefs.SetString(RecordsKey, JsonUtility.ToJson(new RecordList { Records = records }));
        PlayerPrefs.Save();
    }
}

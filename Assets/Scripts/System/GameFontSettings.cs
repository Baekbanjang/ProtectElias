using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>글꼴 설정 - 헌글 ↔ 읽기 쉬운 글꼴(갈무리) 전환 (PlayerPrefs). 모든 TMP 글자에 적용</summary>
public static class GameFontSettings
{
    private const string ReadableFontKey = "DUT_ReadableFont"; // 0 = 헌글 · 1 = 갈무리
    private const string ReadableFontPath = "Fonts/Galmuri11 SDF"; // Resources 경로

    /// <summary>전환 알림 - 버튼 라벨 갱신용</summary>
    public static event Action Changed;

    public static bool IsReadableFont => PlayerPrefs.GetInt(ReadableFontKey, 0) == 1;

    private static TMP_FontAsset s_readableFont;
    private static readonly HashSet<TMP_Text> s_pendingTexts = new HashSet<TMP_Text>();
    private static readonly List<TMP_Text> s_processingTexts = new List<TMP_Text>();

    /// <summary>플레이 시작 - 글꼴 로드, 글자 갱신 이벤트·씬 로드 구독</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        s_pendingTexts.Clear();
        s_processingTexts.Clear();
        s_readableFont = Resources.Load<TMP_FontAsset>(ReadableFontPath);
        if (s_readableFont == null) Debug.LogError($"GameFontSettings: Resources/{ReadableFontPath} 없음");

        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged); // 도메인 리로드 꺼짐 대비 중복 구독 방지
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
        _ = CanvasUpdateRegistry.instance;
        Canvas.willRenderCanvases -= ApplyPendingTexts;
        Canvas.willRenderCanvases += ApplyPendingTexts;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    /// <summary>헌글 ↔ 갈무리 전환 후 저장, 화면의 모든 글자 즉시 반영</summary>
    public static void Toggle()
    {
        PlayerPrefs.SetInt(ReadableFontKey, IsReadableFont ? 0 : 1);
        PlayerPrefs.Save();
        ApplyAllTexts();
        Changed?.Invoke();
    }

    /// <summary>글자 생성·갱신마다 - 새로 뜬 팝업 글자도 여기서 적용</summary>
    private static void OnTextChanged(Object obj)
    {
        if (!Application.isPlaying) return; // 편집 모드 씬 변경 방지
        if (obj is not TMP_Text text || text == null) return;
        if (IsReadableFont ? text.font != s_readableFont && IsKoreanText(text) : text.font == s_readableFont)
            s_pendingTexts.Add(text);
    }

    private static void ApplyAllTexts()
    {
        foreach (TMP_Text text in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Apply(text);
    }

    private static void ApplyPendingTexts()
    {
        if (!Application.isPlaying || s_pendingTexts.Count == 0) return;
        if (CanvasUpdateRegistry.IsRebuildingGraphics() || CanvasUpdateRegistry.IsRebuildingLayout()) return;

        s_processingTexts.AddRange(s_pendingTexts);
        s_pendingTexts.Clear();
        foreach (TMP_Text text in s_processingTexts) Apply(text);
        s_processingTexts.Clear();
    }

    /// <summary>현재 설정 글꼴로 교체 - 원래 글꼴은 기억했다가 되돌림</summary>
    private static void Apply(TMP_Text text)
    {
        if (text == null || s_readableFont == null) return;
        if (CanvasUpdateRegistry.IsRebuildingGraphics() || CanvasUpdateRegistry.IsRebuildingLayout())
        {
            s_pendingTexts.Add(text);
            return;
        }

        if (IsReadableFont)
        {
            if (text.font == s_readableFont || !IsKoreanText(text)) return;
            GameFontOriginal original = text.GetComponent<GameFontOriginal>();
            if (original == null) original = text.gameObject.AddComponent<GameFontOriginal>();
            original.Capture(text);
            text.font = s_readableFont;
        }
        else
        {
            if (text.font != s_readableFont) return;
            GameFontOriginal original = text.GetComponent<GameFontOriginal>();
            if (original == null || original.Font == null) return;
            text.font = original.Font;
            text.fontSharedMaterial = original.Material;
        }

        if (text.isActiveAndEnabled) text.ForceMeshUpdate();
    }

    /// <summary>바꿀 대상 - 헌글 글자 전부 + 영문 글꼴 중 한글이 섞인 글자 (로고 등 영문 전용은 유지)</summary>
    private static bool IsKoreanText(TMP_Text text)
    {
        if (text.font == null) return false;
        if (text.font.name.StartsWith("Hungeul")) return true;

        string content = text.text;
        if (string.IsNullOrEmpty(content)) return false;
        foreach (char c in content)
        {
            if (c >= '가' && c <= '힣') return true; // 한글 음절
        }
        return false;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => ApplyAllTexts();

    /// <summary>씬 종료 - 파괴된 글자 기록 정리</summary>
    private static void OnSceneUnloaded(Scene scene)
    {
        s_pendingTexts.RemoveWhere(text => text == null);
    }
}

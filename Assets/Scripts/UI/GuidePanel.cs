using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>게임 가이드 창 - 페이지 넘김(버튼·←→), 페이지 점, 마지막 페이지 닫기·시작</summary>
public class GuidePanel : MonoBehaviour
{
    private const string CloseLabel = "닫기";
    private const string StartLabel = "시작"; // 첫 실행

    [SerializeField] private GuidePageData[] _pages;

    [Header("내용")]
    [SerializeField] private Image _image;
    [SerializeField] private GameObject _imagePlaceholder; // 이미지 없을 때
    [SerializeField] private TMP_Text _pageTitleText;
    [SerializeField] private TMP_Text _bodyText;
    [SerializeField] private TMP_Text _pageNumberText; // n/8

    [Header("페이지 점")]
    [Tooltip("점 원본 - 페이지 수만큼 복제")]
    [SerializeField] private Image _dotTemplate;
    [SerializeField] private Color _dotOnColor = new Color(0.255f, 0.161f, 0.161f);
    [SerializeField] private Color _dotOffColor = new Color(0.255f, 0.161f, 0.161f, 0.3f);

    [Header("버튼")]
    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _prevButton;
    [SerializeField] private Button _nextButton;
    [SerializeField] private GameObject _nextArrow;   // ▶
    [SerializeField] private TMP_Text _nextLabelText; // 마지막 페이지 닫기·시작
    [SerializeField] private Button _skipButton;      // 첫 실행만

    private readonly List<Image> _dots = new List<Image>();
    private int _pageIndex;
    private Action _onClosed; // 첫 실행 - 닫은 뒤 전투 시작

    public bool IsOpen => gameObject.activeSelf;

    /// <summary>페이지 점 생성, 버튼 연결</summary>
    private void Awake()
    {
        for (int i = 0; i < _pages.Length; i++)
        {
            Image dot = Instantiate(_dotTemplate, _dotTemplate.transform.parent);
            dot.gameObject.SetActive(true);
            _dots.Add(dot);
        }
        _dotTemplate.gameObject.SetActive(false);

        _closeButton.onClick.AddListener(Close);
        _skipButton.onClick.AddListener(Close);
        _prevButton.onClick.AddListener(() => ShowPage(_pageIndex - 1));
        _nextButton.onClick.AddListener(OnNextClicked);
    }

    /// <summary>←→ 페이지 넘김</summary>
    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb.leftArrowKey.wasPressedThisFrame) ShowPage(_pageIndex - 1);
        else if (kb.rightArrowKey.wasPressedThisFrame) ShowPage(_pageIndex + 1);
    }

    /// <summary>1페이지부터 열기</summary>
    public void Open()
    {
        _onClosed = null;
        OpenFirstPage();
    }

    /// <summary>첫 실행 - 마지막 버튼 = 시작, 닫으면 onClosed 실행</summary>
    public void OpenFirstRun(Action onClosed)
    {
        _onClosed = onClosed;
        OpenFirstPage();
    }

    /// <summary>닫기 - 첫 실행이면 onClosed 실행</summary>
    public void Close()
    {
        gameObject.SetActive(false);
        Action onClosed = _onClosed;
        _onClosed = null;
        onClosed?.Invoke();
    }

    private void OpenFirstPage()
    {
        gameObject.SetActive(true);
        _skipButton.gameObject.SetActive(_onClosed != null);
        ShowPage(0);
    }

    private void OnNextClicked()
    {
        if (_pageIndex >= _pages.Length - 1) Close();
        else ShowPage(_pageIndex + 1);
    }

    /// <summary>페이지 내용·점·버튼 갱신</summary>
    private void ShowPage(int index)
    {
        _pageIndex = Mathf.Clamp(index, 0, _pages.Length - 1);
        GuidePageData page = _pages[_pageIndex];

        _image.sprite = page.Image;
        _image.enabled = page.Image != null;
        _imagePlaceholder.SetActive(page.Image == null);
        _pageTitleText.text = page.Title;
        _bodyText.text = page.Body;
        _pageNumberText.text = $"{_pageIndex + 1}/{_pages.Length}";

        for (int i = 0; i < _dots.Count; i++) _dots[i].color = i == _pageIndex ? _dotOnColor : _dotOffColor;

        bool isLastPage = _pageIndex == _pages.Length - 1;
        _prevButton.interactable = _pageIndex > 0;
        _nextArrow.SetActive(!isLastPage);
        _nextLabelText.gameObject.SetActive(isLastPage);
        _nextLabelText.text = _onClosed != null ? StartLabel : CloseLabel;
    }
}

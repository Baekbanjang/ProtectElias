using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>캐릭터 선택 창 - 캐릭터 넘김(버튼·←→), 무기·스킬 정보, START = 선택 저장 후 시작</summary>
public class CharacterSelectPanel : MonoBehaviour
{
    [SerializeField] private CharacterData[] _characters;

    [Header("캐릭터")]
    [SerializeField] private Image _portraitImage;
    [Tooltip("전신 이미지 배율 - 정수")]
    [SerializeField] private int _portraitScale = 2;
    [SerializeField] private TMP_Text _nameText; // [ 이름 ]

    [Header("무기 정보")]
    [SerializeField] private Image _weaponIcon;
    [SerializeField] private TMP_Text _weaponNameText;
    [SerializeField] private TMP_Text _weaponDescriptionText;

    [Header("스킬 정보")]
    [SerializeField] private Image _skillIcon;
    [SerializeField] private GameObject _skillIconPlaceholder; // 아이콘 없을 때
    [SerializeField] private TMP_Text _skillNameText;
    [SerializeField] private TMP_Text _skillDescriptionText;

    [Header("페이지 점")]
    [Tooltip("점 원본 - 캐릭터 수만큼 복제")]
    [SerializeField] private Image _dotTemplate;
    [SerializeField] private Sprite _dotOnSprite;
    [SerializeField] private Sprite _dotOffSprite;

    [Header("버튼")]
    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _prevButton;
    [SerializeField] private Button _nextButton;
    [SerializeField] private Button _startButton;

    private readonly List<Image> _dots = new List<Image>();
    private int _index;
    private Action _onStart; // START - 전투 진입

    public bool IsOpen => gameObject.activeSelf;

    /// <summary>페이지 점 생성, 버튼 연결</summary>
    private void Awake()
    {
        for (int i = 0; i < _characters.Length; i++)
        {
            Image dot = Instantiate(_dotTemplate, _dotTemplate.transform.parent);
            dot.gameObject.SetActive(true);
            _dots.Add(dot);
        }
        _dotTemplate.gameObject.SetActive(false);

        _closeButton.onClick.AddListener(Close);
        _prevButton.onClick.AddListener(() => ShowCharacter(_index - 1));
        _nextButton.onClick.AddListener(() => ShowCharacter(_index + 1));
        _startButton.onClick.AddListener(OnStartClicked);
    }

    /// <summary>←→ 캐릭터 넘김</summary>
    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb.leftArrowKey.wasPressedThisFrame) ShowCharacter(_index - 1);
        else if (kb.rightArrowKey.wasPressedThisFrame) ShowCharacter(_index + 1);
    }

    /// <summary>저장된 캐릭터로 열기 - START 시 onStart 실행</summary>
    public void Open(Action onStart)
    {
        _onStart = onStart;
        gameObject.SetActive(true);

        int saved = Array.FindIndex(_characters, c => c.Id == SaveData.SelectedCharacter);
        ShowCharacter(Mathf.Max(saved, 0));
    }

    /// <summary>닫기 - 시작 안 함</summary>
    public void Close()
    {
        _onStart = null;
        gameObject.SetActive(false);
    }

    private void OnStartClicked()
    {
        SaveData.SetSelectedCharacter(_characters[_index].Id);

        Action onStart = _onStart;
        Close();
        onStart?.Invoke();
    }

    /// <summary>캐릭터·무기·스킬·점·화살표 갱신</summary>
    private void ShowCharacter(int index)
    {
        _index = Mathf.Clamp(index, 0, _characters.Length - 1);
        CharacterData character = _characters[_index];

        _portraitImage.sprite = character.Portrait;
        _portraitImage.rectTransform.sizeDelta = character.Portrait.rect.size * _portraitScale;
        _nameText.text = $"[ {character.DisplayName} ]";

        WeaponData weapon = character.StartingWeapon;
        _weaponIcon.sprite = character.SelectWeaponIcon != null ? character.SelectWeaponIcon : weapon.Icon; // 칸 크기 고정 + Preserve Aspect
        _weaponNameText.text = weapon.DisplayName;
        _weaponDescriptionText.text = weapon.Description;

        bool hasSkillIcon = character.SkillIcon != null;
        _skillIcon.sprite = character.SkillIcon;
        _skillIcon.enabled = hasSkillIcon;
        _skillIconPlaceholder.SetActive(!hasSkillIcon);
        _skillNameText.text = character.SkillName;
        _skillDescriptionText.text = character.SkillDescription;

        for (int i = 0; i < _dots.Count; i++) _dots[i].sprite = i == _index ? _dotOnSprite : _dotOffSprite;

        _prevButton.interactable = _index > 0;
        _nextButton.interactable = _index < _characters.Length - 1;
    }
}

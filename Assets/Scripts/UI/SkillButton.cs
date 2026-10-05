using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>SPC 버튼 - 클릭 발동, 재사용 대기 덮개·남은 초, 준비 테두리·반짝임, 사용 불가 표시</summary>
public class SkillButton : MonoBehaviour
{
    [SerializeField] private SkillController _skill;
    [SerializeField] private Button _button; // Sprite Swap - 눌림·사용 불가 그림
    [SerializeField] private Image _icon;
    [SerializeField] private Image _cooldownMask; // Filled Radial 360
    [SerializeField] private TMP_Text _cooldownText;
    [SerializeField] private Image _readyRim;

    [Tooltip("사용 불가 시 아이콘 색")]
    [SerializeField] private Color _disabledIconColor = new Color(0.55f, 0.55f, 0.55f, 1f);

    [Tooltip("준비 반짝임 시간(초) - 실제 시간")]
    [SerializeField] private float _readyFlashDuration = 0.6f;

    [Tooltip("준비 반짝임 깜박임 간격(초)")]
    [SerializeField] private float _readyFlashInterval = 0.1f;

    private bool _wasReady = true;
    private float _flashRemaining;

    private void Awake()
    {
        _button.onClick.AddListener(_skill.TryActivate);
    }

    /// <summary>캐릭터 스킬 아이콘</summary>
    private void Start()
    {
        _icon.sprite = _skill.Character.SkillIcon;
    }

    /// <summary>상태 반영 - 사용 불가·대기·준비</summary>
    private void Update()
    {
        bool isBlocked = _skill.IsBlocked;
        bool isCooling = _skill.IsCasting || _skill.CooldownRemaining > 0f;

        _button.interactable = !isBlocked;
        _icon.color = isBlocked ? _disabledIconColor : Color.white;

        _cooldownMask.enabled = isCooling;
        _cooldownMask.fillAmount = _skill.IsCasting ? 1f : _skill.CooldownRemaining / _skill.Cooldown;
        _cooldownText.enabled = isCooling;
        _cooldownText.text = Mathf.CeilToInt(_skill.IsCasting ? _skill.Cooldown : _skill.CooldownRemaining).ToString();

        // 대기 끝난 순간 반짝임
        bool isReady = !isCooling;
        if (isReady && !_wasReady) _flashRemaining = _readyFlashDuration;
        _wasReady = isReady;
        _flashRemaining -= Time.unscaledDeltaTime;

        bool isFlashOff = _flashRemaining > 0f && (int)(_flashRemaining / _readyFlashInterval) % 2 == 1;
        _readyRim.enabled = isReady && !isBlocked && !isFlashOff;
    }
}

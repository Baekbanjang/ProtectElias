using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>닉네임 등록 창 - 입력 규칙 검사, 서버 등록(중복 검사), 성공 시 닫기</summary>
public class NicknamePanel : MonoBehaviour
{
    private const string RuleMessage = "2~8자, 한글/영문/숫자만 쓸 수 있어요";
    private const string CheckingMessage = "확인 중...";
    private const string TakenMessage = "이미 사용 중인 닉네임이에요";
    private const string AlreadyRegisteredMessage = "이미 닉네임이 등록된 계정이에요";
    private const string FailedMessage = "연결에 실패했어요. 다시 시도해 주세요";

    private static readonly Regex NicknameRule = new Regex("^[가-힣a-zA-Z0-9]{2,8}$"); // 서버 규칙과 동일

    [SerializeField] private TMP_InputField _nameInput;
    [SerializeField] private TMP_Text _messageText; // 오류·진행 안내
    [SerializeField] private Button _confirmButton;

    private bool _isSubmitting;

    public bool IsOpen => gameObject.activeSelf;

    private void Awake()
    {
        _confirmButton.onClick.AddListener(Confirm);
        _nameInput.onSubmit.AddListener(_ => Confirm());
    }

    /// <summary>입력 비우고 열기</summary>
    public void Open()
    {
        gameObject.SetActive(true);
        _nameInput.text = string.Empty;
        _messageText.text = string.Empty;
        _confirmButton.interactable = true;
        _nameInput.ActivateInputField();
    }

    public void Close() => gameObject.SetActive(false);

    /// <summary>규칙 검사 후 서버 등록 - 성공 시 닫기, 실패 시 안내</summary>
    private async void Confirm()
    {
        if (_isSubmitting) return;

        string nickname = _nameInput.text.Trim();
        if (!NicknameRule.IsMatch(nickname))
        {
            _messageText.text = RuleMessage;
            return;
        }

        _isSubmitting = true;
        _confirmButton.interactable = false;
        _messageText.text = CheckingMessage;

        string message;
        try
        {
            RankingService.NicknameResult result = await RankingService.Instance.RegisterNicknameAsync(nickname);
            message = result.ok ? null
                : result.reason == "taken" ? TakenMessage
                : result.reason == "already_registered" ? AlreadyRegisteredMessage
                : RuleMessage;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"닉네임 등록 실패: {e.Message}");
            message = FailedMessage;
        }

        if (this == null) return; // 대기 중 씬 전환
        _isSubmitting = false;
        _confirmButton.interactable = true;

        if (message == null) Close();
        else _messageText.text = message;
    }
}

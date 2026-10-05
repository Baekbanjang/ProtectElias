using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>배속 - TAB·버튼으로 배율 순환, 정지 해제 시 현재 배율로 복귀</summary>
public class GameSpeed : MonoBehaviour
{
    [Tooltip("실제 속도 목록 - 순서대로 순환. 표시는 1x·2x·3x (단계 번호)")]
    [SerializeField] private float[] _scales = { 1f, 1.5f, 2f };

    [SerializeField] private Button _button;
    [SerializeField] private TMP_Text _label;

    private int _index;

    /// <summary>실제 Time.timeScale 값</summary>
    public float CurrentScale => _scales[_index];

    private void Awake()
    {
        _button.onClick.AddListener(Toggle);
        UpdateLabel();
    }

    /// <summary>TAB 입력 감지</summary>
    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame) Toggle();
    }

    /// <summary>다음 배율 - 정지 중이면 배율만 바꾸고 정지 유지</summary>
    public void Toggle()
    {
        _index = (_index + 1) % _scales.Length;
        if (!Mathf.Approximately(Time.timeScale, 0f)) Time.timeScale = CurrentScale;
        UpdateLabel();
    }

    /// <summary>정지 해제 - 현재 배율 적용</summary>
    public void ResumeTime() => Time.timeScale = CurrentScale;

    private void UpdateLabel() => _label.text = $"{_index + 1}x"; // 표시 = 단계 번호 (실제 속도와 다름)
}

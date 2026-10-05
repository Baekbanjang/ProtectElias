using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>설정 창 - 배경음·효과음 볼륨, 음소거. 닫을 때 저장</summary>
public class SettingsPanel : MonoBehaviour
{
    [SerializeField] private Slider _bgmSlider;
    [SerializeField] private TMP_Text _bgmValueText; // %
    [SerializeField] private Slider _sfxSlider;
    [SerializeField] private TMP_Text _sfxValueText; // %
    [SerializeField] private Toggle _muteToggle;
    [SerializeField] private TMP_Text _muteValueText; // ON·OFF
    [SerializeField] private Button _closeButton;

    public bool IsOpen => gameObject.activeSelf;

    private void Awake()
    {
        _bgmSlider.onValueChanged.AddListener(OnBgmChanged);
        _sfxSlider.onValueChanged.AddListener(OnSfxChanged);
        _muteToggle.onValueChanged.AddListener(OnMuteChanged);
        _closeButton.onClick.AddListener(Close);
    }

    /// <summary>저장값 반영 후 열기</summary>
    public void Open()
    {
        gameObject.SetActive(true);
        Refresh();
    }

    /// <summary>저장 후 닫기</summary>
    public void Close()
    {
        GameAudioSettings.Save();
        gameObject.SetActive(false);
    }

    private void Refresh()
    {
        _bgmSlider.SetValueWithoutNotify(GameAudioSettings.BgmVolume);
        _sfxSlider.SetValueWithoutNotify(GameAudioSettings.SfxVolume);
        _muteToggle.SetIsOnWithoutNotify(GameAudioSettings.IsMuted);
        UpdateLabels();
    }

    private void OnBgmChanged(float value)
    {
        GameAudioSettings.SetBgmVolume(value);
        UpdateLabels();
    }

    private void OnSfxChanged(float value)
    {
        GameAudioSettings.SetSfxVolume(value);
        UpdateLabels();
    }

    private void OnMuteChanged(bool isOn)
    {
        GameAudioSettings.SetMuted(isOn);
        UpdateLabels();
    }

    private void UpdateLabels()
    {
        _bgmValueText.text = $"{Mathf.RoundToInt(_bgmSlider.value * 100f)}%";
        _sfxValueText.text = $"{Mathf.RoundToInt(_sfxSlider.value * 100f)}%";
        _muteValueText.text = _muteToggle.isOn ? "ON" : "OFF";
    }
}

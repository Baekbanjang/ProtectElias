using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>글꼴 전환 버튼 - 누를 때마다 헌글 ↔ 한글(갈무리), 라벨에 현재 글꼴 표시</summary>
[RequireComponent(typeof(Button))]
public class FontToggleButton : MonoBehaviour
{
    [SerializeField] private TMP_Text _label;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(GameFontSettings.Toggle);
    }

    private void OnEnable()
    {
        GameFontSettings.Changed += UpdateLabel;
        UpdateLabel();
    }

    private void OnDisable() => GameFontSettings.Changed -= UpdateLabel;

    /// <summary>라벨 = 현재 글꼴 (헌글 / 한글)</summary>
    private void UpdateLabel()
    {
        if (_label != null) _label.text = GameFontSettings.IsReadableFont ? "한글" : "헌글";
    }
}

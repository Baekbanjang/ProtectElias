using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HP바 - Health 의 현재 비율을 채움 이미지·퍼센트 글자에 반영
/// </summary>
public class HealthBar : MonoBehaviour
{
    [SerializeField] private Health _target;
    [SerializeField] private Image _fill;
    [SerializeField] private TMP_Text _percentText;

    /// <summary>매 프레임 갱신</summary>
    private void Update()
    {
        if (_target == null) return;
        
        _fill.fillAmount = (float)_target.CurrentHp/ _target.MaxHp;
        _percentText.text = $"{Mathf.RoundToInt(_fill.fillAmount * 100f)}%";
    }
}

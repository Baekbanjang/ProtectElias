using TMPro;
using UnityEngine;

/// <summary>글꼴 복원 정보 보관 — 템플릿 복제 시 함께 복사</summary>
[DisallowMultipleComponent]
public class GameFontOriginal : MonoBehaviour
{
    [SerializeField] private TMP_FontAsset _font;
    [SerializeField] private Material _material;

    public TMP_FontAsset Font => _font;
    public Material Material => _material;

    /// <summary>전환 전 글꼴과 머티리얼 기록</summary>
    public void Capture(TMP_Text text)
    {
        _font = text.font;
        _material = text.fontSharedMaterial;
    }
}

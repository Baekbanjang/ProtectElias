using System.Collections;
using UnityEngine;

/// <summary>피격 흰색 플래시 - 잠깐 플래시 머티리얼로 교체 후 복귀</summary>
public class SpriteFlash : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _renderer;
    [SerializeField] private Material _flashMaterial;

    [Tooltip("플래시 유지 시간(초)")]
    [SerializeField] private float _duration = 0.06f;

    private Material _originalMaterial;
    private Coroutine _flashRoutine;

    /// <summary>원래 머티리얼 캐싱</summary>
    private void Awake()
    {
        if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();
        _originalMaterial = _renderer.sharedMaterial;
    }

    /// <summary>플래시 1회 - 연속 피격 시 시간 갱신</summary>
    public void Flash()
    {
        if (_flashRoutine != null) StopCoroutine(_flashRoutine);
        _flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        _renderer.sharedMaterial = _flashMaterial;
        yield return new WaitForSeconds(_duration);
        _renderer.sharedMaterial = _originalMaterial;
        _flashRoutine = null;
    }
}

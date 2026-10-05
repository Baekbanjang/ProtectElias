using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>버튼 누르는 동안 글자를 아래로 이동</summary>
[RequireComponent(typeof(Button))]
public class PressedTextOffset : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private RectTransform _text;
    [SerializeField] private float _offset = 2f; // 화면 px

    private Button _button;
    private Vector2 _basePosition;
    private bool _isPressed;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _basePosition = _text.anchoredPosition;
    }

    private void OnDisable() => SetPressed(false);

    public void OnPointerDown(PointerEventData eventData) => SetPressed(_button.interactable);
    public void OnPointerUp(PointerEventData eventData) => SetPressed(false);
    public void OnPointerExit(PointerEventData eventData) => SetPressed(false);

    private void SetPressed(bool isPressed)
    {
        if (_isPressed == isPressed) return;
        _isPressed = isPressed;
        _text.anchoredPosition = _basePosition + (isPressed ? Vector2.down * _offset : Vector2.zero);
    }
}

using UnityEngine;

/// <summary>게임 가이드 페이지 - 제목, 본문, 이미지</summary>
[CreateAssetMenu(fileName = "GuidePageData", menuName = "Scriptable Objects/GuidePageData")]
public class GuidePageData : ScriptableObject
{
    [SerializeField] private string _title;

    [TextArea(4, 10)]
    [SerializeField] private string _body;

    [Tooltip("비어 있으면 '이미지 준비 중' 표시")]
    [SerializeField] private Sprite _image;

    public string Title => _title;
    public string Body => _body;
    public Sprite Image => _image;
}

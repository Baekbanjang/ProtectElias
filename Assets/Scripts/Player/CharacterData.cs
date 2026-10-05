using UnityEngine;

/// <summary>캐릭터 데이터 - 선택 창 표시, 시작 무기, 스킬</summary>
[CreateAssetMenu(fileName = "CharacterData", menuName = "Scriptable Objects/CharacterData")]
public class CharacterData : ScriptableObject
{
    [Tooltip("저장용 id - DUT_SelectedCharacter")]
    [SerializeField] private string _id;

    [SerializeField] private string _displayName;

    [Tooltip("선택 창 전신 이미지")]
    [SerializeField] private Sprite _portrait;

    [SerializeField] private WeaponData _startingWeapon;

    [Tooltip("선택 창 무기 아이콘 - 비어 있으면 무기 아이콘 사용")]
    [SerializeField] private Sprite _selectWeaponIcon;

    [Header("스킬")]
    [SerializeField] private string _skillName;

    [TextArea(2, 4)]
    [SerializeField] private string _skillDescription;

    [Tooltip("비어 있으면 빈 칸 표시")]
    [SerializeField] private Sprite _skillIcon;

    [Tooltip("재사용 대기(초) - 게임 시간")]
    [SerializeField] private float _skillCooldown = 20f;

    [Tooltip("투사체 1개 피해 - 공격력 특성 적용 전")]
    [SerializeField] private int _skillDamage = 30;

    [Tooltip("투척 줄 수 - 전투 영역 균등 분할")]
    [SerializeField] private int _skillLaneCount = 4;

    [Tooltip("스킬 투사체 프리팹")]
    [SerializeField] private SkillSpear _skillSpearPrefab;

    [Tooltip("컷인 초상 프레임")]
    [SerializeField] private Sprite[] _skillCutinFrames;

    public string Id => _id;
    public string DisplayName => _displayName;
    public Sprite Portrait => _portrait;
    public WeaponData StartingWeapon => _startingWeapon;
    public Sprite SelectWeaponIcon => _selectWeaponIcon;
    public string SkillName => _skillName;
    public string SkillDescription => _skillDescription;
    public Sprite SkillIcon => _skillIcon;
    public float SkillCooldown => _skillCooldown;
    public int SkillDamage => _skillDamage;
    public int SkillLaneCount => _skillLaneCount;
    public SkillSpear SkillSpearPrefab => _skillSpearPrefab;
    public Sprite[] SkillCutinFrames => _skillCutinFrames;

    /// <summary>저장된 선택 캐릭터 - 없으면 첫 번째</summary>
    public static CharacterData FindSelected(CharacterData[] characters)
    {
        if (characters == null || characters.Length == 0) return null;

        CharacterData selected = System.Array.Find(characters, c => c.Id == SaveData.SelectedCharacter);
        return selected != null ? selected : characters[0];
    }
}

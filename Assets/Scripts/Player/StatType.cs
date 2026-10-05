/// <summary>특성·아티팩트가 올리는 스탯 종류</summary>
public enum StatType
{
    Attack,           // 공격력 %
    AttackSpeed,      // 발사 간격 감소 %
    ProjectileSpeed,  // 투사체 속도 %
    MaxHp,            // 최대 HP (정수)
    CritChance,       // 치명타 확률 %p
    CritDamage,       // 치명타 피해 %p
    Exp,              // 경험치 획득 %
    TypeDamage,       // 태그 대미지 증가 % - RequiredAttackType 과 조합
    TypeReload,       // 태그 재장전 시간 감소 % - RequiredAttackType 과 조합
    Multishot,        // 고유 - 전체 무기 발사 수 +1
    LifeSteal,        // 처치 시 회복 확률 %p
    LastStand,        // 고유 - HP 30% 이하 공격력 +40%
    TypeRadius,       // 태그 폭발 반경 증가 % - RequiredAttackType 과 조합
    Gold,             // 골드 획득 %
    SkillCooldownOnHit,     // 아티팩트 - 무기 명중마다 스킬 재사용 대기 감소(초)
    DefenseDamageReduction, // 아티팩트 - 방어선 받는 피해 감소 %
    Bounty,                 // 고유 - 보스 처치 골드 2배
}

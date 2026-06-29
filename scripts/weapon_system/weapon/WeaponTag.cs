public enum WeaponTag
{
    // 공격 형태
    Melee,          // 근접
    Ranged,         // 원거리
    Area,           // 범위/장판
    Magic,          // 마법
    Physical,       // 물리

    // 무기 종류
    Sword,          // 검
    Blunt,          // 둔기
    Bow,            // 활
    Staff,          // 지팡이
    Gun,            // 총기류

    // 속성
    Fire,          // 화염
    Ice,           // 얼음
    Lightning,     // 번개
    Poison,        // 독 
    Holy,          // 세인트
    Dark,          // 어둠
    Blood,         // 피
    Rock,          // 바위
    Stun,          // 기절

    // 장르
    Acient,    // 고대
    Medieval,  // 중세
    Modern,    // 현대
    Martial,   // 무협
    Cyberpunk, // 사이버펑크
    School,    // 학교물
    Army,      // 군용

    // 전용
    Exclusive,  // 전용 무기

    // 없음
    None = 0,
}

[System.Serializable]
public class TagModifier
{
    public WeaponTag targetTag;   // 어떤 태그에 보너스를 줄 것인가?
    public StatType targetStat;   // 어떤 스탯을 강화할 것인가?
    public float amount;          // 얼마나 강화할 것인가? (0.2 = +20%)
}
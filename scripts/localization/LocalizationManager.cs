using System.Collections.Generic;
using System.Runtime.InteropServices;
//using Unity.Android.Gradle;
using Unity.VisualScripting;
using UnityEditor.PackageManager;
using UnityEngine;

public static class LocalizationManager
{
    private static string m_CurrentLanguage = "ko";
    // --- [한국어 사전] ---
    private static Dictionary<string, string> m_KoreanDict = new Dictionary<string, string>()
    {
        // 캐릭터 이름
        {"Warmachine_Chloe", "전투 병기 클로에" },
        {"Black_Knight", "검은 기사" },
        {"Blue_Flame_Magician", "마법 학교 우등생 아리아" },
        // 1. Survival
        { "maxHealth", "최대 체력" }, 
        { "hpRegen", "체력 회복량" }, 
        { "armor", "방어력" },
        { "evasion", "회피율" }, 
        { "lifesteal", "흡혈율" }, 
        { "thorns", "반사 데미지" },

        // 2. Offense
        { "damageMult", "공격력 배율" }, 
        { "attackSpeed", "공격 속도" }, 
        { "critChance", "치명타 확률" },
        { "critDamage", "치명타 피해" }, 
        { "eliteDamage", "엘리트 추가 피해" }, 
        { "knockback", "넉백 파워" },

        // 3. Projectile
        { "projectileCount", "투사체 개수" }, 
        { "projectilePierce", "관통 횟수" }, 
        { "projectileBounces", "도탄 횟수" },
        { "projectileChain", "전이 횟수" }, 
        { "projectileSpeed", "투사체 속도" }, 
        { "areaSize", "범위 크기" }, 
        { "duration", "지속 시간" },

        // 4. Mobility
        { "moveSpeed", "이동 속도" },
        { "jumpCount", "점프 횟수" },
        { "jumpHeight", "점프력" },

        // 5. Utility & Economy
        { "luck", "행운" },
        { "pickupRange", "획득 범위" },
        { "xpGain", "경험치 추가 획득" },
        { "goldGain", "골드 추가 획득" },

        // 6. Elemental Damage
        { "burnDamageMult", "화염 피해 배율" },
        { "poisonDamageMult", "중독 피해 배율" },
        { "electricDamageMult", "번개 피해 배율" },
        { "freezeDamageMult", "얼음 피해 배율" },
        { "impactDamageMult", "충격 피해 배율" },

        // 7. Status Effectiveness
        { "burnEfficiency", "화상 효율" },
        { "poisonEfficiency", "중독 효율" },
        { "electricSynergy", "감전 효율" },
        { "stunEfficiency", "기절 효율" },
        { "freezeEfficiency", "빙결 효율" },

        // 8. Status Accumulation
        { "burnAccumulation", "화상 축적률" },
        { "poisonAccumulation", "중독 축적률" },
        { "electricAccumulation", "감전 축적률" },
        { "freezeAccumulation", "빙결 축적률" },
        { "impactAccumulation", "충격 축적률" },
        
        // Tier & UI
        { "Tier_I", "I 티어" }, 
        { "Tier_II", "II 티어" }, 
        { "Tier_III", "III 티어" }, 
        { "Tier_IV", "IV 티어" }, 
        { "Tier_V", "V 티어" },
        { "stat_upgrade_format", "{0} {1}{2}" },

        { "ui_next_level", "다음 레벨 : {0}" },

        // 무기 업그레이드
        // 기본 전투
        { "Damage", "공격력" },
        { "DamageMult", "공격력 배율" },
        { "ProjectileSpeed", "투사체 속도" },
        { "ProjectileCount", "투사체 수" },
        { "AreaSize", "범위 크기" },
        { "AttackSpeed", "공격 속도" }, 
        { "CritChance", "치명타 확률" }, 
        { "CritMultiplier", "치명타 피해" },
        {"EliteDamageMult", "보스 피해" },
        { "ExplosionRadius", "폭발 반경" },
        { "MaxDistance", "사거리" },
        {"PierceCount", "관통 횟수" },
        {"BurstCount", "연사 횟수" },
        {"ChainCount", "전이 횟수" },
        {"ChainRange", "전이 범위" },

        {"Armor", "방어도" },
        { "Shield", "보호막" },
        { "MeleeRange", "근접 공격 사거리" },
        { "MeleeAngle", "근접 공격 각도" },
        { "LaserWidth", "레이저 두께" },
        { "LaserTickInterval", "레이저 피해 간격" },
        { "FieldTickInterval", "장판 피해 간격" },

        // 원소 대미지
        { "BurnDamageMult", "화염 피해 배율" }, 
        { "PoisonDamageMult", "중독 피해 배율" },
        { "ElectricDamageMult", "전기 피해 배율" }, 
        { "FreezeDamageMult", "얼음 피해 배율" },
        { "ImpactDamageMult", "충격 피해 배율" },

        // 원소 효율
        { "BurnEfficiency", "화상 효율" }, 
        { "PoisonEfficiency", "중독 효율" },
        { "ElectricSynergy", "감전 효율" }, 
        { "StunEfficiency", "기절 효율" },
        { "FreezeEfficiency", "빙결 효율" },

        // 원소 축적
        { "BurnAccumulation", "화상 축적률" }, 
        { "PoisonAccumulation", "중독 축적률" },
        { "ElectricAccumulation", "감전 축적률" }, 
        { "FreezeAccumulation", "빙결 축적률" },
        { "ImpactAccumulation", "충격 축적률" },

        // 무기 태그
        {"Melee", "근접"},
        {"Ranged", "원거리" },
        {"Area", "범위" },
        {"Magic", "마법" },
        {"Physical", "물리" },

        {"Sword", "검" },
        {"Blunt", "둔기" },
        {"Bow", "활" },
        {"Staff", "지팡이" },
        {"Gun", "총기" },

        {"Fire", "화염" },
        {"Ice", "얼음" },
        {"Lightning", "번개" },
        {"Poison", "독" },
        {"Holy", "신성" },
        {"Dark", "어둠"},
        {"Blood", "피" },
        {"Rock", "바위" },
        {"Stun", "기절" },

        {"Acient", "고대" },
        {"Medieval", "중세" },
        {"Modern", "현대" },   
        {"Martial", "무협" },   
        {"Cyberpunk", "사이버펑크" }, 
        {"School", "학교" },
        {"Army", "군용" },
        {"Exclusive", "전용 무기" },  

        // 무기 이름
        // --- Weapons ---
        { "weapon_pistol", "권총" },
        { "weapon_rifle", "돌격 소총" },
        { "weapon_shotgun", "산탄총" },
        { "weapon_grenade", "수류탄" },
        { "weapon_sniper", "저격총" },
        { "weapon_shuriken", "중독 표창" },
        { "weapon_kunai", "뇌전 수리검" },
        { "weapon_sword", "검" },
        { "weapon_fireball_new", "화염구" },
        { "weapon_iceball", "얼음 구체" },
        { "weapon_lightningball", "전격 구체" },
        { "weapon_fireball_ultimate", "무 영창 화염구" },
        { "weapon_waterball", "물방울" },
        { "weapon_lightning", "낙뢰" },
        { "weapon_meteor_staff", "유성 지팡이" },

        // UI
        { "ui_inspector_default", "" },

        // 태그 효과
        { "Tag_Army_Tier2_Desc", " + 군용 무기 피해 20%" },
        { "Tag_Army_Tier3_Desc", " + 군용 무기 피해 30%" },
        { "Tag_Army_Tier4_Desc", " + 군용 무기 피해 50%\n (60초마다 전투 순양함 발동)" },

        // 어빌리티 노드
        { "ui_ability_current", "현재 효과: <color=#FFD700>+{0}</color>" },
        { "ui_ability_next", "다음 레벨: <color=#00FF00>+{0}</color>" },
        { "ui_ability_max", "<color=#FF4500>최대 레벨 달성</color>" },

        { "c_tier1_dm_name", "공격력 강화 I" },
        { "c_tier1_dm_desc", "모든 무기의 피해를 증폭시킵니다." },

        { "c_tier1_as_name", "가속 I" },
        { "c_tier1_as_desc", "반응 속도를 높여 공격 주기를 단축합니다." },

        { "c_tier1_pr_name", "인지 범위 확대" },
        { "c_tier1_pr_desc", "오브젝트 획득 반경이 늘어납니다." },

        { "c_tier2_hp_name", "체력 강화 I" },
        { "c_tier2_hp_desc", "기본 체력이 증가합니다." },

        { "c_tier2_sh_name", "전투 슈트 강화 II" },
        { "c_tier2_sh_desc", "최대 쉴드량이 증가합니다." },

        { "c_tier2_ws_name", "추가 무기 슬롯 I" },
        { "c_tier2_ws_desc", "전투 중 휴대할 수 있는 무기 슬롯이 영구적으로 증가합니다." },

        { "c_tier3_cc_name", "약점 포착" },
        { "c_tier3_cc_desc", "적의 약점을 포착하여 치명타 확률이 증가합니다." },

        { "c_tier3_cd_name", "정밀 타격" },
        { "c_tier3_cd_desc", "치명타 피해량이 증가합니다." },

        { "c_tier3_ws_name", "추가 무기 슬롯 II" },
        { "c_tier3_ws_desc", "전투 중 휴대할 수 있는 무기 슬롯이 영구적으로 증가합니다." },

        { "c_tier4_wd_name", "공격력 강화 II" },
        { "c_tier4_wd_desc", "[군용] 태그 무기의 피해가 증가합니다." },

        { "c_tier4_ws_name", "가속 II" },
        { "c_tier4_ws_desc", "[군용] 태그 무기의 공격 주기가 감소합니다." },

        { "c_tier4_ed_name", "보스 킬러" },
        { "c_tier4_ed_desc", "엘리트 몬스터 피해량이 증가합니다." },

        { "c_tier5_hp_name", "체력 강화 II" },
        { "c_tier5_hp_desc", "기본 체력이 대폭 증가합니다." },

        { "c_tier5_sh_name", "전투 슈트 강화 II" },
        { "c_tier5_sh_desc", "최대 쉴드량이 대폭 증가합니다." },

        { "c_tier5_bc_name", "전투 머신 각성" },
        { "c_tier5_bc_desc", "[군용] 태그 무기의 공격 횟수가 증가합니다." },
        
        // --- Black Knight Ability Nodes ---
        { "bk_tier1_regen_name", "체력 회복량 강화 I" },
        { "bk_tier1_regen_desc", "전투 중 체력 회복량이 증가합니다." },
        { "bk_tier1_dm_name", "공격력 배율 강화 I" },
        { "bk_tier1_dm_desc", "모든 무기의 공격력 배율이 증가합니다." },
        { "bk_tier1_angle_name", "공격 각도 강화 I" },
        { "bk_tier1_angle_desc", "근접 무기의 공격 범위 각도가 증가합니다." },
        
        { "bk_tier2_armor_name", "방어력 강화 I" },
        { "bk_tier2_armor_desc", "방어력이 증가하여 받는 피해가 감소합니다." },
        { "bk_tier2_range_name", "공격 범위 강화 I" },
        { "bk_tier2_range_desc", "근접 무기의 공격 범위가 증가합니다." },
        { "bk_tier2_kb_name", "넉백 파워 강화 I" },
        { "bk_tier2_kb_desc", "적을 밀쳐내는 넉백 파워가 증가합니다." },
        
        { "bk_tier3_hp_name", "최대 체력 강화 I" },
        { "bk_tier3_hp_desc", "캐릭터의 최대 체력이 증가합니다." },
        { "bk_tier3_ms_name", "이동 속도 강화 I" },
        { "bk_tier3_ms_desc", "캐릭터의 이동 속도가 증가합니다." },
        { "bk_tier3_as_name", "공격 속도 강화 I" },
        { "bk_tier3_as_desc", "모든 무기의 공격 속도가 증가합니다." },
        
        { "bk_tier4_cc_name", "치명타 확률 강화 II" },
        { "bk_tier4_cc_desc", "[중세] 태그 무기의 치명타 확률이 증가합니다." },
        { "bk_tier4_ed_name", "엘리트 추가 피해 II" },
        { "bk_tier4_ed_desc", "[중세] 태그 무기의 엘리트 몬스터 추가 피해가 증가합니다." },
        { "bk_tier4_dm_name", "공격력 배율 강화 II" },
        { "bk_tier4_dm_desc", "[중세] 태그 무기의 공격력 배율이 증가합니다." },
        
        { "bk_tier5_cm_name", "치명타 피해 강화 II" },
        { "bk_tier5_cm_desc", "모든 무기의 치명타 피해량이 증가합니다." },
        { "bk_tier5_th_name", "반사 데미지 강화 II" },
        { "bk_tier5_th_desc", "적에게 피격 시 반사 데미지를 줍니다." },
        { "bk_tier5_exc_name", "전용 보호막 각성" },
        { "bk_tier5_exc_desc", "[전용 무기] 장착 시 특수한 쉴드(Shield) 효과를 획득합니다." },
        
        // --- Blue Flame Magician Ability Nodes ---
        { "bm_tier1_dm_name", "공격력 배율 강화 I" },
        { "bm_tier1_dm_desc", "모든 무기의 공격력 배율이 증가합니다." },
        { "bm_tier1_ps_name", "영창 : 증폭" },
        { "bm_tier1_ps_desc", "마력을 증폭시켜 모든 무기 피해를 증가시킵니다." },
        { "bm_tier1_asize_name", "투사체 강화 I" },
        { "bm_tier1_asize_desc", "투사체를 강화해 투사체의 속도를 증가시킵니다." },
        { "bm_tier2_size_name", "투사체 강화 II" },
        { "bm_tier2_size_desc", "투사체를 더욱 강화하여 크기를 증가시킵니다" },

        { "bm_tier2_cc_name", "지혜의 눈 I" },
        { "bm_tier2_cc_desc", "지혜의 눈으로 상대의 약점을 꿰뚫어 크리티컬 확률이 증가합니다." },
        { "bm_tier3_cc_name", "지혜의 눈 II" },
        { "bm_tier3_cc_desc", "지혜의 눈으로 상대의 약점을 꿰뚫어 크리티컬 피해가 증가합니다." },

        { "bm_tier2_burn_dm_name", "화염학 수련 I" },
        { "bm_tier2_burn_dm_desc", "화염 피해량이 증가합니다." },
        { "bm_tier3_burn_eff_name", "화상 효율 강화 I" },
        { "bm_tier3_burn_eff_desc", "화상 상태이상으로 주는 지속 피해 효율이 증가합니다." },

        { "bm_tier3_bc_name", "마력 회로 확장" },
        { "bm_tier3_bc_desc", "모든 [마법] 무기의 연사 횟수가 증가합니다." },
        
        { "bm_N12_MS_name", "영창 숙련" },
        { "bm_N12_MS_desc", "영창 속도를 수련하여 [마법] 무기의 공격 속도가 빨라집니다." },
        { "bm_N12_M3_name", "대마도사 : 화염" },
        { "bm_N12_M3_desc", "화염 마법의 대가가 되어 무영창 화염구를 기본 무기로 사용합니다." },
        
        { "bm_tier4_md_name", "영창 : 보호" },
        { "bm_tier4_md_desc", "체내의 마나를 둘러 보호막을 형성합니다." },
        { "bm_tier4_ms_name", "부유 I" },
        { "bm_tier4_ms_desc", "일정 확률로 공중에 떠 적의 공격을 회피합니다." },
        { "bm_tier5_ed_name", "부유 II" },
        { "bm_tier5_ed_desc", "적의 공격을 회피할 확률이 추가로 증가합니다." },

        { "bm_tier4_cm_name", "보호 마법 강화 I" },
        { "bm_tier4_cm_desc", "보호막의 재사용 대기 시간이 감소합니다." },
        { "bm_tier5_exc_name", "보호 마법 강화 II" },
        { "bm_tier5_exc_desc", "보호막의 충전량이 증가합니다." },

        { "bm_tier5_pc_name", "지구력 강화 I" },
        { "bm_tier5_pc_desc", "체력을 단련하여 최대 체력이 증가합니다." },
        { "bm_N8_SS3_name", "지구력 강화 II" },
        { "bm_N8_SS3_desc", "체력을 추가로 단련하여, 체력 재생이 증가합니다." },
        { "bm_N8_M2_name", "보호 마법 강화" },
        { "bm_N8_M2_desc", "보호막의 총량이 대폭 증가합니다." },

        { "bm_N2_1_name", "마력 숙련도 I" },
        { "bm_N2_1_desc", "[마법] 무기 피해가 증가합니다." },
        { "bm_N2_2_name", "마력 숙련도 II" },
        { "bm_N2_2_desc", "마법을 다루는 것에 익숙해져 [마법] 무기 피해가 더욱 증가합니다." },
        { "bm_N2_3_name", "마력 숙련도 III" },
        { "bm_N2_3_desc", "마법에 통달하여 [마법] 무기 피해가 대폭 증가합니다." },
        { "bm_N2_M_T_name", "특화 : 얼음" },
        { "bm_N2_M_T_desc", "기본 무기가 [얼음]으로 변경됩니다. 이 노드는 '대마도사 : 화염'과 함께 선택할 수 없습니다." },
        { "bm_N2_M_B_name", "특화 : 번개" },
        { "bm_N2_M_B_desc", "기본 무기가 [번개]로 변경됩니다. 이 노드는 '대마도사 : 화염'과 함께 선택할 수 없습니다." },

        { "bm_N6_1_name", "방어도 강화 I" },
        { "bm_N6_1_desc", "마력으로 신체를 보호하여 방어도가 증가합니다." },
        { "bm_N6_2_name", "방어도 강화 II" },
        { "bm_N6_2_desc", "강한 마력으로 신체를 보호하여 방어도가 더욱 증가합니다." },
        { "bm_N6_3_name", "방어도 강화 III" },
        { "bm_N6_3_desc", "마력을 극한으로 끌어올려 방어도가 대폭 증가합니다." },
        { "bm_N6_M_L_name", "영창 : 전환" },
        { "bm_N6_M_L_desc", "마력을 완전히 발휘하는 대신 생명력을 소모합니다. 체력이 1이 되는 대신 보호막이 3배가 됩니다." },
        { "bm_N6_M_R_name", "영창 : 마법 가시" },
        { "bm_N6_M_R_desc", "자신이 피격 시, 피해를 주는 가시를 발사하여 공격합니다." },

        { "bm_N10_1_name", "화염 주입 I" },
        { "bm_N10_1_desc", "적에게 적용하는 화염 축적률이 증가합니다." },
        { "bm_N10_2_name", "화염 주입 II" },
        { "bm_N10_2_desc", "마법에 화염 속성을 담아 적에게 적용하는 화염 축적률이 더욱 증가합니다." },
        { "bm_N10_3_name", "화염 주입 II" },
        { "bm_N10_3_desc", "적을 불태울 정도의 불꽃을 생성하여, 적에게 적용하는 화염 축적률이 대폭 증가합니다." },
        { "bm_N10_M_B_name", "내재된 화염" },
        { "bm_N10_M_B_desc", "[화염] 태그를 하나 보유한 채로 게임을 시작합니다." },
        { "bm_N10_M_T_name", "화염 통달" },
        { "bm_N10_M_T_desc", "화염 속성을 통달하여, 적에게 화상으로 주는 지속 피해의 효율이 극한으로 증가합니다."},

        { "bm_N4_M1_name", "영창 : 신속" },
        { "bm_N4_M1_desc", "마법의 힘으로 이동 속도를 상승시킵니다." },
        { "bm_N4_S1_name", "무기 슬롯 I" },
        { "bm_N4_S1_desc", "사용 가능한 무기 슬롯이 하나 늘어납니다." },
        { "bm_N4_SS1_name", "무기 슬롯 II" },
        { "bm_N4_SS1_desc", "사용 가능한 무기 슬롯이 하나 늘어납니다." },
        { "bm_N4_S2_name", "픽업 범위 증가" },
        { "bm_N4_S2_desc", "재화 습득 범위가 증가합니다." },
        { "bm_N4_SS2_name", "행운" },
        { "bm_N4_SS2_desc", "캐릭터의 행운 스탯이 증가합니다. 더 높은 등급의 옵션이 등장할 확률이 증가합니다." },
        { "bm_N4_S3_name", "경험치 획득 증가" },
        { "bm_N4_S3_desc", "캐릭터의 경험치 획득량이 증가합니다." },
        { "bm_N4_SS3_name", "골드 획득량 증가" },
        { "bm_N4_SS3_desc", "캐릭터의 골드 획득량이 증가합니다." }

    };

    // --- [영어 사전] ---
    private static Dictionary<string, string> m_EnglishDict = new Dictionary<string, string>()
    {
        { "maxHealth", "Max HP" }, 
        { "hpRegen", "HP Regen" }, 
        { "armor", "Armor" },
        { "evasion", "Evasion" }, 
        { "lifesteal", "Lifesteal" }, 
        { "thorns", "Thorns" },

        { "damageMult", "Damage Mult" }, 
        { "attackSpeed", "Attack Speed" }, 
        { "critChance", "Crit Chance" },
        { "critDamage", "Crit Damage" }, 
        { "eliteDamage", "Elite Damage" }, 
        { "knockback", "Knockback" },

        { "projectileCount", "Projectile Count" }, 
        { "projectilePierce", "Pierce" }, 
        { "projectileBounces", "Bounce" },
        { "projectileChain", "Chain" }, 
        { "projectileSpeed", "Proj Speed" }, 
        { "areaSize", "Area Size" }, 
        { "duration", "Duration" },
        { "moveSpeed", "Move Speed" }, 
        { "luck", "Luck" }, 
        { "pickupRange", "Pickup Range" },
        { "Tier_I", "Tier I" },
        { "Tier_II", "Tier II" }, 
        { "Tier_III", "Tier III" }, 
        { "Tier_IV", "Tier IV" }, 
        { "Tier_V", "Tier V" },
        { "stat_upgrade_format", "{0} {1}{2}" }
    };

    // --- [중국어/일본어 사전 (Placeholders)] ---
    private static Dictionary<string, string> m_ChineseDict = new Dictionary<string, string>() 
    { 
        { "maxHealth", "最大生命" } 
        // ...
    };
    
    private static Dictionary<string, string> m_JapaneseDict = new Dictionary<string, string>() 
    { 
        { "maxHealth", "最大体力" } 
        // ...
    };

    // 언어 코드별 사전을 관리하는 마스터 사전
    private static Dictionary<string, Dictionary<string, string>> m_MasterDict = new Dictionary<string, Dictionary<string, string>>()
    {
        { "ko", m_KoreanDict },
        { "en", m_EnglishDict },
        { "zh", m_ChineseDict },
        { "jp", m_JapaneseDict }
    };

    static LocalizationManager()
    {
        AddUITitleKeys(m_KoreanDict, true);
        AddUITitleKeys(m_EnglishDict, false);
        // zh, jp 나중에 추가
    }

    // --- [핵심 함수] ---
    public static string GetText(string key)
    {
        if (string.IsNullOrEmpty(key)) return "";

        if (!m_MasterDict.TryGetValue(m_CurrentLanguage, out var targetDict))
            targetDict = m_KoreanDict;

        if (targetDict.TryGetValue(key, out string value)) return value;

        if (m_KoreanDict.TryGetValue(key, out string fallbackValue)) return fallbackValue;

        return key;
    }

    public static string GetText(string key, params object[] args)
    {
        string baseText = GetText(key);
        try { return string.Format(baseText, args); }
        catch { return baseText; }
    }
    public static string GetUnit(string key)
    {
        if (key == "ProjectileSpeed" || key == "moveSpeed")
            return "m/s";

        if (key.Contains("Size") || key.Contains("Distance") || key.Contains("Radius") || key.Contains("Range") || key.Contains("Width"))
            return "m";

        if (key.Contains("Duration") || key.Contains("Interval"))
            return "s";

        if (key.Contains("Angle"))
            return "°";

        // %가 붙어야 하는 스탯들의 키워드를 모아둡니다.
        if (
            key.EndsWith("Mult") ||
            key.EndsWith("Multiplier") ||
            key.Contains("Chance") ||
            key.Contains("Gain") ||
            key.Contains("evasion") ||
            key.Contains("lifesteal") ||
            key.Contains("Efficiency") ||
            key.Contains("Synergy") || 
            key.Contains("Accumulation") ||
            key == "AttackSpeed")
        {
            return "%";
        }
        return "";
    }

    public static void SetLanguage(string langCode)
    {
        if (m_MasterDict.ContainsKey(langCode))
        {
            m_CurrentLanguage = langCode;
            Debug.Log($"[Localization] Language changed to: {langCode}");
        }
    }

    private static void AddUITitleKeys(Dictionary<string, string> dict, bool isKorean)
    {
        if (isKorean)
        {
            dict.Add("ui_title_character", "캐릭터 정보");
            dict.Add("ui_title_equipment", "장착 장비");
            dict.Add("ui_subtitle_weapons", "무기");
            dict.Add("ui_subtitle_cores", "코어");
            dict.Add("ui_subtitle_items", "보유 아이템");
            dict.Add("ui_title_stats", "상세 스탯");
            dict.Add("ui_title_upgrade", "업그레이드 선택");
        }
        else
        {
            dict.Add("ui_title_character", "CHARACTER");
            dict.Add("ui_title_equipment", "EQUIPMENT");
            dict.Add("ui_subtitle_weapons", "WEAPONS");
            dict.Add("ui_subtitle_cores", "CORES");
            dict.Add("ui_subtitle_items", "ITEMS");
            dict.Add("ui_title_stats", "DETAILED STATS");
            dict.Add("ui_title_upgrade", "SELECT UPGRADE");
        }
    }

    public static string FormatStatValue(string key, float value)
    {
        string unit = GetUnit(key);
        string lowerKey = key.ToLower();

        bool multiplyBy100 = (lowerKey.EndsWith("mult") || lowerKey.EndsWith("multiplier") || lowerKey.Contains("evasion") || lowerKey.Contains("lifesteal") || lowerKey.Contains("accumulation"))
                             || lowerKey.EndsWith("efficiency") || lowerKey.EndsWith("synergy")  && !lowerKey.Contains("crit") && !lowerKey.Contains("chance");

        if (multiplyBy100)
        {
            float percentValue = value * 100f;
            return (percentValue % 1 == 0) ? $"{percentValue:F0}{unit}" : $"{percentValue:F1}{unit}";
        }
        else
        {
            // 2. 이미 정수 기준인 크리티컬 수치(6 -> 6%)나 일반 수치(2.5 -> 2.5m/s)는 그대로 출력!
            return (value % 1 == 0) ? $"{value:F0}{unit}" : $"{value:F1}{unit}";
        }
    }
}
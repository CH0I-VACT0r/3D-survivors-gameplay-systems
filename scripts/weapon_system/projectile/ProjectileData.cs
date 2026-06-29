using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct StatUpgrade
{
    public StatType type;
    public float value;
}

[System.Serializable]
public struct LevelPerk
{
    public string description; // 에디터에서 보기 편하게 적는 설명
    public List<StatUpgrade> upgrades; // 한 레벨업에 여러 스탯 변경 가능
}

[CreateAssetMenu(fileName = "NewProjectileData", menuName = "ScriptableObjects/ProjectileData")]
public class ProjectileData : ScriptableObject
{
    [Header("UI Settings")]
    public string weaponName;
    public Sprite icon;
    public GameObject weaponPrefab;

    [Header("Weapon Tags")]
    public List<WeaponTag> weaponTags = new List<WeaponTag>();

    [Header("Base Stats")]
    public float baseDamage = 10f;          // Base Damage
    public float speed = 10f;               // Base Speed
    public float size = 1f;                 // Base Size
    public float maxDistance = 5f;          // Disctance
    public float duration = 5f;             // Duration
    public float baseFireRate = 1f;         // 기본 공격 속도
    public float baseCritChance = 5f;       // 기본 크리티컬 확률 (%)
    public float baseCritMultiplier = 1.5f; // 기본 크리티컬 대미지 배율
    public int baseProjectileCount = 1;     // 기본 발사 탄환 수
    public int baseBurstCount = 1;          // 기본 발사 반복 횟수

    [Header("Status Effect Settings")]
    public StatusType statusType = StatusType.None;  // 상태 이상 종류
    public float statusDamage = 0f;                  // 상태 이상 피해
    public float statusGaugeValue = 1f;              // 상태 이상 게이지 수치

    [Header("Movement Settings")]
    [Tooltip("유도 기능")]
    public bool isHoming = false;           // Homing
    public float homingDelay = 0.3f;        // Homing Delay
    public float turnSpeed = 5f;            // Turn Speed when it is homing
    public float homingGiveUpThreshold = 0.2f;
    [Tooltip("중력 기능")]
    public bool useGravity = false;         // Gravity Apply
    public float launchForce = 5f;          // Upper Force
    [Tooltip("회전 기능")]
    public bool isSpinning = false;         // Spinning Visual
    public float rotationSpeed = 360f;      // Rotation Speed

    [Header("Collision Settings")]
    public int pierceCount = 0;             // Possible Pierce Count
    public int bounceCount = 0;             // Possible Bounce Count
    public float hitCooldown = 0.5f;        // Hit Cooldown (Prevent damage per frame)

    [Header("Explosion & Knockback")]
    public bool isExplosive = false;        // Explosive
    public float explosionRadius = 3f;      // Explosion Radius
    public float knockbackForce = 10f;      // KnockBack Force

    [Header("Field Settings")]
    public bool isField;
    public float baseFieldDuration = 3.0f;
    public float fieldTickInterval = 0.5f;    // 대미지 주기
    public float fieldDamageRatio = 0.2f;     // 대미지 배율 (0.2 = 20%)
    public GameObject fieldEffectPrefab;      // 장판 VFX 프리팹
    public GameObject warningIndicatorPrefab; // 범위 표시 프리팹

    [Header("Melee Settings")]
    public bool isMelee = false;
    public float meleeRange = 3f;           // 부채꼴 반지름 (사거리)
    public float meleeAngle = 60f;          // 부채꼴 각도
    public float meleeHeight = 2f;          // 공격 높이

    [Header("Orbit Settings")]
    public bool isOrbit = false;            // 공전 여부 
    public float orbitRadius = 3f;          // 플레이어와의 거리
    public float orbitYOffset = 1.2f;       // 공전 높이

    [Header("Boomerang Settings")]
    public bool isBoomerang = false;
    public float returnThreshold = 0.5f;    // 전체 지속 시간 중 어느 시점에 돌아올지

    [Header("Laser Settings")]
    public bool isLaser = false;
    public float laserWidth = 1.0f;         // 직육면체 가로
    public float laserHeight = 2.0f;        // 직육면체 세로 (높이)
    public float laserTickInterval = 0.2f;  // 틱 뎀 주기

    [Header("Meteor Settings")]
    public bool isMeteor = false;
    public float meteorRadius = 3.0f;       // 떨어지는 지점 반경
    public float meteorSpawnHeight = 15.0f; // 생성 높이
    public bool isMeteorTargeted = false;

    [Header("Chain Settings")]
    public bool isChain = false;
    public int baseChainCount = 0;   // 기본 몇 번 튕길 것인가
    public float chainRange = 5f;    // 다음 타겟을 찾는 반경
    public GameObject chainEffectPrefab;

    [Header("Visuals")] 
    public GameObject prefab;               // Projectile object
    public GameObject muzzleFlashPrefab;    // 발사 지점 이펙트
    public GameObject impactEffectPrefab;   // 히트 이펙트
    public GameObject despawnEffectPrefab;
    public Material trailMaterial;          // Trail

    [Header("Audio Settings")]
    public AudioClip fireSound;                          // 발사할 때 재생할 소리
    [Range(0f, 1f)] public float fireSoundVolume = 0.5f; // 발사음 볼륨
    public GameObject soundPlayerPrefab;                 // 사운드 플레이어 프리팹
    public float fireSoundDuration = 0f;

    public AudioClip impactSound;                        // 피격음
    [Range(0f, 1f)] public float impactSoundVolume = 0.4f;

    [Header("Level Up Table")]
    public List<UpgradeOption> upgradePool = new List<UpgradeOption>();
}
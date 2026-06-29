[System.Serializable]
public enum StatType
{
    // --- [기존: 공용 전투 스탯] ---
    Damage,             // (DamageMult 역할)
    ProjectileSpeed,
    AreaSize,
    MaxDistance,        // (Duration 역할로 쓸 수도 있음)
    CritChance,
    CritMultiplier,     // (CritDamage 역할)
    AttackSpeed,
    ProjectileCount,
    BurstCount,
    PierceCount,        // (ProjectilePierce 역할)
    BounceCount,        // (ProjectileBounces 역할)
    ExplosionRadius,
    KnockbackForce,     // (Knockback 역할)
    MeleeRange,
    MeleeAngle,
    LaserWidth,
    LaserTickInterval,
    FieldDuration,      // (Duration 역할)
    FieldTickInterval,
    ChainCount,         // (ProjectileChain 역할)
    ChainRange,
    EliteDamageMult,    // (EliteDamage 역할)

    // --- [기존: 상태 이상 대미지] ---
    BurnDamageMult, PoisonDamageMult, ElectricDamageMult, FreezeDamageMult, ImpactDamageMult,

    // --- [기존: 상태 이상 효율] ---
    BurnEfficiency, PoisonEfficiency, ElectricSynergy, StunEfficiency, FreezeEfficiency,

    // --- [기존: 상태 이상 축적] ---
    BurnAccumulation, PoisonAccumulation, ElectricAccumulation, FreezeAccumulation, ImpactAccumulation,

    // --- [기존: 플레이어 유틸리티] ---
    MoveSpeed, Luck, PickupRange, XpGain, GoldGain,


    // ==================================================================================
    // ▼▼▼ [새로 추가된 스탯들] (기존에 없던 생존, 기동성 관련 스탯) ▼▼▼
    // ==================================================================================

    // 1. Survival (생존)
    MaxHealth,
    HpRegen,
    Armor,
    Shield,
    Evasion,
    Lifesteal,
    Thorns,

    // 2. Mobility (기동성)
    JumpCount,
    JumpHeight,


    //3. MaxWeapon Slot
    MaxWeaponSlots,

    None,
   
    DamageMult,
    ShieldRegenDelay,
    ShieldRegenRate,
    // (CritDamage, ProjectilePierce 등은 위쪽 기존 이름과 역할이 겹치므로 추가하지 않음)
    // CritMultiplier -> CritDamage
    // PierceCount -> ProjectilePierce
    // BounceCount -> ProjectileBounces
    // KnockbackForce -> Knockback
    // ChainCount -> ProjectileChain
    // FieldDuration -> Duration
    // EliteDamageMult -> EliteDamage
}
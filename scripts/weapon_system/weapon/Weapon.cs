using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class Weapon : MonoBehaviour
{
    [Header("Owner Settings")]
    [Tooltip("True : 플레이어 장착 무기, False : 적 장착 무기")]
    public bool isPlayerOwned = true; 
    public LayerMask targetLayer;
    public Collider[] GetDetectionBuffer() => _detectionBuffer;

    public enum FireMode { Scatter, Sequential, Swarm }

    [Header("Weapon Data")]
    public ProjectileData weaponData;
    public int currentLevel = 1;

    [Header("Attack Settings")]
    public FireMode fireMode = FireMode.Scatter;
    public float detectRange = 10f;

    [Header("Burst Settings")]
    public float spreadAngle = 15f;    // 분사형 각도 (Scatter용)
    public float burstInterval = 0.1f; // 연사형 간격 (Sequential용)

    [Header("Spawn Settings")]
    public Vector3 spawnOffset = new Vector3(0, 1.2f, 0.5f); // 발사 위치 오프셋
    public float targetHeightOffset = 1.0f;

    [Header("Player Stats")]
    public float damageMultiplier = 1f;

    // --- 레벨업에 의해 계산될 최종 스탯들 ---
    private float finalDamage;
    private float finalSpeed;
    private float finalSize;
    private float finalMaxDistance;
    private float finalCritChance;
    private float finalCritMultiplier;
    private float finalFireRate;
    private int finalProjectileCount;
    private int finalBurstCount;
    private int finalPierceCount;
    private int finalBounceCount;
    private float finalExplosionRadius;
    private float finalKnockbackForce;
    private float finalMeleeRange;
    private float finalMeleeAngle;
    private float finalLaserWidth;
    private float finalLaserTickInterval;
    private float finalFieldDuration;
    private float finalFieldTickInterval;
    private int finalChainCount;
    private float finalChainRange;
    private float finalStatusDmgMult;
    private float finalStatusAccumMult;
    private float currentSynergy;
    private float finalEliteDamageMult;
    protected float timer;
    public bool isFiring = false;
    public bool isAttackCommanded = false;
    private List<Projectile> spawnedProjectiles = new List<Projectile>();

    // 툴팁 정보용 스탯
    // Weapon.cs 내부
    public float GetFinalDamage() => finalDamage;
    public float GetFinalFireRate() => finalFireRate;
    public float GetFinalMaxDistance() => finalMaxDistance;
    public float GetFinalCritChance() => finalCritChance;
    public float GetFinalCritMultiplier() => finalCritMultiplier;
    public float GetFinalEliteDamage() => PlayerStatusManager.Instance.eliteDamage.Value + PlayerStatusManager.Instance.GetSynergyBonus(weaponData.weaponTags, StatType.EliteDamageMult) + finalEliteDamageMult;

    public int GetFinalProjectileCount() => finalProjectileCount;
    public int GetFinalBurstCount() => finalBurstCount;
    public float GetFinalExplosionRadius() => finalExplosionRadius;
    public float GetFinalFieldDuration() => finalFieldDuration;
    public float GetFinalFieldTickInterval() => finalFieldTickInterval;
    public int GetFinalChainCount() => finalChainCount;
    public float GetFinalChainRange() => finalChainRange;
    public int GetFinalPierceCount() => finalPierceCount;
    public int GetFinalBounceCount() => finalBounceCount;
    public float GetFinalStatusDamage() => weaponData.statusDamage * finalStatusDmgMult;
    public float GetFinalStatusGauge() => weaponData.statusGaugeValue * finalStatusAccumMult;

    private Collider[] _detectionBuffer = new Collider[200];

    void Start()
    {
        if (targetLayer == 0)
        {
            targetLayer = isPlayerOwned ? LayerMask.GetMask("Enemy") : LayerMask.GetMask("Player");
        }

        UpdateFinalStats();

        if (!isPlayerOwned)
        {
            timer = finalFireRate;
        }
    }

    private void OnValidate()
    {
        if (weaponData != null) UpdateFinalStats();
    }

    protected virtual void Update()
    {
        timer += Time.deltaTime;

        Transform target = GetClosestTarget();
        if (target != null)
        {
            Vector3 targetDir = (target.position - transform.position).normalized;
            targetDir.y = 0;
            if (targetDir != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(targetDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
            }
        }

        if (CanFire())
        {
            if (isPlayerOwned || isAttackCommanded)
            {
                if (weaponData.isMelee || weaponData.isOrbit || weaponData.isLaser || target != null || isAttackCommanded)
                {
                    StartCoroutine(FireRoutine(target));
                    timer = 0f;
                }
            }
        }
    }
    private void OnEnable()
    {
        // 플레이어 스탯 변경
        if (PlayerStatusManager.Instance != null)
        {
            PlayerStatusManager.Instance.OnStatChanged += UpdateFinalStats;
        }
        else
        {
            StartCoroutine(WaitForStatusManager());
        }
        // 최종 스탯 갱신
        UpdateFinalStats();
    }
    private IEnumerator WaitForStatusManager()
    {
        yield return new WaitUntil(() => PlayerStatusManager.Instance != null);
        PlayerStatusManager.Instance.OnStatChanged += UpdateFinalStats;
        UpdateFinalStats();
    }

    public bool CanFire()
    {
        return !isFiring && timer >= finalFireRate;
    }

    public float GetTimer() => timer;

    public void SetOwner(bool playerOwned)
    {
        isPlayerOwned = playerOwned;
        targetLayer = isPlayerOwned ? LayerMask.GetMask("Enemy") : LayerMask.GetMask("Player");
    }
    //  ----- [레벨 및 최종 스탯] -----
    public List<AppliedUpgrade> upgradeHistory = new List<AppliedUpgrade>();
    public void ApplySelectedUpgrade(AppliedUpgrade chosen)
    {
        upgradeHistory.Add(chosen);
        currentLevel++;
        UpdateFinalStats(); // 스탯 갱신
    }

    public void SetLevel(int level)
    {
        currentLevel = level;
        UpdateFinalStats();
    }
    // --- [ 스탯 계산 로직 ] ---

    public void UpdateFinalStats()
    {
        if (weaponData == null) return;

        // 내부 무기 레벨업 수치 계산
        CalculateWeaponInternalStats();

        if (isPlayerOwned)
        {
            var player = PlayerStatusManager.Instance;
            if (player == null) return;

            ApplyPlayerModifiers(player);
        }
        else
        {
            if (targetLayer == 0) targetLayer = LayerMask.GetMask("Player");
            if (finalFireRate <= 0) finalFireRate = weaponData.baseFireRate;
            if (finalMaxDistance <= 0) finalMaxDistance = weaponData.maxDistance;
            if (finalSpeed <= 0) finalSpeed = weaponData.speed;
            if (finalDamage <= 0) finalDamage = weaponData.baseDamage;
        }
        detectRange = finalMaxDistance;
    }

    private void ApplyPlayerModifiers(PlayerStatusManager player)
    {
        var myTags = weaponData.weaponTags;

        // 시너지 보너스 수집 (% 단위)
        float dmgBonus = player.GetSynergyBonus(myTags, StatType.DamageMult);
        float asBonus = player.GetSynergyBonus(myTags, StatType.AttackSpeed);
        float areaBonus = player.GetSynergyBonus(myTags, StatType.AreaSize);
        float critChanceBonus = player.GetSynergyBonus(myTags, StatType.CritChance);
        float critDmgBonus = player.GetSynergyBonus(myTags, StatType.CritMultiplier);
        float projSpeedBonus = player.GetSynergyBonus(myTags, StatType.ProjectileSpeed);
        float projCountBonus = player.GetSynergyBonus(myTags, StatType.ProjectileCount);
        float pierceBonus = player.GetSynergyBonus(myTags, StatType.PierceCount);
        float bounceBonus = player.GetSynergyBonus(myTags, StatType.BounceCount);
        float chainBonus = player.GetSynergyBonus(myTags, StatType.ChainCount);

        // 데미지 배율 적용
        finalDamage += player.flatDamage.Value;
        finalDamage = (finalDamage * player.damageMult.Value) * (1.0f + dmgBonus);

        // --- 공격 속도(FireRate) 플레이어 스탯/시너지 적용 ---
        // 공식: 현재FireRate * (1 - 보너스 합산 / 100)
        float playerASBonus = player.attackSpeed.Value + asBonus;
        finalFireRate *= (1.0f - (playerASBonus / 100f));
        if (finalFireRate < 0.05f) finalFireRate = 0.05f;

        // 기타 스탯 배율 적용
        finalCritChance += (player.critChance.Value + critChanceBonus);
        finalCritMultiplier = weaponData.baseCritMultiplier + ((player.critDamage.Value + critDmgBonus) / 100f);

        int extraProj = Mathf.FloorToInt(player.projectileCount.Value + projCountBonus);
        if (fireMode == FireMode.Sequential)
        {
            finalBurstCount += extraProj;
        }
        else finalBurstCount += extraProj;

        float burstBonus = player.GetSynergyBonus(myTags, StatType.BurstCount);
        finalBurstCount += Mathf.FloorToInt(player.burstCount.Value);

        finalPierceCount += Mathf.FloorToInt(player.projectilePierce.Value + pierceBonus);
        finalBounceCount += Mathf.FloorToInt(player.projectileBounces.Value + bounceBonus);
        finalChainCount += Mathf.FloorToInt(player.projectileChain.Value + chainBonus);
        finalSpeed *= (player.projectileSpeed.Value + projSpeedBonus);

        float totalAreaMult = player.areaSize.Value + areaBonus;
        finalSize *= totalAreaMult;
        finalExplosionRadius *= totalAreaMult;
        finalMeleeRange *= totalAreaMult;

        finalFieldDuration *= player.duration.Value;
        finalKnockbackForce *= player.knockback.Value;

        // 상태 이상 상세 계산 호출
        ApplyStatusEffectCalculations(player);

        detectRange = finalMaxDistance;
    }

    private void CalculateWeaponInternalStats()
    {
        // 기본값 초기화
        finalDamage = weaponData.baseDamage;
        finalSpeed = weaponData.speed;
        finalSize = weaponData.size;
        finalMaxDistance = weaponData.maxDistance;
        finalCritChance = weaponData.baseCritChance;
        finalCritMultiplier = weaponData.baseCritMultiplier;
        finalProjectileCount = weaponData.baseProjectileCount;
        finalBurstCount = weaponData.baseBurstCount;
        finalFireRate = weaponData.baseFireRate;
        finalPierceCount = weaponData.pierceCount;
        finalBounceCount = weaponData.bounceCount;
        finalExplosionRadius = weaponData.explosionRadius;
        finalKnockbackForce = weaponData.knockbackForce;
        finalMeleeRange = weaponData.meleeRange;
        finalMeleeAngle = weaponData.meleeAngle;
        finalLaserWidth = weaponData.laserWidth;
        finalLaserTickInterval = weaponData.laserTickInterval;
        finalFieldDuration = weaponData.baseFieldDuration;
        finalFieldTickInterval = weaponData.fieldTickInterval;
        finalChainCount = weaponData.baseChainCount;
        finalChainRange = weaponData.chainRange;
        finalEliteDamageMult = 0f;
        finalStatusDmgMult = 1.0f;
        finalStatusAccumMult = 1.0f;
        currentSynergy = 0f;

        float internalASPercentSum = 0f;
        float internalCritDmgBonus = 0f;
        foreach (var upgrade in upgradeHistory)
        {
            switch (upgrade.statType)
            {
                case StatType.Damage: finalDamage += upgrade.value; break;
                case StatType.ProjectileSpeed: finalSpeed += upgrade.value; break;
                case StatType.AreaSize: finalSize += upgrade.value; break;
                case StatType.MaxDistance: finalMaxDistance += upgrade.value; break;
                case StatType.CritChance: finalCritChance += upgrade.value; break;
                case StatType.CritMultiplier: internalCritDmgBonus += upgrade.value; break;
                case StatType.EliteDamageMult: finalEliteDamageMult += upgrade.value; break;
                // 공속 보너스 % 합산
                case StatType.AttackSpeed: internalASPercentSum += upgrade.value; break;

                case StatType.ProjectileCount: finalProjectileCount += Mathf.FloorToInt(upgrade.value); break;
                case StatType.BurstCount: finalBurstCount += Mathf.FloorToInt(upgrade.value); break;
                case StatType.PierceCount: finalPierceCount += Mathf.FloorToInt(upgrade.value); break;
                case StatType.BounceCount: finalBounceCount += Mathf.FloorToInt(upgrade.value); break;

                // 상태 이상 관련 (동적 반영)
                case StatType.BurnDamageMult:
                case StatType.PoisonDamageMult:
                case StatType.ElectricDamageMult:
                case StatType.FreezeDamageMult:
                case StatType.ImpactDamageMult:
                    finalStatusDmgMult += upgrade.value; break;

                case StatType.BurnAccumulation:
                case StatType.PoisonAccumulation:
                case StatType.ElectricAccumulation:
                case StatType.FreezeAccumulation:
                case StatType.ImpactAccumulation:
                    finalStatusAccumMult += upgrade.value; break;

                case StatType.BurnEfficiency:
                case StatType.PoisonEfficiency:
                case StatType.ElectricSynergy:
                case StatType.StunEfficiency:
                case StatType.FreezeEfficiency:
                    currentSynergy += upgrade.value; break;

                case StatType.ExplosionRadius: finalExplosionRadius += upgrade.value; break;
                case StatType.KnockbackForce: finalKnockbackForce += upgrade.value; break;
                case StatType.MeleeRange: finalMeleeRange += upgrade.value; break;
                case StatType.MeleeAngle: finalMeleeAngle += upgrade.value; break;
                case StatType.LaserWidth: finalLaserWidth += upgrade.value; break;
                case StatType.FieldDuration: finalFieldDuration += upgrade.value; break;
                case StatType.ChainRange: finalChainRange += upgrade.value; break;
                case StatType.LaserTickInterval: finalLaserTickInterval -= upgrade.value; break;
                case StatType.FieldTickInterval: finalFieldTickInterval -= upgrade.value; break;
            }
        }
        // 무기 내부 공속 보너스 적용
        finalCritMultiplier = weaponData.baseCritMultiplier + (internalCritDmgBonus / 100f);
        finalFireRate *= (1.0f - (internalASPercentSum / 100f));
    }

    private void ApplyStatusEffectCalculations(PlayerStatusManager player)
    {
        var st = weaponData.statusType;
        if (st == StatusType.None) return;

        StatType accumKey = st switch
        {
            StatusType.Burn => StatType.BurnAccumulation,
            StatusType.Poison => StatType.PoisonAccumulation,
            StatusType.Electric => StatType.ElectricAccumulation,
            StatusType.Freeze => StatType.FreezeAccumulation,
            StatusType.Impact => StatType.ImpactAccumulation,
            _ => StatType.Damage
        };

        StatType effKey = st switch
        {
            StatusType.Burn => StatType.BurnEfficiency,
            StatusType.Poison => StatType.PoisonEfficiency,
            StatusType.Electric => StatType.ElectricSynergy,
            StatusType.Freeze => StatType.FreezeEfficiency,
            StatusType.Impact => StatType.StunEfficiency,
            _ => StatType.Damage
        };

        StatType dmgKey = st switch
        {
            StatusType.Burn => StatType.BurnDamageMult,
            StatusType.Poison => StatType.PoisonDamageMult,
            StatusType.Electric => StatType.ElectricDamageMult,
            StatusType.Freeze => StatType.FreezeDamageMult,
            StatusType.Impact => StatType.ImpactDamageMult,
            _ => StatType.Damage
        };

        float sAccumSyn = player.GetSynergyBonus(weaponData.weaponTags, accumKey);
        float sEffSyn = player.GetSynergyBonus(weaponData.weaponTags, effKey);
        float sDmgSyn = player.GetSynergyBonus(weaponData.weaponTags, dmgKey);

        // 축적 효율
        float bAccum = st switch
        {
            StatusType.Burn => player.burnAccumulation.Value,
            StatusType.Poison => player.poisonAccumulation.Value,
            StatusType.Electric => player.electricAccumulation.Value,
            StatusType.Freeze => player.freezeAccumulation.Value,
            StatusType.Impact => player.impactAccumulation.Value,
            _ => 1f
        };
        finalStatusAccumMult *= bAccum * (1.0f + sAccumSyn);

        // 상태 이상 대미지
        float bDmg = st switch
        {
            StatusType.Burn => player.burnDamageMult.Value,
            StatusType.Poison => player.poisonDamageMult.Value,
            StatusType.Freeze => player.freezeDamageMult.Value,
            StatusType.Electric => player.electricDamageMult.Value,
            StatusType.Impact => player.impactDamageMult.Value,
            _ => 1f
        };
        finalStatusDmgMult *= (bDmg + sDmgSyn);

        // 시너지 효율
        float bEff = st switch
        {
            StatusType.Electric => player.electricSynergy.Value,
            StatusType.Burn => player.burnEfficiency.Value,
            StatusType.Poison => player.poisonEfficiency.Value,
            StatusType.Freeze => player.freezeEfficiency.Value,
            StatusType.Impact => player.stunEfficiency.Value,
            _ => 0f
        };
        currentSynergy += bEff + sEffSyn;
    }

    public float GetSynergyBonus(List<WeaponTag> myTags, StatType stat)
    {
        return PlayerStatusManager.Instance.GetSynergyBonus(myTags, stat);
    }

    //  ----- [프로젝타일 생성 및 삭제] -----
    protected void SpawnProjectile(Vector3 spawnPos, Vector3 fireDir, float damage, Transform target, int index = 0, Vector3? targetPos = null)
    {
        if (weaponData.muzzleFlashPrefab != null)
        {
            // Calculate accurate spawn position for lasers relative to the Player, not the empty weapon logic object
            if (weaponData.isLaser)
            {
                Transform actualTarget = GameObject.FindWithTag("Player")?.transform ?? transform.root;
                spawnPos = actualTarget.TransformPoint(spawnOffset) + (fireDir * 0.3f);
            }

            GameObject muzzleObj = ObjectPoolManager.Instance.Get(weaponData.muzzleFlashPrefab, spawnPos, Quaternion.LookRotation(fireDir));
            
            if (weaponData.isLaser)
            {
                Transform actualTarget = GameObject.FindWithTag("Player")?.transform ?? transform.root;
                muzzleObj.transform.SetParent(actualTarget, true);
            }
            
            if (muzzleObj.TryGetComponent<ParticleSystem>(out var ps))
            {
                ps.Clear();
                ps.Play();
            }

            if (weaponData.isMelee && muzzleObj.TryGetComponent<SwordSlashEffect>(out var slash))
            {
                slash.PlaySlash(spawnPos, fireDir, finalMeleeRange, finalMeleeAngle, 0.2f);
            }
        }
        float eliteSynergy = PlayerStatusManager.Instance.GetSynergyBonus(weaponData.weaponTags, StatType.EliteDamageMult);

        ProjectileInfo info = new ProjectileInfo
        {
            damage = damage,
            speed = finalSpeed,
            size = finalSize,
            maxDistance = finalMaxDistance,
            critChance = finalCritChance,
            critMultiplier = finalCritMultiplier,
            eliteDamageMult = PlayerStatusManager.Instance.eliteDamage.Value + eliteSynergy + finalEliteDamageMult,

            pierceCount = finalPierceCount,
            bounceCount = finalBounceCount,
            explosionRadius = finalExplosionRadius,
            knockbackForce = finalKnockbackForce,
            targetPosition = targetPos ?? Vector3.zero,
            fieldDuration = finalFieldDuration,
            fieldTickInterval = finalFieldTickInterval,
            chainCount = finalChainCount,
            chainRange = finalChainRange,
            finalStatusDamage = weaponData.statusDamage * finalStatusDmgMult,
            finalStatusGauge = weaponData.statusGaugeValue * finalStatusAccumMult,
            dynamicStatusSynergy = currentSynergy
        };

        // 무기 타입에 따른 데이터
        if (weaponData.isOrbit)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            info.target = playerObj != null ? playerObj.transform : transform.root;
            float count = Mathf.Max(1, finalProjectileCount);
            info.orbitOffset = (index * (Mathf.PI * 2f / count));
        }
        else if (weaponData.isLaser || weaponData.isBoomerang)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            info.target = playerObj != null ? playerObj.transform : transform.root;

            if (weaponData.isLaser)
            {
                info.spawnOffset = spawnOffset;
                info.laserWidth = finalLaserWidth;
                info.laserTickInterval = finalLaserTickInterval;
            }
        }
        else if (weaponData.isMelee)
        {
            info.meleeRange = finalMeleeRange;
            info.meleeAngle = finalMeleeAngle;
        }

        GameObject obj = ObjectPoolManager.Instance.Get(weaponData.prefab, spawnPos, Quaternion.LookRotation(fireDir));
        if (obj == null)
        {
            Debug.LogWarning($"{weaponData.weaponName} 발사 실패: 오브젝트 풀이 가득 찼습니다.");
            return;
        }

        if (obj.TryGetComponent<Projectile>(out var proj))
        {
            if (!spawnedProjectiles.Contains(proj)) spawnedProjectiles.Add(proj);
            proj.Init(weaponData, info, this);
        }
    }
    public void RemoveFromSpawnedList(Projectile proj)
    {
        if (spawnedProjectiles.Contains(proj))
        {
            spawnedProjectiles.Remove(proj);
        }
    }
    public void ClearAllProjectiles()
    {
        if (spawnedProjectiles == null || spawnedProjectiles.Count == 0) return;
        List<Projectile> tempCloneList = new List<Projectile>(spawnedProjectiles);

        foreach (var proj in tempCloneList)
        {
            if (proj != null && proj.gameObject.activeInHierarchy)
            {
                proj.Release();
            }
        }
        spawnedProjectiles.Clear();
    }
    private void OnDisable()
    {
        if (PlayerStatusManager.Instance != null)
        {
            PlayerStatusManager.Instance.OnStatChanged -= UpdateFinalStats;
        }

        ClearAllProjectiles();
    }
    private void OnDestroy()
    {
        ClearAllProjectiles();
    }

    //  ----- [발사 로직] -----
    private Vector3 GetTargetCenter(Transform t)
    {
        if (t == null) return transform.position;

        if (t.TryGetComponent<IDamageable>(out var damageable))
        {
            return damageable.GetCenterPosition();
        }
        return t.position + Vector3.up * targetHeightOffset;
    }

    private bool IsTargetDead(Transform t)
    {
        if (t.TryGetComponent<SimpleEnemy>(out var enemy))
        {
            return enemy.currentHp <= 0;
        }
        return false;
    }

    private IEnumerator FireRoutine(Transform initialTarget)
    {
        isFiring = true;

        Transform currentTarget = initialTarget;
        if (currentTarget == null) { 
            currentTarget = weaponData.isMelee ? GetBestMeleeTarget() : GetClosestTarget();
       
        }

        if (!isPlayerOwned && currentTarget == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) currentTarget = p.transform;
        }
        float damageToApply = finalDamage * damageMultiplier;
        Vector3 fireDirection = transform.forward;

        int attackCycles = finalBurstCount;
        for (int b = 0; b < attackCycles; b++)
        {
            if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy || IsTargetDead(currentTarget))
            {
                currentTarget = GetClosestTarget();
            }

            // 방향 최신화
            if (currentTarget != null)
            {
                Vector3 targetCenter = GetTargetCenter(currentTarget);
                Vector3 spawnPos = transform.TransformPoint(spawnOffset);
                Vector3 targetDir = (targetCenter - spawnPos).normalized;

                if (targetDir != Vector3.zero) fireDirection = targetDir;
            }

            // 실제 발사 실행
            if (weaponData.isMelee) ExecuteMeleeAttack(damageToApply, currentTarget);
            else if (weaponData.isOrbit) ExecuteOrbitFire(damageToApply);
            else if (weaponData.isLaser) ExecuteLaserFire(fireDirection, damageToApply);
            else if (weaponData.isMeteor)
            {
                yield return StartCoroutine(ExecuteMeteorFallRoutine(damageToApply));
            }
            else
            {
                if (fireMode == FireMode.Scatter)
                    ExecuteScatterFire(fireDirection, damageToApply);
                else if (fireMode == FireMode.Sequential)
                    ExecuteSingleSequentialShot(fireDirection, damageToApply);
                else if (fireMode == FireMode.Swarm)
                {
                    yield return StartCoroutine(ExecuteSwarmRoutine(fireDirection, damageToApply));
                }
            }

            if (attackCycles > 1 && b < attackCycles - 1)
            {
                yield return new WaitForSeconds(burstInterval);
            }
        }
        if (!isPlayerOwned) isFiring = false;
        yield return new WaitForSeconds(finalFireRate);
        isFiring = false;
    }

    // ----- [1. 분사형 로직 (Scatter)] -----
    private void ExecuteScatterFire(Vector3 fixedDir, float damage)
    {
        Vector3 spawnPos = transform.root.TransformPoint(spawnOffset);
        PlayFireSound();

        // 부채꼴 시작 각도 계산
        float startAngle = -(spreadAngle * (finalProjectileCount - 1)) / 2f;

        for (int i = 0; i < finalProjectileCount; i++)
        {
            float currentAngle = startAngle + (spreadAngle * i);
            Quaternion rotation = Quaternion.Euler(0, currentAngle, 0);
            Vector3 fireDir = rotation * fixedDir;

            SpawnProjectile(spawnPos, fireDir, damage, null, i); // 산탄은 유도 제외
        }
    }

    //  ----- [2. 연사형 로직 (Sequential)] -----
    private void ExecuteSingleSequentialShot(Vector3 fixedDir, float damage)
    {
        PlayFireSound();
        Vector3 currentSpawnPos = transform.TransformPoint(spawnOffset);
        SpawnProjectile(currentSpawnPos, fixedDir, damage, null);
    }

    //  ----- [3. 메테오 로직] -----
    private IEnumerator ExecuteMeteorFallRoutine(float damage)
    {
        Vector3 centerPos = GameObject.FindWithTag("Player").transform.position;

        for (int i = 0; i < finalProjectileCount; i++)
        {
            PlayFireSound();

            Vector3 fallTargetPos;
            if (weaponData.isMeteorTargeted)
            {
                Transform enemyTarget = GetRandomTargetInRange();

                if (enemyTarget != null)
                {
                    fallTargetPos = GetTargetCenter(enemyTarget);
                }
                else
                {
                    Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * finalMaxDistance;
                    fallTargetPos = centerPos + new Vector3(randomOffset.x, 0, randomOffset.y);
                }
            }

            // 완전 무작위 랜덤 모드
            else
            {
                Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * finalMaxDistance;
                fallTargetPos = centerPos + new Vector3(randomOffset.x, 0, randomOffset.y);
            }

            // 지형 높이 보정
            if (Physics.Raycast(fallTargetPos + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f, LayerMask.GetMask("Floor", "Environment", "Spawnable")))
            {
                fallTargetPos.y = hit.point.y;
            }
            else
            {
                fallTargetPos.y = 0f;
            }

            Vector3 spawnPos = fallTargetPos + Vector3.up * weaponData.meteorSpawnHeight;
            SpawnProjectile(spawnPos, Vector3.down, damage, null, i, fallTargetPos);

            if (i < finalProjectileCount - 1)
                yield return new WaitForSeconds(0.15f);
        }
    }

    //  ----- [4. 근접 공격 로직] -----
    private void ExecuteMeleeAttack(float damage, Transform target)
    {
        Transform actualOwner = transform;
        while (actualOwner.parent != null && !actualOwner.CompareTag("Player") && !actualOwner.CompareTag("Enemy"))
        {
            actualOwner = actualOwner.parent;
        }

        Vector3 ownerPos = actualOwner.position;
        Vector3 ownerForward = actualOwner.forward;

        Vector3 spawnPos = ownerPos + (ownerForward * spawnOffset.z) + (Vector3.up * spawnOffset.y);
        Vector3 fireDir = ownerForward;

        if (target != null)
        {
            fireDir = (GetTargetCenter(target) - ownerPos).normalized;
            fireDir.y = 0;
        }

        PlayFireSound();

        if (fireMode == FireMode.Scatter)
        {
            float startAngle = -(spreadAngle * (finalProjectileCount - 1)) / 2f;
            for (int i = 0; i < finalProjectileCount; i++)
            {
                float currentAngle = startAngle + (spreadAngle * i);
                Vector3 finalDir = Quaternion.Euler(0, currentAngle, 0) * fireDir;
                SpawnProjectile(spawnPos, finalDir, damage, null, i);
            }
        }
        else
        {
            SpawnProjectile(spawnPos, fireDir, damage, null);
        }
    }

    //  ----- [5. 공전 무기 로직] -----
    private void ExecuteOrbitFire(float damage)
    {
        PlayFireSound();
        for (int i = 0; i < finalProjectileCount; i++)
        {
            SpawnProjectile(transform.position, transform.forward, damage, null, i);
        }
    }

    //  ----- [6. 레이저 공격 로직] -----
    private void ExecuteLaserFire(Vector3 fixedDir, float damage)
    {
        PlayFireSound();
        Vector3 spawnPos = transform.TransformPoint(spawnOffset);

        //Scatter일 때 여러 갈래로 나가도록 루프 추가
        if (fireMode == FireMode.Scatter)
        {
            float startAngle = -(spreadAngle * (finalProjectileCount - 1)) / 2f;

            for (int i = 0; i < finalProjectileCount; i++)
            {
                float currentAngle = startAngle + (spreadAngle * i);
                Vector3 fireDir = Quaternion.Euler(0, currentAngle, 0) * fixedDir;
                SpawnProjectile(spawnPos, fireDir, damage, null, i);
            }
        }
        else
        {
            // Sequential일 경우, FireRoutine의 루프에 의해 짧은 간격으로 레이저가 재생성 됨
            SpawnProjectile(spawnPos, fixedDir, damage, null);
        }
    }

    // ----- [7. 카이사 같은 유도탄 로직] -----
    private IEnumerator ExecuteSwarmRoutine(Vector3 fixedDir, float damage)
    {
        PlayFireSound(); // 쏠 때 한 번 소리 재생

        for (int i = 0; i < finalProjectileCount; i++)
        {
            Vector3 spawnPos = transform.root.TransformPoint(spawnOffset);

            // X, Y축 양방향으로 흩어지는 랜덤 곡사포 방향 생성
            float randX = UnityEngine.Random.Range(-spreadAngle, spreadAngle);
            float randY = UnityEngine.Random.Range(-spreadAngle, spreadAngle);
            Vector3 randomDir = Quaternion.Euler(randX, randY, 0) * fixedDir;

            // 카이사 Q의 핵심: 여러 명의 적에게 골고루 조준이 나뉘도록 랜덤 타겟팅
            Transform individualTarget = GetRandomTargetInRange() ?? GetClosestTarget();

            SpawnProjectile(spawnPos, randomDir, damage, individualTarget, i);

            // 미사일 사이의 아주 짧은 발사 간격 (0.1초)
            if (i < finalProjectileCount - 1)
                yield return new WaitForSeconds(0.1f);
        }
    }

    private Transform GetRandomTargetInRange()
    {
        Vector3 center = transform.root.position;
        int count = Physics.OverlapSphereNonAlloc(center, detectRange, _detectionBuffer, targetLayer);

        List<Transform> validTargets = new List<Transform>();
        for (int i = 0; i < count; i++)
        {
            Collider col = _detectionBuffer[i];
            if (col != null && col.gameObject.activeInHierarchy)
            {
                if (col.TryGetComponent<IDamageable>(out var dmg))
                {
                    if (dmg is SimpleEnemy se && se.currentHp <= 0) continue;
                    validTargets.Add(col.transform);
                }
            }
        }

        if (validTargets.Count > 0)
        {
            return validTargets[UnityEngine.Random.Range(0, validTargets.Count)];
        }
        return null;
    }

    //  ----- [가까운 적 찾기] -----
    private Transform GetClosestTarget()
    {
        Vector3 center = transform.root.position;
        int count = Physics.OverlapSphereNonAlloc(center, detectRange, _detectionBuffer, targetLayer);

        Transform closest = null;
        Transform priorityTarget = null;

        float minDistanceSqr = Mathf.Infinity;
        float minPriorityDistSqr = Mathf.Infinity;

        for (int i = 0; i < count; i++)
        {
            Collider col = _detectionBuffer[i];
            if (col != null && col.gameObject.activeInHierarchy)
            {
                // IDamageable 인터페이스가 있는지, 그리고 살아있는지 확인
                if (col.TryGetComponent<IDamageable>(out var damageable))
                {
                    // 적이라면 체력 체크 (SimpleEnemy일 때만)
                    if (damageable is SimpleEnemy se && se.currentHp <= 0) continue;
                    float distSqr = (col.transform.position - center).sqrMagnitude;

                    bool isBoss = false;
                    if (damageable is SimpleEnemy enemy)
                    {
                        isBoss = enemy.isElite;
                    }

                    if (isBoss)
                    {
                        if (distSqr < minPriorityDistSqr)
                        {
                            minPriorityDistSqr = distSqr;
                            priorityTarget = col.transform;
                        }
                    }
                    else
                    {
                        if (distSqr < minDistanceSqr)
                        {
                            minDistanceSqr = distSqr;
                            closest = col.transform;
                        }
                    }
                }
            }
        }
        return priorityTarget != null ? priorityTarget : closest;
    }

    private Transform GetBestMeleeTarget()
    {
        int count = Physics.OverlapSphereNonAlloc(transform.root.position, detectRange, _detectionBuffer, targetLayer);
        if (count == 0) return null;

        Transform bestTarget = null;
        int maxNearbyCount = -1;
        float minDistanceSqr = Mathf.Infinity;

        int checkLimit = Mathf.Min(count, 10);
        for (int i = 0; i < checkLimit; i++)
        {
            Collider candidate = _detectionBuffer[i];
            if (candidate == null) continue;

            Collider[] nearby = Physics.OverlapSphere(candidate.transform.position, 3f, targetLayer);
            int nearbyCount = nearby.Length;

            float distSqr = (candidate.transform.position - transform.position).sqrMagnitude;
            if (nearbyCount > maxNearbyCount || (nearbyCount == maxNearbyCount && distSqr < minDistanceSqr))
            {
                maxNearbyCount = nearbyCount;
                minDistanceSqr = distSqr;
                bestTarget = candidate.transform;
            }
        }
        return bestTarget;
    }

    // ----- [사운드 재생] -----
    private void PlayFireSound()
    {
        if (weaponData.fireSound == null || weaponData.soundPlayerPrefab == null) return;

        GameObject obj = ObjectPoolManager.Instance.Get(weaponData.soundPlayerPrefab, transform.position, Quaternion.identity);
        if (obj.TryGetComponent<SoundPlayer>(out var player))
        {
            player.Play(weaponData.fireSound, weaponData.fireSoundVolume, weaponData.fireSoundDuration);
        }
    }

    //  ----- [범위 디버깅] -----
    // 디버그: 발사 위치 확인 (빨간 점)
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.TransformPoint(spawnOffset), 0.1f);
    }

    // 디버그: 사거리 확인 (하늘색 원)
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 1, 0.3f);
        Vector3 center = (Application.isPlaying) ? transform.root.position : transform.position;

        Gizmos.matrix = Matrix4x4.TRS(center, Quaternion.identity, new Vector3(1, 0.01f, 1));
        Gizmos.DrawWireSphere(Vector3.zero, (weaponData != null) ? weaponData.maxDistance : detectRange);
        Gizmos.matrix = Matrix4x4.identity;
    }
}
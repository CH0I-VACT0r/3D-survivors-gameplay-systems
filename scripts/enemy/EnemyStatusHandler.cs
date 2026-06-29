using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyStatusHandler : MonoBehaviour
{
    private IEnemy owner;
    private SimpleEnemy enemyScript;
    private StatusVFXHandler vfxHandler;

    [Header("Status Immunity Thresholds")]
    public float maxImpactGauge = 10f;
    public float maxBurnGauge = 10f;
    public float maxFreezeGauge = 10f;
    public float maxElectricGauge = 10f;
    public float maxPoisonGauge = 10f;

    [Header("Status Settings")]
    public float globalImmunityDuration = 5f;
    public float stunDuration = 1f;
    public float freezeDuration = 2f;
    public float electricStatusDuration = 3f;
    public float poisonStatusDuration = 3f;

    [Header("Status Damage Ratios")]
    [Tooltip("화상 축적 중 초당 체력 비례 데미지 배율 (0.005 = 0.5%)")]
    public float burnDotRatio = 0.005f;
    [Tooltip("화상 폭발 시 체력 비례 데미지 배율 (0.1 = 10%)")]
    public float burnExplosionRatio = 0.1f;
    [Tooltip("중독 축적 중 초당 체력 비례 데미지 배율 (0.003 = 0.3%)")]
    public float poisonDotRatio = 0.003f;
    [Tooltip("중독 오라가 주변에 입히는 고정 데미지")]
    public float poisonAuraBaseDamage = 5f;

    // --- [밸런스 조절 계수] ---
    [Header("밸런스 조절 계수")]
    [Tooltip("CC 지속 시간 성장 억제")]
    private const float CC_DURATION_FACTOR = 0.3f;    // CC 지속 시간 성장 억제 (제곱근 적용)
    [Tooltip("화상 대미지 성장 완화")]
    private const float BURN_DAMAGE_FACTOR = 0.5f;    // 화상 대미지 성장 완화 (직선 적용)
    [Tooltip("독 범위 성장 억제")]
    private const float POISON_RANGE_FACTOR = 0.25f;  // 독 범위 성장 억제 (제곱근 적용)

    private Dictionary<StatusType, float> currentGauges = new Dictionary<StatusType, float>();
    private HashSet<StatusType> activeStatus = new HashSet<StatusType>();
    private bool isGlobalImmune = false;

    void Awake()
    {
        owner = GetComponent<IEnemy>();
        enemyScript = GetComponent<SimpleEnemy>();
        vfxHandler = GetComponent<StatusVFXHandler>();

        foreach (StatusType type in System.Enum.GetValues(typeof(StatusType)))
        {
            if (type != StatusType.None) currentGauges[type] = 0f;
        }
    }
    public bool IsStatusActive(StatusType type)
    {
        return activeStatus.Contains(type);
    }
    public void AddStatusValue(StatusDamageInfo info)
    {
        if (info.type == StatusType.Impact && activeStatus.Contains(StatusType.Freeze)) return;

        var player = PlayerStatusManager.Instance;
        if (player == null) return;

        // --- 대미지 계산 및 적용 ---
        if (info.damage > 0)
        {
            float finalDmg = info.damage;

            // [감전 특수 로직]
            if (info.type == StatusType.Electric)
            {
                if (activeStatus.Contains(StatusType.Electric))
                {
                    finalDmg *= (1.2f + info.synergy);
                }
                else
                {
                    float gaugeRatio = currentGauges[StatusType.Electric] / GetMaxGauge(StatusType.Electric);
                    finalDmg *= (1.2f + gaugeRatio);
                }
            }
            enemyScript.ProcessStatusDirectDamage(finalDmg, info.type);
        }
        if (activeStatus.Contains(info.type) || isGlobalImmune) return;

        // --- [게이지 누적 및 트리거] ---
        currentGauges[info.type] += info.gaugeValue;
        HandleAccumulationEffect(info.type, info.synergy);

        // 게이지 폭발 체크
        if (currentGauges[info.type] >= GetMaxGauge(info.type))
        {
            TriggerStatusEffect(info.type, info.synergy);
        }
    }

    private void HandleAccumulationEffect(StatusType type, float synergy)
    {
        var player = PlayerStatusManager.Instance;
        if (player == null) return;

        switch (type)
        {
            case StatusType.Burn:
                float burnDot = Mathf.Max(1f, enemyScript.maxHp * burnDotRatio * (1.0f + synergy));
                enemyScript.ProcessStatusDirectDamage(burnDot, StatusType.Burn);
                break;
            case StatusType.Freeze:
                float freezeRatio = currentGauges[StatusType.Freeze] / maxFreezeGauge;
                float slowAmount = freezeRatio * 0.5f * (1.0f + synergy);
                enemyScript.SetSpeedMultiplier(Mathf.Clamp(1f - slowAmount, 0.1f, 1f));
                break;
            case StatusType.Poison:
                float poisonDot = Mathf.Max(1f, enemyScript.maxHp * poisonDotRatio * (1.0f + synergy));
                enemyScript.ProcessStatusDirectDamage(poisonDot, StatusType.Poison);
                break;
        }
    }

    private void TriggerStatusEffect(StatusType type, float synergy)
    {
        activeStatus.Add(type);
        currentGauges[type] = 0f;

        if (vfxHandler != null)
        {
            vfxHandler.PlayOneShotVFX(type);            // 화상 폭발, 독구름, 감전 파티클
            vfxHandler.TogglePersistentVFX(type, true); // 화염, 빙결 지속 파티클
        }

        switch (type)
        {
            case StatusType.Impact: StartCoroutine(StunRoutine(synergy)); break;
            case StatusType.Burn: ExecuteBurnExplosion(synergy); break;
            case StatusType.Freeze: StartCoroutine(FreezeRoutine(synergy)); break;
            case StatusType.Electric: StartCoroutine(ElectricRoutine(synergy)); break;
            case StatusType.Poison: StartCoroutine(PoisonAuraRoutine(synergy)); break;
        }
    }

    // ----- [상태 이상별 상세 로직] -----

    private IEnumerator StunRoutine(float synergy)
    {
        enemyScript.SetAiActive(false);
        float dur = stunDuration * (1.0f + Mathf.Sqrt(synergy) * CC_DURATION_FACTOR);
        yield return new WaitForSeconds(dur);
        enemyScript.SetAiActive(true);
        EndStatus(StatusType.Impact);
    }

    private void ExecuteBurnExplosion(float synergy)
    {
        // 본체 폭발 대미지
        float explosionDamage = enemyScript.maxHp * burnExplosionRatio * (1.0f + synergy * BURN_DAMAGE_FACTOR);
        enemyScript.ProcessStatusDirectDamage(explosionDamage, StatusType.Burn);
        EndStatus(StatusType.Burn);
    }

    private IEnumerator FreezeRoutine(float synergy)
    {
        float finalDur = freezeDuration * (1.0f + Mathf.Sqrt(synergy) * CC_DURATION_FACTOR);

        enemyScript.SetSpeedMultiplier(0f);
        yield return new WaitForSeconds(finalDur);

        enemyScript.SetSpeedMultiplier(1f);
        EndStatus(StatusType.Freeze);
    }

    private IEnumerator ElectricRoutine(float synergy)
    {
        float elapsed = 0;
        float tickInterval = 1f;
        float dur = electricStatusDuration * (1.0f + synergy);

        while (elapsed < dur)
        {
            if (vfxHandler != null) vfxHandler.PlayOneShotVFX(StatusType.Electric);

            yield return new WaitForSeconds(tickInterval);
            elapsed += tickInterval;
        }
        EndStatus(StatusType.Electric);
    }

    private IEnumerator PoisonAuraRoutine(float synergy)
    {
        float elapsed = 0;
        float tickInterval = 1.0f;
        float maxDuration = poisonStatusDuration * (1.0f + synergy);
        float currentRange = 1.0f + (Mathf.Sqrt(synergy) * POISON_RANGE_FACTOR);

        while (elapsed < maxDuration)
        {
            if (vfxHandler != null) vfxHandler.PlayOneShotVFX(StatusType.Poison);

            Collider[] nearbyEnemies = Physics.OverlapSphere(transform.position, currentRange, enemyScript.enemyLayer);
            foreach (var col in nearbyEnemies)
            {
                if (col.gameObject != gameObject && col.CompareTag("Enemy"))
                {
                    if (col.TryGetComponent<IEnemy>(out var other))
                    {
                        float finalAuraDmg = poisonAuraBaseDamage * (1.0f + synergy);
                        other.TakeDamage(finalAuraDmg, false, this.gameObject);
                    }
                }
            }
            yield return new WaitForSeconds(tickInterval);
            elapsed += tickInterval;
        }
        enemyScript.SetSpeedMultiplier(1f);
        EndStatus(StatusType.Poison);
    }

    private void EndStatus(StatusType type)
    {
        activeStatus.Remove(type);
        if (vfxHandler != null) vfxHandler.TogglePersistentVFX(type, false);
        StartCoroutine(GlobalImmuneRoutine());
    }

    private IEnumerator GlobalImmuneRoutine()
    {
        isGlobalImmune = true;
        List<StatusType> keys = new List<StatusType>(currentGauges.Keys);
        foreach (var key in keys) currentGauges[key] = 0f;

        yield return new WaitForSeconds(globalImmunityDuration);
        isGlobalImmune = false;
    }

    private float GetMaxGauge(StatusType type)
    {
        return type switch
        {
            StatusType.Impact => maxImpactGauge,
            StatusType.Burn => maxBurnGauge,
            StatusType.Freeze => maxFreezeGauge,
            StatusType.Electric => maxElectricGauge,
            StatusType.Poison => maxPoisonGauge,
            _ => 10f
        };
    }

    public void ResetGauges()
    {
        StopAllCoroutines();

        List<StatusType> keys = new List<StatusType>(currentGauges.Keys);
        foreach (var key in keys)
        {
            currentGauges[key] = 0f;
        }

        activeStatus.Clear();
        isGlobalImmune = false;

        if (vfxHandler != null) vfxHandler.ClearAllVFX();
    }
}
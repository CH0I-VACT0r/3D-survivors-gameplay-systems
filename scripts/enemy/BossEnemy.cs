using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class BossEnemy : SimpleEnemy
{
    [Header("Boss Patterns")]
    [Tooltip("사용할 무기 프리팹 리스트")]
    public List<Weapon> weaponPrefabs;

    // 실제 생성된 무기 인스턴스들을 관리
    private List<Weapon> instantiatedWeapons = new List<Weapon>();
    private Weapon currentActiveWeapon;

    private float patternDelayTimer = 0f;
    public float timeBetweenPatterns = 3f;
    protected override void Awake()
    {
        base.Awake();
        instantiatedWeapons.Clear();
        foreach (var prefab in weaponPrefabs)
        {
            if (prefab == null) continue;
            Weapon instance = Instantiate(prefab, transform);
            instance.isPlayerOwned = false;
            instance.targetLayer = LayerMask.GetMask("Player");
            instance.UpdateFinalStats();
            instance.gameObject.SetActive(true);
            instantiatedWeapons.Add(instance);
        }
        if (instantiatedWeapons.Count > 0)
        {
            equippedWeapon = instantiatedWeapons[0];
        }
    }

    protected override void OnEnable()
    {
        this.enabled = true;
        base.OnEnable();
        isAiActive = true;
        currentState = EnemyState.Idle;

        if (instantiatedWeapons != null)
        {
            foreach (var w in instantiatedWeapons)
            {
                if (w != null) w.gameObject.SetActive(true);
            }
        }
    }

    protected override void UpdateState()
    {
        if (currentState == EnemyState.Die || !isAiActive) return;
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        EnemyState previousState = currentState;

        if (currentState == EnemyState.Attack)
        {
            if (currentActiveWeapon != null && !currentActiveWeapon.isFiring)
            {
                currentActiveWeapon.isAttackCommanded = false;
                currentActiveWeapon = null;
                currentState = EnemyState.Idle;
                patternDelayTimer = timeBetweenPatterns;

                Debug.Log("<color=cyan>패턴 종료: 휴식 단계로 진입</color>");
            }
            UpdateAnimationAndState(previousState);
            return;
        }
        if (patternDelayTimer > 0)
        {
            patternDelayTimer -= Time.deltaTime;
            currentState = EnemyState.Walk;
            UpdateAnimationAndState(previousState);
        }
        else
        {
            Weapon readyWeapon = GetReadyWeapon(distance);
            if (readyWeapon != null)
            {
                currentActiveWeapon = readyWeapon;
                currentState = EnemyState.Attack;
            }
            else if (distance > attackRange)
            {
                currentState = EnemyState.Walk;
            }
            else
            {
                currentState = EnemyState.Idle;
            }
        }
        if (previousState != currentState) OnStateChanged(previousState, currentState);
        UpdateAnimationAndState(previousState);
    }
    private void UpdateAnimationAndState(EnemyState previousState)
    {
        if (previousState != currentState)
        {
            OnStateChanged(previousState, currentState);
        }
        UpdateAnimationValues();
    }

    private Weapon GetReadyWeapon(float distance)
    {
        foreach (var w in instantiatedWeapons)
        {
            if (w.CanFire() && distance <= w.weaponData.maxDistance)
            {
                return w;
            }
        }
        return null;
    }

    protected override void OnStateChanged(EnemyState from, EnemyState to)
    {
        foreach (var w in instantiatedWeapons) w.isAttackCommanded = false;

        if (to == EnemyState.Attack && currentActiveWeapon != null)
        {
            currentActiveWeapon.isAttackCommanded = true;
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            if (anim != null)
            {
                int weaponIndex = instantiatedWeapons.IndexOf(currentActiveWeapon);
                string triggerName = $"Attack {weaponIndex + 1}";
                anim.SetTrigger(triggerName);
                Debug.Log($"<color=orange>보스 패턴 시작: {triggerName}</color>");
            }
        }
    }
}
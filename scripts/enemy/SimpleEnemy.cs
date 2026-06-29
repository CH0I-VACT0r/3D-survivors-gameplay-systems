using UnityEngine;
using System.Collections;
using System.ComponentModel;

public class SimpleEnemy : MonoBehaviour, IEnemy, IDamageable
{
    public enum EnemyState { Idle, Walk, Attack, Die }

    [Header("Stats")]
    public float maxHp = 10f;
    public float currentHp;
    public float moveSpeed = 1f;
    public float rotationSpeed = 10f;
    public bool isElite = false;
    public bool IsElite => isElite;

    [Header("Targeting")]
    public Transform centerPoint;
    protected Transform player;

    [Header("Weapon System")]
    [Tooltip("적에게 장착시킬 무기 프리팹 혹은 인스턴스")]
    public Weapon equippedWeapon;
    public float attackRange = 1.5f; // 무기 사거리에 맞춰 설정

    [Header("Detection & Avoidance")]
    public LayerMask wallLayer;      // 벽 레이어
    public LayerMask enemyLayer;     // 적 레이어
    public float wallCheckDist = 1.5f; // 벽 감지 거리
    public float separationDist = 0.5f; // 적끼리 유지할 거리
    public float climbForce = 6f;    // 벽 기어오르기 힘

    [Header("References")]
    protected Rigidbody rb;
    protected EnemyStatusHandler statusHandler;
    protected Animator anim;
    protected MeshRenderer meshRenderer;
    protected Color originalColor;

    [Header("Feedback")]
    public GameObject damagePopupPrefab;

    [Header("Audio")]
    protected AudioSource audioSource;
    public AudioClip deathClip;

    protected EnemyState currentState = EnemyState.Idle;
    protected float speedMultiplier = 1f;
    protected bool isAiActive = true;
    protected bool isKnockbackActive = false;
    protected float orbitAngle;
    private bool isClimbing = false;
    private float climbTimer = 0f;
    private const float climbStopDelay = 0.2f;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        statusHandler = GetComponent<EnemyStatusHandler>();
        anim = GetComponentInChildren<Animator>();
        meshRenderer = GetComponentInChildren<MeshRenderer>();
        if (meshRenderer != null) originalColor = meshRenderer.material.color;

        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        // 무기 초기 설정
        if (equippedWeapon != null)
        {
            Weapon weaponInstance = Instantiate(equippedWeapon, transform);
            equippedWeapon = weaponInstance;
            equippedWeapon.transform.localPosition = Vector3.zero;
            equippedWeapon.transform.localRotation = Quaternion.identity;

            equippedWeapon.isPlayerOwned = false;
            equippedWeapon.targetLayer = LayerMask.GetMask("Player");
            equippedWeapon.UpdateFinalStats();
        }
    }

    protected virtual void OnEnable()
    {
        // 이전 생애주기에서 남은 Invoke 취소 (풀에서 재활용 시 안전장치)
        CancelInvoke();

        if (TryGetComponent<Collider>(out var col)) col.enabled = true;
        rb.isKinematic = false;

        if (EnemyDifficultyManager.Instance != null)
        {
            // 현재 난이도 배율 적용
            float multiplier = EnemyDifficultyManager.Instance.GetCurrentHpMultiplier();
            float scaledMaxHp = maxHp * multiplier;
            currentHp = scaledMaxHp;

            if (isElite)
            {
                currentHp *= 10f;
            }
        }
        else
        {
            currentHp = maxHp;
        }

        currentState = EnemyState.Idle;
        isAiActive = true;
        speedMultiplier = 1f;
        isKnockbackActive = false;
        gameObject.layer = LayerMask.NameToLayer("Enemy");
        orbitAngle = UnityEngine.Random.Range(0f, 360f);

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            Vector3 lookDir = (player.position - transform.position).normalized;
            lookDir.y = 0; 

            if (lookDir != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(lookDir);
            }
        }

        if (equippedWeapon != null)
        {
            // 1. 오브젝트를 강제로 켠다
            equippedWeapon.gameObject.SetActive(true);
            // 2. 스크립트를 강제로 켠다
            equippedWeapon.enabled = true;
            equippedWeapon.SetOwner(false);
            // 3. 적용 여부 확인 로그
        }
        else
        {
            Debug.LogError($"{gameObject.name}에 연결된 무기가 없습니다!");
        }
    }
    protected virtual void Update()
    {
        if (currentState == EnemyState.Die || !isAiActive) return;
        UpdateState();
    }

    protected virtual void FixedUpdate()
    {
        if (currentState == EnemyState.Die || !isAiActive || isKnockbackActive) return;

        // 벽에서 떨어지면 기어오름 중지
        if (isClimbing)
        {
            climbTimer -= Time.fixedDeltaTime;
            if (climbTimer <= 0f) isClimbing = false;
        }

        if (currentState == EnemyState.Walk)
        {
            MoveToPlayer();
        }
        else
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            RotateToPlayer();
        }
    }

    public Vector3 GetCenterPosition()
    {
        return centerPoint != null ? centerPoint.position : transform.position + Vector3.up * 1.0f;
    }

    protected bool IsTargetVisible(Camera cam, GameObject obj)
    {
        if (cam == null) return false;
        Vector3 screenPoint = cam.WorldToViewportPoint(obj.transform.position);
        return screenPoint.z > 0 && screenPoint.x > 0 && screenPoint.x < 1 && screenPoint.y > 0 && screenPoint.y < 1;
    }

    protected virtual void UpdateState()
    {
        if (currentState == EnemyState.Die || !isAiActive) return;
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        EnemyState previousState = currentState;
        bool canAttack = equippedWeapon != null && equippedWeapon.CanFire();

        if (currentState == EnemyState.Attack)
        {
            if (!canAttack || distance > attackRange * 1.2f)
            {
                currentState = EnemyState.Idle;
            }
            if (previousState != currentState) OnStateChanged(previousState, currentState);
            return;
        }
        if (canAttack && distance <= attackRange)
        {
            currentState = EnemyState.Attack;
        }
        else if (distance > attackRange * 0.8f) // 사거리의 80%보다 멀어지면 추적
        {
            currentState = EnemyState.Walk;
        }
        else
        {
            currentState = EnemyState.Idle;
        }
        if (previousState != currentState) OnStateChanged(previousState, currentState);
        UpdateAnimationValues();
    }

    protected virtual void OnStateChanged(EnemyState from, EnemyState to)
    {
        if (equippedWeapon == null) return;

        switch (to)
        {
            case EnemyState.Attack:
                equippedWeapon.isAttackCommanded = true;
                if (anim != null) anim.SetTrigger("Attack");
                break;
            default: // Walk, Idle 등
                equippedWeapon.isAttackCommanded = false;
                break;
        }
    }

    protected virtual void UpdateAnimationValues()
    {
        if (anim == null) return;

        if (currentState == EnemyState.Walk)
        {
            anim.SetFloat("Speed", 1f);
            MoveToPlayer();
        }
        else
        {
            anim.SetFloat("Speed", 0f);
            RotateToPlayer();
        }
    }

    // ----- [이동] -----
    protected virtual void MoveToPlayer()
    {
        if (isKnockbackActive) return;
        // 기본 방향 (플레이어)
        float orbitRadius = 1.25f;
        Vector3 offset = new Vector3(Mathf.Cos(orbitAngle * Mathf.Deg2Rad),0,Mathf.Sin(orbitAngle * Mathf.Deg2Rad)) * orbitRadius;
        Vector3 targetPos = player.position + offset;

        Vector3 diff = targetPos - transform.position;
        Vector3 targetDir = diff.normalized;
        targetDir.y = 0;

        // 적끼리 겹침 방지 및 벽 회피
        Vector3 separationDir = GetSeparationVector();
        Vector3 avoidanceDir = GetWallAvoidanceVector(targetDir);
        Vector3 finalDir = (targetDir + separationDir * 1.5f + avoidanceDir).normalized;
        if (finalDir == Vector3.zero) return;

        Quaternion targetRotation = Quaternion.LookRotation(finalDir);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);

        float angle = Vector3.Angle(transform.forward, finalDir);
        float moveWeight = (angle < 20f) ? 1f : (angle < 60f ? 0.5f : 0.1f);
        float currentSpeed = moveSpeed * speedMultiplier;

        Vector3 targetVelocity = finalDir * (currentSpeed * moveWeight);
        float yVelocity = isClimbing ? climbForce : rb.linearVelocity.y;
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, new Vector3(targetVelocity.x, yVelocity, targetVelocity.z), Time.fixedDeltaTime * 15f);

        if (anim != null) anim.SetFloat("Speed", rb.linearVelocity.magnitude);
    }

    protected virtual void OnCollisionStay(Collision collision)
    {
        // wallLayer에 해당하는 오브젝트에 닿았을 때만
        if (((1 << collision.gameObject.layer) & wallLayer) == 0) return;
        if (currentState == EnemyState.Die || isKnockbackActive) return;

        // 충돌 법선이 수평에 가까우면 (벽면) 기어오름 활성화
        foreach (ContactPoint contact in collision.contacts)
        {
            if (Mathf.Abs(contact.normal.y) < 0.5f)
            {
                isClimbing = true;
                climbTimer = climbStopDelay;
                return;
            }
        }
    }

    protected virtual void RotateToPlayer()
    {
        if (player == null) return;
        Vector3 dir = (player.position - transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
        }
    }

    // 적끼리 서로 밀어내는 힘
    protected Vector3 GetSeparationVector()
    {
        Vector3 separation = Vector3.zero;
        Collider[] neighbors = Physics.OverlapSphere(transform.position, separationDist, enemyLayer);

        foreach (var neighbor in neighbors)
        {
            if (neighbor.gameObject != gameObject)
            {
                Vector3 diff = transform.position - neighbor.transform.position;

                // 핵심 수정: Y축 차이를 완전히 제거하여 수평으로만 밀어내게 함
                diff.y = 0;

                float distance = diff.magnitude;
                if (distance > 0 && distance < separationDist)
                {
                    // 너무 가까울수록 강하게 밀어내되, 최대치를 제한하여 튀는 현상 방지
                    float force = (separationDist - distance) / separationDist;
                    separation += diff.normalized * force;
                }
            }
        }
        return separation;
    }

    // 벽을 감지하고 옆으로 회피 (높은 벽)
    protected Vector3 GetWallAvoidanceVector(Vector3 currentDir)
    {
        RaycastHit hit;
        Vector3 origin = transform.position + Vector3.up * 0.5f;

        if (Physics.Raycast(origin, currentDir, out hit, wallCheckDist, wallLayer))
        {
            // 기어오르는 중이면 회피 불필요
            if (isClimbing) return Vector3.zero;

            // 벽 법선 기반으로 옆으로 회피
            Vector3 hitNormal = hit.normal;
            hitNormal.y = 0;
            return Vector3.Cross(Vector3.up, Vector3.Cross(currentDir, hitNormal));
        }
        return Vector3.zero;
    }


    public void TakeDamage(float amount, bool isCritical, UnityEngine.GameObject attacker = null)
    {
        if (currentState == EnemyState.Die) return;
        currentHp -= amount;
        HandlePopup(amount, isCritical, StatusType.None);
        if (currentHp <= 0) Die();
    }

    public void TakeStatusDamage(StatusDamageInfo info) => statusHandler.AddStatusValue(info);
    
    public void ApplyKnockback(Vector3 direction, float force)
    {
        if (currentState == EnemyState.Die) return;
        StartCoroutine(KnockbackRoutine(direction, force));
    }

    protected IEnumerator KnockbackRoutine(Vector3 dir, float force)
    {
        isKnockbackActive = true;
        rb.linearVelocity = Vector3.zero;
        rb.AddForce(dir * force, ForceMode.Impulse);
        yield return new WaitForSeconds(0.2f); // 넉백 시간 동안 AI 정지
        isKnockbackActive = false;
    }

    public void ProcessStatusDirectDamage(float amount, StatusType type)
    {
        if (currentState == EnemyState.Die || amount <= 0) return;
        currentHp -= amount;
        HandlePopup(amount, false, type);
        if (currentHp <= 0) Die();
    }

    protected void HandlePopup(float amount, bool isCrit, StatusType type)
    {
        if (IsTargetVisible(Camera.main, gameObject) && DamagePopup.CanSpawn(isCrit))
        {
            if (damagePopupPrefab != null)
            {
                GameObject popupObj = ObjectPoolManager.Instance.Get(damagePopupPrefab, transform.position + Vector3.up * 1.5f, Quaternion.identity);
                if (popupObj.TryGetComponent<DamagePopup>(out var popup))
                {
                    // 일반 대미지(None)와 상태 이상 대미지를 구분하여 셋업
                    if (type == StatusType.None) popup.Setup(amount, isCrit);
                    else popup.SetupStatus(amount, type);
                }
            }
        }
    }

    public void SetSpeedMultiplier(float m)
    {
        speedMultiplier = m;
        if (anim != null) anim.speed = m;
    }

    public void SetAiActive(bool active)
    {
        isAiActive = active;

        if (anim != null) {anim.SetBool("IsStunned", !active);}

        if (!active)
        {
            if (equippedWeapon != null) equippedWeapon.gameObject.SetActive(false);
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }

    public void PlayStunAnimation(bool isStunned)
    {
        if (anim == null) return;

        if (isStunned)
        {
            anim.SetBool("IsStunned", true);
        }
    }
    protected virtual void Die()
    {
        if (currentState == EnemyState.Die) return;
        currentState = EnemyState.Die;
        isAiActive = false;

        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.OnEnemyKilled();
        }

        if (equippedWeapon != null) equippedWeapon.gameObject.SetActive(false);

        rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        if (anim != null) { anim.SetTrigger("Die"); }

        StartCoroutine(FreezePhysicsAfterFall());

        if (TryGetComponent<Dropper>(out var dropper)) dropper.Drop(transform.position);
        Invoke("ReturnToPool", 3f);

        if (audioSource != null && deathClip != null)
        {
            audioSource.PlayOneShot(deathClip);
        }

        if (statusHandler != null) statusHandler.ResetGauges();
    }
    protected IEnumerator FreezePhysicsAfterFall()
    {
        yield return new WaitForSeconds(1.0f);

        rb.isKinematic = true;
        if (TryGetComponent<Collider>(out var col)) col.enabled = false;
    }

    protected virtual void ReturnToPool() => ObjectPoolManager.Instance.ReturnToPool(gameObject);
}
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.EventSystems;

// ----- [프로젝타일 구조체] -----
public struct ProjectileInfo
{
    // 기본 스탯
    public float damage;
    public float speed;
    public float size;
    public float maxDistance;
    public float critChance;
    public float critMultiplier;
    public float eliteDamageMult;

    public int pierceCount;
    public int bounceCount;
    public float explosionRadius;
    public float knockbackForce;
    public float fieldDuration;
    public float fieldTickInterval;
    public int chainCount;
    public float chainRange;

    // 특수 무기용 데이터 (옵션)
    public Transform target;
    public float orbitOffset;
    public Vector3? spawnOffset;
    public float meleeRange;
    public float meleeAngle;
    public float laserWidth;
    public float laserTickInterval;
    public Vector3 targetPosition;

    // 상태 이상 데이터 (옵션)
    public float finalStatusDamage;
    public float finalStatusGauge;
    public float dynamicStatusSynergy;
}

public class Projectile : MonoBehaviour
{
    private ProjectileData data;
    private Weapon ownerWeapon;
    private float finalDamage;
    private Vector3 moveDirection;
    private Transform target;
    private GameObject activeIndicator;
    private bool isDead = false;
    private bool isFieldActive = false;
    private bool isAttacked = false;
    private bool hasExploded = false;

    // --- 무기에게 전달받을 동적 스탯 ---
    private float dynamicSpeed;
    private float dynamicMaxDistance;
    private float critChance;
    private float critMultiplier;
    private float dynamicEliteDamage;

    private float dynamicExplosionRadius;
    private float dynamicKnockbackForce;
    private int currentPierce;
    private int currentBounce;
    private float orbitOffsetAngle;
    private float dynamicMeleeRange;
    private float dynamicMeleeAngle;
    private float dynamicLaserWidth;
    private float dynamicLaserTickInterval;
    private Vector3 dynamicTargetPosition;
    private float dynamicFieldDuration;
    private GameObject activeFieldEffect;
    private float dynamicFieldTickInterval;
    private int maxChainCount;
    private int currentChainCount;
    private float dynamicChainRange;
    private float dynamicStatusDamage;
    private float dynamicStatusGauge;
    private float dynamicStatusSynergy;

    // 상태 관리 변수
    private float traveledDistance;
    private float livedTime;
    private Rigidbody rb;
    private TrailRenderer trail;
    private bool isCurrentlyReturning = false;
    private Vector3 laserSpawnOffset;

    private Dictionary<GameObject, float> hitTargets = new Dictionary<GameObject, float>();
    private HashSet<IDamageable> MeleehitTargets = new HashSet<IDamageable>();

    // ----- [INIT : 무기가 계산한 모든 최종 스탯의 매개변수] -----
    public void Init(ProjectileData data, ProjectileInfo info, Weapon owner)
    {
        // 상태 초기화
        this.isDead = false;
        this.isFieldActive = false;
        this.isAttacked = false;
        this.hasExploded = false;
        traveledDistance = 0f;
        livedTime = 0f;
        isCurrentlyReturning = false;
        hitTargets.Clear();
        MeleehitTargets.Clear();
        StopAllCoroutines();
        CancelInvoke();

        // 시각 및 물리 요소 복구 ---
        if (TryGetComponent<Collider>(out var col)) col.enabled = true;
        if (TryGetComponent<Renderer>(out var r)) r.enabled = true;

        // 트레일 초기화 및 바닥 범위 표시
        Transform model = transform.Find("Child");
        if (model != null) model.gameObject.SetActive(true);
        else if (transform.childCount > 0) transform.GetChild(0).gameObject.SetActive(true);

        if (TryGetComponent<TrailRenderer>(out trail))
        {
            if (data.trailMaterial != null) trail.material = data.trailMaterial;
            trail.enabled = true;
            trail.Clear();
        }

        // 데이터 할당
        this.ownerWeapon = owner;
        this.data = data;
        this.finalDamage = info.damage;
        this.moveDirection = transform.forward;
        this.target = info.target;
        this.orbitOffsetAngle = info.orbitOffset;
        this.critChance = info.critChance;
        this.critMultiplier = info.critMultiplier;
        this.dynamicEliteDamage = info.eliteDamageMult;

        this.dynamicSpeed = info.speed;
        this.dynamicMaxDistance = info.maxDistance;
        this.dynamicExplosionRadius = info.explosionRadius;
        this.dynamicKnockbackForce = info.knockbackForce;
        this.currentPierce = info.pierceCount;
        this.currentBounce = info.bounceCount;

        this.dynamicMeleeRange = info.meleeRange;
        this.dynamicMeleeAngle = info.meleeAngle;
        this.dynamicLaserWidth = info.laserWidth;
        this.dynamicLaserTickInterval = info.laserTickInterval;
        this.dynamicTargetPosition = info.targetPosition;
        this.dynamicFieldDuration = info.fieldDuration;
        this.dynamicFieldTickInterval = info.fieldTickInterval;
        this.maxChainCount = info.chainCount;
        this.currentChainCount = 0;
        this.dynamicChainRange = info.chainRange;
        this.dynamicStatusDamage = info.finalStatusDamage;
        this.dynamicStatusGauge = info.finalStatusGauge;
        this.dynamicStatusSynergy = info.dynamicStatusSynergy;

        if (info.spawnOffset.HasValue) this.laserSpawnOffset = info.spawnOffset.Value;

        // 크기 및 RigidBody 설정
        transform.localScale = data.prefab.transform.localScale * info.size;
        if (TryGetComponent<Rigidbody>(out rb))
        {
            bool shouldBeKinematic = data.isMelee || data.isBoomerang || data.isLaser || data.isMeteor || !data.useGravity;
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            rb.useGravity = data.useGravity && !shouldBeKinematic;
            rb.isKinematic = shouldBeKinematic;

            if (!rb.isKinematic && data.useGravity)
            {
                rb.AddForce(Vector3.up * data.launchForce + moveDirection * dynamicSpeed, ForceMode.Impulse);
            }
        }

        // 초기 회전
        if (moveDirection != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(moveDirection);

        // 무기 타입에 따른 로직
        if (data.isMeteor && data.warningIndicatorPrefab != null)
        {
            SpawnWarningIndicator();
        }

        if (data.isOrbit) { MoveOrbit(); }
        else if (data.isMelee) {
            Transform donut_model = transform.childCount > 0 ? transform.GetChild(0) : null;

            if (donut_model != null)
            {
                donut_model.localPosition = Vector3.zero;
                float finalScale = dynamicMeleeRange * 0.75f;
                donut_model.localScale = new Vector3(finalScale, data.meleeHeight, finalScale);
            }
            MeleeAttack(); 
            Invoke("Release", 0.2f); 
        }
        else if (data.isBoomerang) { 
            moveDirection.y = 0; 
            moveDirection.Normalize();
            if (transform.childCount > 0)
            {
                Transform bmodel = transform.GetChild(0);
                bmodel.localRotation = Quaternion.Euler(90f, 0, 0);
            }
        }
        else if (data.isLaser) { UpdateLaser(); }
        else if (data.isMeteor) { }
    }

    void Update()
    {
        if (data == null || isDead) return;
        livedTime += Time.deltaTime;

        if (!isFieldActive && livedTime > data.duration)
        {
            Release();
            return;
        }

        Move();

        if (data.useGravity && rb != null && !rb.isKinematic)
        {
            if (rb.linearVelocity.sqrMagnitude > 0.1f)
            {
                transform.rotation = Quaternion.LookRotation(rb.linearVelocity);
            }
        }

        if (data.isSpinning)
        {
            transform.Rotate(Vector3.up, data.rotationSpeed * Time.deltaTime, Space.Self);
        }

        if (data.isMelee && livedTime < 0.2f)
        {
            Transform swordmodel = transform.childCount > 0 ? transform.GetChild(0) : null;
            if (swordmodel != null)
            {
                float rotationSpeed = dynamicMeleeAngle / 0.2f;
                swordmodel.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
            }
        }
    }

    private IDamageable GetDamageable(Collider col)
    {
        // 자기 자신
        if (col == null) return null;
        IDamageable dmg = col.GetComponent<IDamageable>();
        if (dmg != null) return dmg;

        // 부모/최상단
        dmg = col.GetComponentInParent<IDamageable>();
        if (dmg != null) return dmg;

        // 루트 오브젝트에서 전체 검색
        dmg = col.transform.root.GetComponentInChildren<IDamageable>();
        return dmg;
    }

    // ----- [프로젝타일 이동] -----
    private Vector3 GetTargetCenter(Transform t)
    {
        if (t == null) return transform.position;

        if (t.TryGetComponent<SimpleEnemy>(out var enemy))
        {
            return enemy.GetCenterPosition();
        }

        if (t.TryGetComponent<Collider>(out var col))
        {
            return col.bounds.center;
        }
        return t.position + Vector3.up * 1.0f;
    }

    private void Move()
    {
        // 특수 무기 처리
        if (data.isMelee) return;
        if (data.isLaser) { UpdateLaser(); return; }
        if (data.isBoomerang) { MoveBoomerang(); return; }
        if (data.isOrbit) { MoveOrbit(); return; }
        if (data.isMeteor) { MoveMeteor(); return; }

        // 상태 판단
        bool isWaitingHoming = data.isHoming && livedTime < data.homingDelay;

        // 유도 추격 로직
        if (data.isHoming && !isWaitingHoming)
        {
            if (target == null || !target.gameObject.activeInHierarchy)
            {
                target = GetClosestTarget();
                if (target == null) { ReleaseWithEffect(); return; }
            }

            if (target != null)
            {
                Vector3 targetCenter = GetTargetCenter(target);
                Vector3 targetDir = (targetCenter - transform.position).normalized;
                float distToTarget = Vector3.Distance(transform.position, targetCenter);
                float stickyZone = dynamicMaxDistance * 0.9f;
                // 각도 체크
                float dotProduct = Vector3.Dot(transform.forward, targetDir);

                if (distToTarget > stickyZone && dotProduct < data.homingGiveUpThreshold)
                {
                    ReleaseWithEffect();
                    return;
                }
                float finalTurnSpeed = data.turnSpeed;
                if (distToTarget < stickyZone)
                {
                    float proximityAlpha = 1f - (distToTarget / stickyZone);
                    finalTurnSpeed += proximityAlpha * data.turnSpeed * 3f;
                }

                if (rb != null) rb.useGravity = false;
                moveDirection = Vector3.RotateTowards(transform.forward, targetDir, data.turnSpeed * Time.deltaTime, 0f);
                transform.rotation = Quaternion.LookRotation(moveDirection);

                dynamicSpeed += Time.deltaTime * 1.5f; //
            }
        }

        // 통합 이동 및 충돌 처리
        if (!data.useGravity || (data.isHoming && !isWaitingHoming))
        {
            float moveStep = dynamicSpeed * Time.deltaTime;

            if (isWaitingHoming && rb != null && !rb.isKinematic)
            {
                if (rb.linearVelocity.sqrMagnitude > 0.1f)
                    transform.rotation = Quaternion.LookRotation(rb.linearVelocity);
                CheckHomingCollision(); 
                return;
            }

            // 추격 단계 혹은 일반 투사체 이동
            if (rb != null && !rb.isKinematic)
                rb.linearVelocity = transform.rotation * Vector3.forward * dynamicSpeed;
            else
                transform.position += transform.forward * moveStep;

            traveledDistance += moveStep;
        }

        if (traveledDistance >= dynamicMaxDistance)
        {
            Release();
            return;
        }

        CheckHomingCollision();
    }

    private void MoveOrbit()
    {
        // 기준점 : 플레이어
        if (target == null) return;
        float angle = (livedTime * dynamicSpeed) + orbitOffsetAngle;

        float x = Mathf.Cos(angle) * data.orbitRadius;
        float z = Mathf.Sin(angle) * data.orbitRadius;

        Vector3 orbitPosition = new Vector3(x, data.orbitYOffset, z);
        transform.position = target.position + orbitPosition;

        Vector3 forwardDir = new Vector3(-Mathf.Sin(angle), 0, Mathf.Cos(angle));
        if (forwardDir != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(forwardDir);
        }
    }

    private void MoveBoomerang()
    {
        if(target == null) { Release(); return; }
        float moveStep = dynamicSpeed * Time.deltaTime;

        // 귀환 작동 조건 ---
        if (!isCurrentlyReturning)
        {
            bool timeOver = livedTime > (data.duration * data.returnThreshold);
            bool distanceOver = traveledDistance >= dynamicMaxDistance;

            if (timeOver || distanceOver)
            {
                isCurrentlyReturning = true;
            }
        }

        // --- 상태별 이동 로직 ---
        Vector3 nextDir;
        if (!isCurrentlyReturning)
        {
            // 전진
            nextDir = moveDirection;
            nextDir.y = 0;
            traveledDistance += moveStep;
        }
        else
        {
            // 플레이어 높이 유도
            Vector3 returnTargetPos = target.position + Vector3.up * 1.2f;
            nextDir = (returnTargetPos - transform.position).normalized;

            if (Vector3.Distance(transform.position, returnTargetPos) < 0.8f)
            {
                Release();
                return;
            }
        }

        transform.position += nextDir * moveStep;

        if (data.isSpinning && transform.childCount > 0)
        {
            transform.GetChild(0).Rotate(Vector3.forward * data.rotationSpeed * Time.deltaTime);
        }
    }

    private void MoveMeteor()
    {
        if (isDead) return;

        float moveStep = dynamicSpeed * Time.deltaTime;
        transform.position += Vector3.down * moveStep;

        if (transform.position.y <= dynamicTargetPosition.y + 0.1f)
        {
            transform.position = dynamicTargetPosition;
            ExplodeMeteor();
        }
    }

    // ----- [프로젝타일 범위 폭발] -----
    private void ExplodeMeteor(GameObject skipTarget = null)
    {
        if (hasExploded || isDead) return;
        hasExploded = true;

        // 시각적 효과
        if (data.impactEffectPrefab != null)
            ObjectPoolManager.Instance.Get(data.impactEffectPrefab, transform.position, Quaternion.identity);

        // 실제 대미지 판정
        Collider[] targets = Physics.OverlapSphere(transform.position, dynamicExplosionRadius, ownerWeapon.targetLayer);
        foreach (var col in targets)
        {
            if (skipTarget != null && col.gameObject == skipTarget) continue;

            if (col.TryGetComponent<IDamageable>(out var damageable))
            {
                Vector3 knockDir = (col.transform.position - transform.position).normalized;
                // SendDamageToEnemy 대신 통합 SendDamage 사용
                SendDamage(damageable, knockDir);
            }
        }

        ClearIndicator();

        if (data.isField)
        {
            isFieldActive = true;
            transform.position = GetAdjustedFieldPosition(transform.position);
            StartFieldProcess();
        }
        else
        {
            isDead = true;
            Release();
        }
    }

    private void StartFieldProcess()
    {
        if (rb != null)
        {
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            rb.isKinematic = true;
        }

        if (TryGetComponent<Collider>(out var col)) col.enabled = false;
        if (TryGetComponent<Renderer>(out var r)) r.enabled = false;

        Transform model = transform.Find("Child");                  // 자식을 따로 비활성화할 때의 코드
        if (model != null) model.gameObject.SetActive(false);
        else if (transform.childCount > 0) transform.GetChild(0).gameObject.SetActive(false);
        if (trail != null) trail.enabled = false;

        // 바닥 인디케이터 생성
        if (data.warningIndicatorPrefab != null)
        {
            activeIndicator = ObjectPoolManager.Instance.Get(data.warningIndicatorPrefab, transform.position, Quaternion.identity);
            if (activeIndicator.TryGetComponent<CircleRenderer>(out var circle))
                circle.ShowField(dynamicExplosionRadius, dynamicFieldDuration);
        }

        // 지속 피해 비주얼 이펙트 생성
        if (data.fieldEffectPrefab != null)
        {
            activeFieldEffect = ObjectPoolManager.Instance.Get(data.fieldEffectPrefab, transform.position, Quaternion.identity);
            activeFieldEffect.transform.SetParent(this.transform);
            activeFieldEffect.transform.localScale = Vector3.one;
        }

        // 대미지 코루틴 시작
        StartCoroutine(FieldTickRoutine());
    }

    // 지속 대미지 코루틴
    private IEnumerator FieldTickRoutine()
    {
        float elapsed = 0f;
        PlayImpactSound();

        while (elapsed < dynamicFieldDuration)
        {
            // 범위 내 적 탐색
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, dynamicExplosionRadius, ownerWeapon.targetLayer);

            foreach (var col in hitColliders)
            {
                IDamageable damageable = GetDamageable(col);
                if (damageable != null)
                {
                    SendDamage(damageable, Vector3.zero, data.fieldDamageRatio);
                }
            }

            yield return new WaitForSeconds(dynamicFieldTickInterval);
            elapsed += dynamicFieldTickInterval;
        }
        isDead = true;
        Release();
    }
    
    private void SpawnWarningIndicator()
    {
        if (data.warningIndicatorPrefab == null) return;

        Vector3 groundPos = dynamicTargetPosition;
        if (Physics.Raycast(groundPos + Vector3.up * 15f, Vector3.down, out RaycastHit hit, 30f, LayerMask.GetMask("Floor", "Environment", "Wall", "Spawnable")))
        {
            groundPos = hit.point;
        }

        activeIndicator = ObjectPoolManager.Instance.Get(data.warningIndicatorPrefab, groundPos, Quaternion.identity);

        if (activeIndicator.TryGetComponent<CircleRenderer>(out var circle))
        {
            float fallDuration = Mathf.Max(0.5f, data.meteorSpawnHeight / dynamicSpeed);
            circle.StartGrowth(dynamicExplosionRadius, fallDuration);
        }
    }

    private void ClearIndicator()
    {
        if (activeIndicator != null)
        {
            if (ObjectPoolManager.Instance != null)
                ObjectPoolManager.Instance.ReturnToPool(activeIndicator);
            else
                Destroy(activeIndicator);

            activeIndicator = null;
        }
    }
    
    private Vector3 GetAdjustedFieldPosition(Vector3 hitPoint)
    {
        if (Physics.Raycast(hitPoint + Vector3.up * 0.5f, Vector3.down, out RaycastHit groundHit, 20f, LayerMask.GetMask("Floor", "Environment", "Wall", "Spawnable")))
        {
            return groundHit.point + Vector3.up * 0.01f; 
        }
        return hitPoint;
    }
    
    // ----- [프로젝타일 레이저] -----
    private void UpdateLaser()
    {
        if (ownerWeapon == null) { Release(); return; }

        // 위치 및 방향 동기화
        transform.position = target.TransformPoint(laserSpawnOffset);
        Transform currentTarget = GetClosestTarget();

        if (currentTarget != null)
        {
            Vector3 dirToTarget = (currentTarget.position - transform.position).normalized;
            dirToTarget.y = 0;
            transform.rotation = Quaternion.LookRotation(dirToTarget);
        }
        else
        {
            transform.rotation = ownerWeapon.transform.rotation;
        }

        // 벽 충돌 체크 (레이저 길이 결정)
        float currentMaxDist = dynamicMaxDistance;
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit wallHit, dynamicMaxDistance, LayerMask.GetMask("Wall", "Environment", "Spawnable")))
        {
            currentMaxDist = wallHit.distance;
        }

        // 시각화 (LineRenderer)
        if (TryGetComponent<LineRenderer>(out var line))
        {
            line.useWorldSpace = true;
            line.SetPosition(0, transform.position);
            line.SetPosition(1, transform.position + transform.forward * currentMaxDist);
            line.startWidth = dynamicLaserWidth * transform.localScale.x;
            line.endWidth = dynamicLaserWidth * transform.localScale.x;
        }

        // 직육면체 범위 판정 (BoxCast)
        Vector3 boxCenter = transform.position + transform.forward * (currentMaxDist * 0.5f);
        Vector3 boxHalfExtents = new Vector3(dynamicLaserWidth * 0.5f, data.laserHeight * 0.5f, currentMaxDist * 0.5f);
        RaycastHit[] hits = Physics.BoxCastAll(boxCenter, boxHalfExtents, transform.forward, transform.rotation, 0.01f, ownerWeapon.targetLayer);

        foreach (var hit in hits)
        {
            // 타겟 레이어인 경우에만 레이저 히트 처리
            HandleLaserHit(hit);
        }
    }

    // ----- [프로젝타일 미사일] -----
    // 미사일 전용 고속 충돌 검사
    private void CheckHomingCollision()
    {
        float moveStep = dynamicSpeed * Time.deltaTime;
        float radius = transform.localScale.x * 0.5f;

        if (Physics.SphereCast(transform.position, radius, transform.forward, out RaycastHit hit, moveStep))
        {
            if (hit.collider.CompareTag("Enemy") || hit.collider.CompareTag("Wall") || hit.collider.CompareTag("Environment") || hit.collider.CompareTag("Spawnable"))
            {
                transform.position = hit.point;
                HandleHit(hit.collider, hit.normal);
            }
        }
    }

    // 공중에서 가장 가까운 적 찾기
    private Transform GetClosestTarget()
    {
        LayerMask targetLayer = ownerWeapon.targetLayer;
        float searchRange = Mathf.Max(dynamicMaxDistance, 10f);
        
        int count = Physics.OverlapSphereNonAlloc(transform.position, searchRange, ownerWeapon.GetDetectionBuffer(), targetLayer);

        Transform closest = null;
        float minDistanceSqr = Mathf.Infinity;

        for (int i = 0; i < count; i++)
        {
            Collider col = ownerWeapon.GetDetectionBuffer()[i];
            if (col != null && col.gameObject.activeInHierarchy)
            {
                // IDamageable 인터페이스가 있는지 확인 (죽은 타겟 제외 로직은 인터페이스나 체력 체크로)
                if (col.TryGetComponent<IDamageable>(out var targetHealth))
                {
                    // 만약 상대가 SimpleEnemy라면 기존처럼 체력 0 이하 체크
                    if (targetHealth is SimpleEnemy se && se.currentHp <= 0) continue;

                    float distSqr = (transform.position - col.transform.position).sqrMagnitude;
                    if (distSqr < minDistanceSqr)
                    {
                        minDistanceSqr = distSqr;
                        closest = col.transform;
                    }
                }
            }
        }
        return closest;
    }

    // ----- [프로젝타일 근접 공격] -----
    private void MeleeAttack()
    {
        if (isAttacked) return;
        if (ownerWeapon == null) return;
        isAttacked = true;
        // 반경 내 모든 콜라이더 검출 (트리거 포함)
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, dynamicMeleeRange, ownerWeapon.targetLayer, QueryTriggerInteraction.Collide);

        foreach (var col in hitColliders)
        {
            // 인터페이스 검색
            IDamageable damageable = GetDamageable(col);
            if (damageable == null || MeleehitTargets.Contains(damageable)) continue;

            // 높이(Y축) 체크 
            float yDiff = Mathf.Abs(col.bounds.center.y - transform.position.y);
            if (yDiff > data.meleeHeight)
            {
                continue;
            }

            // Y축을 제외한 평면 각도 계산 :  앞방향 벡터의 Y값을 0으로 만들고 정규화
            Vector3 myForward = transform.forward;
            myForward.y = 0;
            myForward.Normalize();

            // 타겟으로의 방향 벡터의 Y값을 0으로 만들고 정규화
            Vector3 dirToTarget = (col.transform.position - transform.position);
            dirToTarget.y = 0;
            dirToTarget.Normalize();

            // 두 평면 벡터 사이의 각도 계산
            float angleToTarget = Vector3.Angle(myForward, dirToTarget);

            // 각도 판정
            if (angleToTarget <= dynamicMeleeAngle * 0.5f)
            {
                Vector3 finalPushDir = (transform != null) ? transform.forward : Vector3.forward;

                try
                {
                    MeleehitTargets.Add(damageable);
                    SendDamage(damageable, finalPushDir);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[MeleeAttack] {col.name} 타격 처리 중 에러: {e.Message}");
                }
            }
            else{ }
        }
    }

    // ----- [프로젝타일 연쇄 로직] -----
    // 다음 적 탐색
    private Transform FindNextChainTarget(GameObject currentEnemy)
    {
        // ownerWeapon의 타겟 레이어(피아식별)를 사용하여 충돌체 검출
        Collider[] enemies = Physics.OverlapSphere(transform.position, dynamicChainRange, ownerWeapon.targetLayer);
        Transform closest = null;
        float minDistanceSqr = Mathf.Infinity; // sqrMagnitude 최적화

        foreach (var col in enemies)
        {
            // 방금 때린 녀석은 제외
            if (col.gameObject != currentEnemy && col.gameObject.activeInHierarchy)
            {
                // [핵심] 죽은 적(체력 0 이하) 시체로 전이되는 현상 방지
                if (col.TryGetComponent<IDamageable>(out var damageable))
                {
                    if (damageable is SimpleEnemy se && se.currentHp <= 0) continue;

                    float distSqr = (transform.position - col.transform.position).sqrMagnitude;
                    if (distSqr < minDistanceSqr)
                    {
                        minDistanceSqr = distSqr;
                        closest = col.transform;
                    }
                }
            }
        }
        return closest;
    }

    // ----- [프로젝타일 타격 시 로직] -----
    private void OnTriggerEnter(Collider other)
    {
        if (ownerWeapon == null || data.isMelee) return;
        if (!ownerWeapon.isPlayerOwned)
        {
            Debug.Log($"[투사체 스침] 대상: {other.gameObject.name}, 대상레이어: {LayerMask.LayerToName(other.gameObject.layer)}, 무기타겟: {ownerWeapon.targetLayer.value}");
        }
        if (other.CompareTag("Wall") || other.CompareTag("Environment") || other.CompareTag("Floor") || other.CompareTag("Spawnable"))
        {
            HandleHit(other);
            return;
        }

        // 레이어 마스크를 이용해 타겟인지 확인
        if (((1 << other.gameObject.layer) & ownerWeapon.targetLayer) != 0)
        {
            HandleHit(other);
        }
    }

    private void SendDamage(IDamageable targetObj, Vector3 knockbackDir, float damageRatio = 1.0f)
    {
        if (targetObj == null) return;
        float damageToDeal = finalDamage * damageRatio;
        try
        {
            if (targetObj is SimpleEnemy targetEnemy && targetEnemy.isElite)
            {
                damageToDeal *= (1.0f + dynamicEliteDamage);
            }
        }
        catch { /* ignored */ }

        // 크리티컬 계산
        bool isCritical = (UnityEngine.Random.Range(0f, 100f) <= critChance);
        if (isCritical) damageToDeal *= (critMultiplier > 0 ? critMultiplier : 1.5f);

        // IDamageable 인터페이스 호출 (Attacker 포함)
        UnityEngine.GameObject attacker = (ownerWeapon != null) ? ownerWeapon.gameObject : this.gameObject;
        targetObj.TakeDamage(damageToDeal, isCritical, attacker);
        targetObj.ApplyKnockback(knockbackDir, dynamicKnockbackForce);

        // 상태 이상 대미지
        if (data.statusType != StatusType.None)
        {
            StatusDamageInfo sInfo = new StatusDamageInfo
            {
                type = data.statusType,
                damage = dynamicStatusDamage * damageRatio,
                gaugeValue = dynamicStatusGauge,
                synergy = dynamicStatusSynergy
            };
            targetObj.TakeStatusDamage(sInfo);
        }

        if (ownerWeapon != null && ownerWeapon.isPlayerOwned)
        {
            if (PlayerHPManager.Instance != null)
            {
                PlayerHPManager.Instance.TriggerLifesteal(damageToDeal);
            }
        }
    }

    private void ApplyImmediateDamage(Collider hitCollider)
    {
        IDamageable damageable = hitCollider.GetComponent<IDamageable>();
        if (damageable == null)
        {
            damageable = hitCollider.GetComponentInParent<IDamageable>();
        }

        if (damageable != null)
        {
            PlayImpactSound();
            SendDamage(damageable, moveDirection);
        }
    }

    private void HandleHit(Collider other, Vector3? hitNormal = null)
    {
        if (isDead) return;

        // 1. 피아식별: 현재 무기에 설정된 targetLayer에 포함되는지 확인
        bool isTarget = ((1 << other.gameObject.layer) & ownerWeapon.targetLayer) != 0;
        bool isObstacle = other.CompareTag("Wall") || other.CompareTag("Environment");

        // 바닥 체크 (기존 로직 유지)
        if (other.CompareTag("Floor") || other.CompareTag("Spawnable"))
        {
            isDead = true;
            SpawnImpactEffect(transform.position, Quaternion.identity);
            Release();
            return;
        }

        if (!isTarget && !isObstacle) return;

        Quaternion effectRotation = hitNormal.HasValue ? Quaternion.LookRotation(hitNormal.Value) : Quaternion.identity;

        // 1. 공전(Orbit) 및 부메랑 로직
        if (data.isOrbit || data.isBoomerang)
        {
            if (isTarget)
            {
                if (hitTargets.ContainsKey(other.gameObject) && Time.time < hitTargets[other.gameObject])
                    return;

                hitTargets[other.gameObject] = Time.time + data.hitCooldown;
                ApplyImmediateDamage(other);
                SpawnImpactEffect(transform.position, effectRotation);
            }

            return;
        }

        // 2. 적 타격 시 로직 (전이 포함)
        if (isTarget)
        {
            if (hitTargets.ContainsKey(other.gameObject) && Time.time < hitTargets[other.gameObject])
                return;

            hitTargets[other.gameObject] = Time.time + data.hitCooldown;
            ApplyImmediateDamage(other);

            // [전이 로직]
            if (data.isChain && currentChainCount < maxChainCount)
            {
                Transform nextTarget = FindNextChainTarget(other.gameObject);
                if (nextTarget != null)
                {
                    if (data.chainEffectPrefab != null)
                    {
                        GameObject effectObj = ObjectPoolManager.Instance.Get(data.chainEffectPrefab, transform.position, Quaternion.identity);
                        if (effectObj.TryGetComponent<ChainEffect>(out var chainFx))
                        {
                            chainFx.Play(transform.position, nextTarget.position, 0.2f);
                        }
                    }

                    currentChainCount++;
                    this.target = nextTarget;

                    moveDirection = (nextTarget.position - transform.position).normalized;
                    transform.rotation = Quaternion.LookRotation(moveDirection);

                    if (rb != null && !rb.isKinematic)
                        rb.linearVelocity = moveDirection * dynamicSpeed;

                    traveledDistance = 0f;
                    livedTime = 0f;

                    SpawnImpactEffect(transform.position, effectRotation);
                    hitTargets[other.gameObject] = Time.time + 0.1f;

                    return;
                }
            }
        }

        // 3 & 4. Pierce/Bounce 우선 체크 → 소진 시 폭발 처리
        if (data.impactEffectPrefab != null)
            ObjectPoolManager.Instance.Get(data.impactEffectPrefab, transform.position, effectRotation);

        if (isTarget && currentPierce > 0)
        {
            // Pierce 남아있음 → 폭발 무기라도 폭발만 하고 투사체는 계속 비행
            if (data.isExplosive) Explode(other.gameObject);
            currentPierce--;
            traveledDistance = 0f;
            livedTime = 0f;
        }
        else if (currentBounce > 0)
        {
            // Bounce 남아있음 → 폭발 무기라도 폭발만 하고 바운스
            if (data.isExplosive) Explode(isTarget ? other.gameObject : null);
            currentBounce--;
            traveledDistance = 0f;
            livedTime = 0f;
            Bounce(other, hitNormal);
        }
        else
        {
            // Pierce/Bounce 모두 소진 → 폭발 처리 후 소멸
            if (data.isExplosive)
            {
                Explode(isTarget ? other.gameObject : null);
                if (data.isField)
                {
                    transform.position = GetAdjustedFieldPosition(transform.position);
                    StartFieldProcess();
                    return;
                }
            }
            isDead = true;
            Release();
        }
    }

    private void HandleLaserHit(RaycastHit hit)
    {
        Collider hitCollider = hit.collider;
        // 틱 대미지 쿨타임 체크
        if (hitTargets.ContainsKey(hitCollider.gameObject) && Time.time < hitTargets[hitCollider.gameObject])
            return;
        PlayImpactSound();

        // 대미지 간격 기록
        hitTargets[hitCollider.gameObject] = Time.time + dynamicLaserTickInterval;

        IDamageable damageable = GetDamageable(hitCollider);
        if (damageable != null)
        {
            PlayImpactSound();
            hitTargets[hitCollider.gameObject] = Time.time + dynamicLaserTickInterval;
            SendDamage(damageable, transform.forward);
            
            // Get accurate hit point
            Vector3 impactPos = hit.point != Vector3.zero ? hit.point : hitCollider.ClosestPoint(transform.position);
            SpawnImpactEffect(impactPos, Quaternion.identity);
        }
    }

    private void Bounce(Collider other, Vector3? hitNormal = null)
    {
        Vector3 normal;
        if (hitNormal.HasValue)
        {
            normal = hitNormal.Value;
        }
        else
        {
            Vector3 closestPoint = other.ClosestPoint(transform.position);
            normal = (transform.position - closestPoint).normalized;
        }

        moveDirection = Vector3.Reflect(moveDirection, normal);
        moveDirection.y = 0;
        transform.rotation = Quaternion.LookRotation(moveDirection);
        target = null;
    }

    private void Explode(GameObject skipTarget = null)
    {
        SpawnImpactEffect(transform.position, Quaternion.identity);

        Collider[] targets = Physics.OverlapSphere(transform.position, dynamicExplosionRadius, ownerWeapon.targetLayer);
        foreach (var col in targets)
        {
            if (skipTarget != null && col.gameObject == skipTarget) continue;
            IDamageable damageable = GetDamageable(col);
            if (damageable != null)
            {
                Vector3 knockDir = (col.transform.position - transform.position).normalized;
                SendDamage(damageable, knockDir);
            }
        }
    }

    public void Release()
    {
        if (!gameObject.activeInHierarchy) return;
        
        if (activeFieldEffect != null)
        {
            activeFieldEffect.transform.SetParent(null);
            ObjectPoolManager.Instance.ReturnToPool(activeFieldEffect);
            activeFieldEffect = null;
        }
        ClearIndicator();
        StopAllCoroutines();

        if (ownerWeapon != null) ownerWeapon.RemoveFromSpawnedList(this);

        if (ObjectPoolManager.Instance != null)
            ObjectPoolManager.Instance.ReturnToPool(this.gameObject);
        else
            Destroy(gameObject);

        isDead = false;
        isFieldActive = false;
    }

    private void ReleaseWithEffect()
    {
        if (data.despawnEffectPrefab != null)
        {
            ObjectPoolManager.Instance.Get(data.despawnEffectPrefab, transform.position, transform.rotation);
        }
        Release();
    }
    private void OnDisable()
    {
        StopAllCoroutines();
        CancelInvoke();
        hitTargets.Clear();
        ClearIndicator();
    }

    // ----- [이펙트 생성] -----
    private void SpawnImpactEffect(Vector3 position, Quaternion rotation)
    {
        if (data.impactEffectPrefab == null) return;

        GameObject effectObj = ObjectPoolManager.Instance.Get(data.impactEffectPrefab, position, rotation);
        if (effectObj.TryGetComponent<ParticleSystem>(out var ps))
        {
            ps.Clear();
            ps.Play();
        }
    }

    // ----- [피격음 재생] -----
    private void PlayImpactSound()
    {
        if (data.impactSound == null) return;
        AudioSource.PlayClipAtPoint(data.impactSound, transform.position, data.impactSoundVolume);
    }

    // ----- [기즈모 그리기] -----
    private void OnDrawGizmos()
    {
        if (data == null) return;

        // 폭발 범위 표시
        if (data.isExplosive)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
            Gizmos.DrawSphere(transform.position, dynamicExplosionRadius);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, dynamicExplosionRadius);
        }

        // 근접 공격 범위 표시 (부채꼴)
        if (data.isMelee)
        {
            Gizmos.color = Color.yellow;
            Vector3 center = transform.position;
            Vector3 forward = transform.forward;

            float currentRange = Application.isPlaying ? dynamicMeleeRange : data.meleeRange;
            float currentAngle = Application.isPlaying ? dynamicMeleeAngle : data.meleeAngle;

            // 높이(Y축) 범위를 보여주기 위해 상단과 하단 두 층을 그립니다.
            float halfHeight = data.meleeHeight * 0.5f;
            DrawSector(center + Vector3.up * halfHeight, currentRange, currentAngle);
            DrawSector(center - Vector3.up * halfHeight, currentRange, currentAngle);

            // 상단과 하단을 잇는 수직선
            Vector3 leftRay = Quaternion.Euler(0, -currentAngle * 0.5f, 0) * forward * currentRange;
            Vector3 rightRay = Quaternion.Euler(0, currentAngle * 0.5f, 0) * forward * currentRange;

            Gizmos.DrawLine(center + Vector3.up * halfHeight + leftRay, center - Vector3.up * halfHeight + leftRay);
            Gizmos.DrawLine(center + Vector3.up * halfHeight + rightRay, center - Vector3.up * halfHeight + rightRay);
        }

        if (data.isMeteor)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, dynamicTargetPosition);

            // 목표 지점 폭발 범위
            Color meteorColor = new Color(1f, 0.5f, 0f); 
            Gizmos.matrix = Matrix4x4.TRS(dynamicTargetPosition, Quaternion.identity, new Vector3(1f, 0.05f, 1f));

            Gizmos.color = new Color(meteorColor.r, meteorColor.g, meteorColor.b, 0.3f);
            Gizmos.DrawSphere(Vector3.zero, dynamicExplosionRadius);

            // 외곽선 원
            Gizmos.color = meteorColor;
            Gizmos.DrawWireSphere(Vector3.zero, dynamicExplosionRadius);

            Gizmos.matrix = Matrix4x4.identity;

            // 타격 중심점 표시
            Gizmos.color = Color.red;
            float crossSize = 0.5f;
            Vector3 target = dynamicTargetPosition;
            Gizmos.DrawLine(target + Vector3.left * crossSize, target + Vector3.right * crossSize);
            Gizmos.DrawLine(target + Vector3.forward * crossSize, target + Vector3.back * crossSize);
        }

        if (data.isLaser)
        {
            Gizmos.color = Color.cyan;
            Gizmos.matrix = transform.localToWorldMatrix;
            float currentMaxDist = dynamicMaxDistance;
            Gizmos.DrawWireCube(Vector3.forward * (currentMaxDist * 0.5f), new Vector3(data.laserWidth, data.laserHeight, currentMaxDist));
        }
    }

    private void DrawSector(Vector3 center, float range, float angle)
    {
        Vector3 forward = transform.forward;
        Vector3 leftRay = Quaternion.Euler(0, -angle * 0.5f, 0) * forward * range;
        Vector3 rightRay = Quaternion.Euler(0, angle * 0.5f, 0) * forward * range;

        Gizmos.DrawLine(center, center + leftRay);
        Gizmos.DrawLine(center, center + rightRay);

        int segments = 10;
        Vector3 prevPoint = center + leftRay;
        for (int i = 1; i <= segments; i++)
        {
            float currAngle = -angle * 0.5f + (angle / segments) * i;
            Vector3 currPoint = center + (Quaternion.Euler(0, currAngle, 0) * forward * range);
            Gizmos.DrawLine(prevPoint, currPoint);
            prevPoint = currPoint;
        }
    }
}
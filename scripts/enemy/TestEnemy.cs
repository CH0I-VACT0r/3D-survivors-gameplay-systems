using UnityEngine;
using System.Collections;

public class TestEnemy : MonoBehaviour, IEnemy
{
    [Header("Settings")]
    public float hp = 100f;      // 체력
    public float moveSpeed = 3f; // 기본 이동 속도
    public bool isElite = false;
    public bool IsElite => isElite;

    [Header("Components")]
    private EnemyStatusHandler statusHandler;
    private float currentSpeedMultiplier = 1f;
    private bool isAiActive = true;
    

    [Header("Feedback")]
    public GameObject damagePopupPrefab;

    public Color hitColor = Color.yellow;
    public Color CriticalHitColor = Color.red;
    public Color normalColor = Color.green;

    private MeshRenderer meshRenderer;
    private Rigidbody rb;

    void Update()
    {
        if (!isAiActive || hp <= 0) return;
        // transform.position += direction * moveSpeed * currentSpeedMultiplier * Time.deltaTime;
    }

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        rb = GetComponent<Rigidbody>();

        if (meshRenderer != null)
            meshRenderer.material.color = normalColor;
    }
    private bool IsTargetVisible(Camera cam, GameObject obj)
    {
        if (cam == null) return false;
        Vector3 screenPoint = cam.WorldToViewportPoint(obj.transform.position);
        return screenPoint.z > 0 && screenPoint.x > 0 && screenPoint.x < 1 && screenPoint.y > 0 && screenPoint.y < 1;
    }

    // ----- [TakeDamage] -----
    public void TakeDamage(float amount, bool isCritical=false, UnityEngine.GameObject attacker = null)
    {
        if (hp <= 0) return;
        hp -= amount;

        if (IsTargetVisible(Camera.main, gameObject))
        {
            if (DamagePopup.CanSpawn(isCritical))
            {
                if (damagePopupPrefab != null)
                {
                    GameObject popupObj = ObjectPoolManager.Instance.Get(damagePopupPrefab, transform.position + Vector3.up * 1.5f, Quaternion.identity);
                    if (popupObj.TryGetComponent<DamagePopup>(out var popup))
                    {
                        popup.Setup(amount, isCritical);
                    }
                }
            }
        }
        StopAllCoroutines(); // 이전 깜빡임이 있다면 중지
        StartCoroutine(HitFlashRoutine(isCritical));

        // 사망 체크
        if (hp <= 0)
        {
            Die();
        }
    }
    public void TakeStatusDamage(StatusDamageInfo info)
    {
        if (hp <= 0) return;
        statusHandler.AddStatusValue(info);
    }
    public void ProcessStatusDirectDamage(float amount, StatusType type)
    {
        if (hp <= 0 || amount <= 0) return;
        hp -= amount;

        // [핵심] 상태 이상 타입에 맞는 색상 팝업 출력 (다음 단계에서 구현)
        HandlePopup(amount, false, type);

        if (hp <= 0) Die();
    }
    public void ApplyKnockback(Vector3 direction, float force)
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.AddForce(direction * force, ForceMode.Impulse);
        }
    }

    // ----- [이동 속도 배율 조절] -----
    public void SetSpeedMultiplier(float multiplier)
    {
        currentSpeedMultiplier = multiplier;
    }

    // ----- [AI 활성화 제어] ----- 
    public void SetAiActive(bool active)
    {
        isAiActive = active;
        // 필요 시 애니메이터 파라미터 조절 가능
        // GetComponent<Animator>().enabled = active;
    }
    
    // ----- [ 피격 피드백 (색상 및 대미지 팝업)] -----
    private IEnumerator HitFlashRoutine(bool isCritical)
    {
        if (meshRenderer != null)
        {
            meshRenderer.material.color = isCritical ? CriticalHitColor : hitColor;
            yield return new WaitForSeconds(0.1f);
            meshRenderer.material.color = normalColor;
        }
    }
    private void HandlePopup(float amount, bool isCrit, StatusType type)
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

    // 이것도 추후에 오브젝트 풀링 적용하기
    private void Die()
    {
        Destroy(gameObject);
    }
}
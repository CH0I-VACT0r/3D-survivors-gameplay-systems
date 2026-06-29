using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    public static int currentActiveCount = 0;
    private const int MAX_NORMAL_POPUP_COUNT = 50; // 일반 팝업 제한
    private const int MAX_TOTAL_POPUP_COUNT = 80;  // 최대 팝업 제한

    private TextMeshPro textMesh;
    private float disappearTimer;
    private Color textColor;
    private const float DISAPPEAR_TIMER_MAX = 0.5f;

    private static int sortingOrder;
    private Vector3 moveVector;

    void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
    }

    private void OnDisable()
    {
        currentActiveCount--;
    }

    public void Setup(float damageAmount, bool isCritical)
    {
        currentActiveCount++;
        textMesh.SetText(damageAmount.ToString("F0"));

        if (isCritical)
        {
            textMesh.fontSize = 7.5f;
            textMesh.color = Color.red;
            moveVector = new Vector3(Random.Range(-1.5f, 1.5f), 3f, 0) * 1.8f;
        }
        else
        {
            textMesh.fontSize = 4.5f;
            textMesh.color = Color.white;
            moveVector = new Vector3(Random.Range(-1f, 1f), 2f, 0) * 1.5f;
        }

        FinalizeSetup();
    }

    // 상태 이상 대미지 전용 설정
    public void SetupStatus(float damageAmount, StatusType type)
    {
        currentActiveCount++;
        textMesh.SetText(damageAmount.ToString("F0"));
        textMesh.fontSize = 4.0f;

        // 상태 이상별 색상 지정
        textMesh.color = type switch
        {
            StatusType.Impact => new Color(0.5f, 0.5f, 0.5f), // 회색 (충격)
            StatusType.Burn => new Color(1f, 0.5f, 0f),       // 주황 (화상)
            StatusType.Freeze => new Color(0.4f, 0.8f, 1f),   // 하늘색 (빙결)
            StatusType.Electric => new Color(0.7f, 0.4f, 1f), // 보라/매젠타 (감전)
            StatusType.Poison => new Color(0.2f, 0.8f, 0.2f), // 연두/녹색 (중독)
            _ => Color.white
        };

        moveVector = new Vector3(Random.Range(-1.2f, 1.2f), 1.8f, 0) * 1.2f;
        FinalizeSetup();
    }

    public void SetupText(string text, Color color)
    {
        currentActiveCount++;
        textMesh.SetText(text);
        textMesh.fontSize = 5.0f;
        textMesh.color = color;
        moveVector = new Vector3(Random.Range(-1.5f, 1.5f), 2.5f, 0) * 1.5f;

        FinalizeSetup();
    }

    private void FinalizeSetup()
    {
        textColor = textMesh.color;
        textColor.a = 1f;
        textMesh.color = textColor;

        disappearTimer = DISAPPEAR_TIMER_MAX;
        transform.position += new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(-0.2f, 0.2f), 0);

        sortingOrder++;
        textMesh.sortingOrder = sortingOrder;
        transform.localScale = Vector3.one * 1.5f;
    }

    public static bool CanSpawn(bool isCritical)
    {
        // 치명타라면 전체 제한 내에서 최대한 허용
        if (isCritical)
        {
            return currentActiveCount < MAX_TOTAL_POPUP_COUNT;
        }

        // 일반 대미지라면 일반 제한까지만 허용
        return currentActiveCount < MAX_NORMAL_POPUP_COUNT;
    }

    void Update()
    {
        // 벡터 기반 이동
        transform.position += moveVector * Time.deltaTime;
        moveVector -= moveVector * 2f * Time.deltaTime;

        // 빌보드 효과
        if (Camera.main != null)
            transform.forward = Camera.main.transform.forward;

        if (transform.localScale.x > 1f)
        {
            float shrinkSpeed = 5f;
            transform.localScale -= Vector3.one * shrinkSpeed * Time.deltaTime;
        }

        disappearTimer -= Time.deltaTime;
        if (disappearTimer < 0)
        {
            textColor.a -= 3f * Time.deltaTime;
            textMesh.color = textColor;

            if (textColor.a <= 0)
            {
                ObjectPoolManager.Instance.ReturnToPool(gameObject);
            }
        }
    }
}

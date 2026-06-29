using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class CircleRenderer : MonoBehaviour
{
    public int segments = 50;
    private LineRenderer lineRenderer;

    private float targetRadius;
    private float currentRadius = 0f;
    private float growthSpeed;
    private bool isAnimating = false;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.useWorldSpace = false;
    }

    public void StartGrowth(float radius, float duration)
    {
        CancelInvoke("Hide");
        targetRadius = radius;
        float safeDuration = Mathf.Max(0.2f, duration);
        growthSpeed = radius / safeDuration;
        currentRadius = 0.05f;
        isAnimating = true;

        gameObject.SetActive(true);
        if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.enabled = true;
        DrawCircle(currentRadius);
    }

    public void ShowField(float radius, float duration)
    {
        isAnimating = false;
        
        gameObject.SetActive(true);
        if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.enabled = true;
        DrawCircle(radius);

        CancelInvoke("Hide");
        Invoke("Hide", duration);
    }

    void Update()
    {
        if (!isAnimating) return;

        if (currentRadius < targetRadius)
        {
            currentRadius += growthSpeed * Time.deltaTime;
            DrawCircle(currentRadius);
        }
        else
        {
            currentRadius = targetRadius;
            DrawCircle(currentRadius);
            isAnimating = false;
        }
    }

    public void DrawCircle(float radius)
    {
        if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();

        lineRenderer.positionCount = segments + 1;
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;

        for (int i = 0; i <= segments; i++)
        {
            float angle = i * (360f / segments) * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;

            lineRenderer.SetPosition(i, new Vector3(x, 0.02f, z));
        }
    }

    private void Hide()
    {
        if (ObjectPoolManager.Instance != null)
            ObjectPoolManager.Instance.ReturnToPool(this.gameObject);
        else
            gameObject.SetActive(false);
    }
}
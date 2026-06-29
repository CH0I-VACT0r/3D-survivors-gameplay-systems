using UnityEngine;
using System.Collections;

[RequireComponent(typeof(LineRenderer))]
public class ChainEffect : MonoBehaviour
{
    private LineRenderer line;

    void Awake()
    {
        line = GetComponent<LineRenderer>();
    }

    public void Play(Vector3 start, Vector3 end, float duration)
    {
        line.SetPosition(0, start);
        line.SetPosition(1, end);
        line.enabled = true;

        StartCoroutine(FadeRoutine(duration));
    }

    private IEnumerator FadeRoutine(float time)
    {
        float elapsed = 0;
        Color originalColor = line.startColor;

        while (elapsed < time)
        {
            elapsed += Time.deltaTime;

            // 투명해지는 연출
            float alpha = Mathf.Lerp(1, 0, elapsed / time);
            line.startColor = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            line.endColor = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }

        line.enabled = false;
        ObjectPoolManager.Instance.ReturnToPool(this.gameObject);
    }
}

using UnityEngine;
using System.Collections;

public class SwordSlashEffect : MonoBehaviour
{
    public void PlaySlash(Vector3 center, Vector3 forward, float range, float angle, float duration)
    {
        StopAllCoroutines();
        StartCoroutine(SlashRoutine(center, forward, range, angle, duration));
    }

    private IEnumerator SlashRoutine(Vector3 center, Vector3 forward, float range, float angle, float duration)
    {
        float elapsed = 0f;
        float halfAngle = angle * 0.5f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            float currentAngle = Mathf.Lerp(-halfAngle, halfAngle, t);
            Quaternion rotation = Quaternion.Euler(0, currentAngle, 0);

            Vector3 targetPos = center + (rotation * forward * range);

            transform.position = targetPos;
            transform.forward = (targetPos - center).normalized;

            yield return null;
        }

        if (ObjectPoolManager.Instance != null)
            ObjectPoolManager.Instance.ReturnToPool(gameObject);
        else
            Destroy(gameObject);
    }
}
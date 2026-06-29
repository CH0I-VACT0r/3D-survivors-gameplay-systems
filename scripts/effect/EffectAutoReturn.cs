using UnityEngine;

public class EffectAutoReturn : MonoBehaviour
{
    public float duration = 0.1f;

    private void OnEnable()
    {
        Invoke("Return", duration);
    }

    private void Return()
    {
        if (ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.ReturnToPool(this.gameObject);
        }
    }

    private void OnDisable()
    {
        CancelInvoke();
    }
}
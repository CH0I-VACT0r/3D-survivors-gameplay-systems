using UnityEngine;

public class VFXAutoReturn: MonoBehaviour
{
    public float delay = 2f;
    void OnEnable()
    {
        Invoke("Return", delay);
    }

    void Return()
    {
        ObjectPoolManager.Instance.ReturnToPool(gameObject);
    }

    void OnDisable()
    {
        CancelInvoke();
    }
}

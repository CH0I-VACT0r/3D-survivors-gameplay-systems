using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class ParticleAutoReturn : MonoBehaviour
{
    private ParticleSystem ps;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();

        // 유니티 파티클 시스템의 Stop Action 설정
        var main = ps.main;
        main.stopAction = ParticleSystemStopAction.Callback;
    }

    void OnParticleSystemStopped()
    {
        if (ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.ReturnToPool(this.gameObject);
        }
    }
}
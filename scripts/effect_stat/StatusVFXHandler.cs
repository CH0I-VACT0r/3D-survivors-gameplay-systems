using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class StatusVFXHandler : MonoBehaviour
{
    [Header("Particle Prefabs")]
    public ParticleSetting burnFX;
    public ParticleSetting burnExplosionFX;
    public ParticleSetting poisonCloudFX;
    public ParticleSetting freezeFX;
    public ParticleSetting electricFX;

    [System.Serializable]
    public struct ParticleSetting
    {
        public GameObject prefab;
        public Vector3 scale;            // 인스펙터에서 조절할 크기
        public float yOffset;

        [Header("Audio Settings")]
        public AudioClip sfxClip;   // 재생할 소리
        [Range(0f, 1f)]
        public float volume;
    }

    [Header("References")]
    private SimpleEnemy enemyScript;
    private Dictionary<StatusType, GameObject> activeParticles = new Dictionary<StatusType, GameObject>();

    void Awake()
    {
        enemyScript = GetComponent<SimpleEnemy>();
    }

    private Vector3 GetSpawnPosition(float offset)
    {
        // SimpleEnemy에서 만든 CenterPosition을 기본으로 사용
        Vector3 basePos = enemyScript != null ? enemyScript.GetCenterPosition() : transform.position + Vector3.up;
        return basePos + Vector3.up * offset;

    }

    // 지속 파티클 생성/제거
    public void TogglePersistentVFX(StatusType type, bool active)
    {
        if (active)
        {
            if (activeParticles.ContainsKey(type)) return;

            ParticleSetting setting = type switch
            {
                StatusType.Burn => burnFX,
                StatusType.Freeze => freezeFX,
                _ => default
            };

            if (setting.prefab == null)
            {
                return;
            }

            if (setting.prefab != null)
            {
                GameObject obj = ObjectPoolManager.Instance.Get(setting.prefab, GetSpawnPosition(setting.yOffset), Quaternion.identity);
                obj.transform.SetParent(transform);
                obj.transform.localScale = setting.scale;
                activeParticles[type] = obj;
            }
        }
        else
        {
            if (activeParticles.TryGetValue(type, out GameObject obj))
            {
                ObjectPoolManager.Instance.ReturnToPool(obj);
                activeParticles.Remove(type);
            }
        }
    }

    // 단발성 파티클 실행
    public void PlayOneShotVFX(StatusType type)
    {
        ParticleSetting setting = type switch
        {
            StatusType.Burn => burnExplosionFX,
            StatusType.Poison => poisonCloudFX,
            StatusType.Electric => electricFX,
            _ => default
        };

        if (setting.prefab != null)
        {
            float sizeFactor = (transform.localScale.x + transform.localScale.y + transform.localScale.z) / 3f;
            Vector3 randomOffset = new Vector3(Random.Range(-0.3f, 0.3f), 0, Random.Range(-0.3f, 0.3f)) * sizeFactor;
            Vector3 finalPos = GetSpawnPosition(setting.yOffset) + randomOffset;
            GameObject obj = ObjectPoolManager.Instance.Get(setting.prefab, finalPos, Quaternion.identity);
            obj.transform.localScale = Vector3.Scale(setting.scale, transform.localScale);

            if (setting.sfxClip != null && SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySfx(setting.sfxClip, finalPos, setting.volume);
            }
        }
    }

    public void ClearAllVFX()
    {
        foreach (var kvp in activeParticles)
        {
            if (kvp.Value != null) ObjectPoolManager.Instance.ReturnToPool(kvp.Value);
        }
        activeParticles.Clear();
    }
}

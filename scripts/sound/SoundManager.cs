using UnityEngine;
using System.Collections.Generic;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;
    public GameObject audioSourcePrefab;

    void Awake() => Instance = this;

    // 窜惯己 家府 犁积
    public void PlaySfx(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null) return;

        GameObject obj = ObjectPoolManager.Instance.Get(audioSourcePrefab, position, Quaternion.identity);
        AudioSource source = obj.GetComponent<AudioSource>();

        source.clip = clip;
        source.volume = volume;
        source.loop = false;
        source.Play();

        StartCoroutine(ReturnToPoolAfterPlay(obj, clip.length));
    }

    private System.Collections.IEnumerator ReturnToPoolAfterPlay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        ObjectPoolManager.Instance.ReturnToPool(obj);
    }
}
using UnityEngine;

public class SoundPlayer : MonoBehaviour
{
    private AudioSource audioSource;

    void Awake() => audioSource = GetComponent<AudioSource>();

    public void Play(AudioClip clip, float volume, float duration)
    {
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.pitch = Random.Range(0.9f, 1.1f);
        audioSource.Play();

        float playTime = (duration > 0) ? duration : clip.length;
        Invoke("ReturnToPool", playTime);
    }

    void ReturnToPool()
    {
        audioSource.Stop();
        ObjectPoolManager.Instance.ReturnToPool(this.gameObject);
    }
}
using UnityEngine;

public class EnemyDifficultyManager : MonoBehaviour
{
    public static EnemyDifficultyManager Instance;

    [Header("Scaling Settings")]
    public float hpIncreasePerMinute = 0.5f; // 1분당 체력 증가율 (0.5 = 50%)
    public float damageIncreasePerMinute = 0.1f; // 1분당 공격력 증가율 (옵션)

    private float gameStartTime;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        gameStartTime = Time.time;
    }

    // 현재 시간에 따른 체력 배율 계산
    public float GetCurrentHpMultiplier()
    {
        float minutesPassed = (Time.time - gameStartTime) / 60f;
        float timeMultiplier = 1f + (minutesPassed * hpIncreasePerMinute);

        float phaseMultiplier = 1f;
        if (GameManager.Instance != null && GameManager.Instance.currentPhase > 1)
        {
            // 루프마다 체력을 크게 증가 (예: 1루프당 체력 150% 증가)
            phaseMultiplier += (GameManager.Instance.currentPhase - 1) * 1.5f;
        }

        return timeMultiplier * phaseMultiplier;
    }
}
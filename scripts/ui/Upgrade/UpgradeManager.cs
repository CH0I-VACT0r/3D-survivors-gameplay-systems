using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public List<AppliedUpgrade> GetUpgradeChoices()
    {
        List<AppliedUpgrade> candidates = new List<AppliedUpgrade>();
        var weaponMgr = PlayerWeaponManager.Instance;
        float luck = PlayerStatusManager.Instance != null ? PlayerStatusManager.Instance.luck.Value : 0f;

        // 새로운 무기 프리팹 후보
        if (weaponMgr.activeWeapons.Count < weaponMgr.CurrentUnlockedSlots)
        {
            var availableNewPrefabs = weaponMgr.AvailableWeaponPool
                .Where(prefab => !weaponMgr.HasWeapon(prefab))
                .ToList();

            foreach (var prefab in availableNewPrefabs)
            {
                Weapon w = prefab.GetComponent<Weapon>();
                candidates.Add(new AppliedUpgrade
                {
                    isNewWeapon = true,
                    weaponPrefab = prefab, // 프리팹 변수
                    weaponData = w.weaponData,
                    weaponTags = w.weaponData.weaponTags,
                    rarity = UpgradeRarity.Tier_I
                });
            }
        }

        // 기존 무기 강화 후보
        foreach (var activeWeapon in weaponMgr.activeWeapons)
        {
            List<UpgradeOption> tempPool = new List<UpgradeOption>(activeWeapon.weaponData.upgradePool);

            // 무기당 최대 2개 정도의 선택지만 후보로 올림 (선택지 다양성 확보)
            for (int i = 0; i < 2; i++)
            {
                if (tempPool.Count == 0) break;
                UpgradeOption opt = PickOne(tempPool, luck);

                if (opt != null)
                {
                    candidates.Add(new AppliedUpgrade
                    {
                        isNewWeapon = false,
                        targetWeapon = activeWeapon,
                        weaponData = activeWeapon.weaponData,
                        statType = opt.statType,
                        rarity = opt.rarity,
                        value = UnityEngine.Random.Range(opt.minValue, opt.maxValue)
                    });
                    tempPool.Remove(opt);
                }
            }
        }
        // 최종 셔플 및 3개 선택
        System.Random rng = new System.Random();
        return candidates.OrderBy(x => rng.Next()).Take(3).ToList();
    }
    // --- 행운 보정 가중치 계산 전용 함수 ---
    private UpgradeOption PickOne(List<UpgradeOption> pool, float luck)
    {
        // 모든 옵션의 '보정된 가중치' 총합 계산
        float totalWeight = 0f;
        foreach (var opt in pool)
        {
            totalWeight += GetModifiedWeight(opt, luck);
        }

        if (totalWeight <= 0) return null;

        float roll = UnityEngine.Random.Range(0f, totalWeight);
        float cumulative = 0f;

        // 누적합을 보정된 가중치 기준으로 계산
        foreach (var option in pool)
        {
            cumulative += GetModifiedWeight(option, luck);
            if (roll < cumulative)
            {
                return option;
            }
        }
        return pool[0];
    }

    // 티어별 행운 보정 공식
    private float GetModifiedWeight(UpgradeOption opt, float luck)
    {
        float baseW = opt.weight;

        switch (opt.rarity)
        {
            // 행운이 높을수록 저티어 가중치 감소
            case UpgradeRarity.Tier_I:
                return baseW / (1f + luck * 0.1f);
            case UpgradeRarity.Tier_II:
                return baseW / (1f + luck * 0.05f);

            // 행운이 높을수록 고티어 가중치는 증가
            case UpgradeRarity.Tier_IV:
                return baseW * (1f + luck * 0.1f);
            case UpgradeRarity.Tier_V:
                return baseW * (1f + luck * 0.2f);

            default:
                return baseW;
        }
    }
}
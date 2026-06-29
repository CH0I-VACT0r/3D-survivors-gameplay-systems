using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PlayerWeaponManager : MonoBehaviour
{
    public static PlayerWeaponManager Instance { get; private set; }

    [Header("Inventory Settings")]
    public int baseWeaponSlots = 1;      // 처음 시작할 때 열려있는 슬롯
    public int maxTotalSlots = 4;        // 업그레이드로 늘릴 수 있는 최대치
    private int currentUnlockedSlots;

    [Header("Catalog")]
    public WeaponPoolData weaponPoolData; // (Master Weapon Catalog - All weapons in game)
    
    // 런타임 중에 실제 드랍/가챠용으로 사용될 필터링된 무기 리스트
    private List<GameObject> availableWeaponPool = new List<GameObject>();

    // 실시간 관리 데이터
    private Dictionary<ProjectileData, Weapon> equippedWeapons = new Dictionary<ProjectileData, Weapon>();
    public List<Weapon> activeWeapons = new List<Weapon>();
    public int CurrentUnlockedSlots => currentUnlockedSlots;
    public List<GameObject> AvailableWeaponPool => availableWeaponPool;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        currentUnlockedSlots = baseWeaponSlots; 
    }
    // --- [시스템 초기화 로직] ---
    public void Initialize(CharacterData charData)
    {
        ResetInventory();
        
        // 0. 가용 가능한 무기 풀(Available Pool) 구성 (잠금 해제 상태 반영)
        PopulateAvailableWeaponPool();

        // 슬롯 초기화 (캐릭터마다 기본 슬롯 수가 다를 경우를 대비)
        int bonus = 0;
        if (PlayerStatusManager.Instance != null){bonus = PlayerStatusManager.Instance.permanentWeaponSlotBonus;}
        currentUnlockedSlots = Mathf.Min(baseWeaponSlots + bonus, maxTotalSlots);

        GameObject finalStartingWeapon = charData.startingWeaponPrefab;
        if (charData.abilityTree != null)
        {
            foreach (var node in charData.abilityTree.nodes)
            {
                if (node.replaceDefaultWeaponPrefab != null)
                {
                    int savedLevel = SaveManager.Instance.GetNodeLevel($"{charData.characterName}_{node.nodeId}");

                    if (savedLevel > 0)
                    {
                        finalStartingWeapon = node.replaceDefaultWeaponPrefab;
                    }
                }
            }
        }

        // 런 데이터(이전 스테이지 정보)가 있다면 무기 복구
        if (GameManager.Instance != null && GameManager.Instance.hasSavedRunData)
        {
            var saved = GameManager.Instance.savedWeapons;
            if (currentUnlockedSlots < saved.Count)
            {
                currentUnlockedSlots = saved.Count;
            }

            foreach (var wData in saved)
            {
                // 1. [수정] 등록된 마스터 무기 풀에서 검색 (도감 전체를 뒤져서 복구)
                GameObject prefab = weaponPoolData.allWeaponPrefabs.Find(p => 
                {
                    Weapon w = p.GetComponent<Weapon>();
                    return w != null && w.weaponData != null && w.weaponData.weaponName == wData.weaponName;
                });

                // 2. 만약 풀에 없더라도 시작 무기(또는 어빌리티 무기)라면 복구 대상에 포함
                if (prefab == null && finalStartingWeapon != null)
                {
                    Weapon startW = finalStartingWeapon.GetComponent<Weapon>();
                    if (startW != null && startW.weaponData != null && startW.weaponData.weaponName == wData.weaponName)
                    {
                        prefab = finalStartingWeapon;
                    }
                }

                if (prefab != null)
                {
                    EquipWeaponFromPrefab(prefab);
                    Weapon newest = activeWeapons.LastOrDefault();
                    if (newest != null)
                    {
                        if (wData.upgradeHistory != null)
                        {
                            newest.upgradeHistory = new List<AppliedUpgrade>(wData.upgradeHistory);
                        }
                        newest.SetLevel(wData.level);
                    }
                }
                else
                {
                    Debug.LogWarning($"[Weapon Manager] 런 데이터 무기 복구 실패: {wData.weaponName} 프리팹을 찾지 못했습니다.");
                }
            }
        }
        else
        {
            if (finalStartingWeapon != null)
            {
                EquipWeaponFromPrefab(finalStartingWeapon);
            }
        }
    }

    private void PopulateAvailableWeaponPool()
    {
        availableWeaponPool.Clear();
        if (weaponPoolData == null) return;

        foreach (var prefab in weaponPoolData.allWeaponPrefabs)
        {
            Weapon w = prefab.GetComponent<Weapon>();
            if (w == null || w.weaponData == null) continue;

            string itemId = w.weaponData.weaponName;

            // UnlockManager를 통해 해금 여부 확인
            if (UnlockManager.Instance != null)
            {
                // 잠금 시스템에 등록되지 않은 무기는 기본적으로 '해금' 된 것으로 간주하거나, 
                // 특정 규칙에 따라 필터링 가능합니다.
                if (UnlockManager.Instance.IsUnlocked(itemId))
                {
                    availableWeaponPool.Add(prefab);
                }
                else
                {
                    // 등록되지 않은 무기(기본 무기 등)는 풀에 포함
                    var allItems = UnlockManager.Instance.GetAllItems();
                    bool isTracked = System.Array.Exists(allItems, x => x.itemId == itemId);
                    if (!isTracked)
                    {
                        availableWeaponPool.Add(prefab);
                    }
                }
            }
            else
            {
                // UnlockManager가 없으면 모든 무기 사용 가능
                availableWeaponPool.Add(prefab);
            }
        }

        Debug.Log($"<color=green>[WeaponPool]</color> Available weapons populated: {availableWeaponPool.Count} / {weaponPoolData.allWeaponPrefabs.Count}");
    }

    // --- [무기 장착 및 슬롯 로직] ---
    public bool HasWeapon(GameObject prefab)
    {
        Weapon w = prefab.GetComponent<Weapon>();
        if (w == null || w.weaponData == null) return false;
        return equippedWeapons.ContainsKey(w.weaponData);
    }
    public void EquipWeapon(GameObject prefab)
    {
        if (prefab == null) return;

        Weapon prefabWeapon = prefab.GetComponent<Weapon>();
        if (prefabWeapon == null || prefabWeapon.weaponData == null)
        {
            Debug.LogError("프리팹에 Weapon 컴포넌트가 없습니다.");
            return;
        }
        ProjectileData data = prefabWeapon.weaponData;

        if (HasWeapon(prefab)) return;
        if (activeWeapons.Count >= currentUnlockedSlots) return;

        // 실제 생성
        GameObject weaponObj = Instantiate(prefab, this.transform);
        weaponObj.name = data.weaponName;
        weaponObj.transform.localPosition = Vector3.zero;
        weaponObj.transform.localRotation = Quaternion.identity;
        Weapon weaponScript = weaponObj.GetComponent<Weapon>();

        activeWeapons.Add(weaponScript);
        equippedWeapons.Add(weaponScript.weaponData, weaponScript);

        weaponScript.SetLevel(1);

        // 풀링 최적화 (프리팹의 데이터 안의 투사체 프리팹)
        if (ObjectPoolManager.Instance != null && weaponScript.weaponData.prefab != null)
        {
            ObjectPoolManager.Instance.Preload(weaponScript.weaponData.prefab, 30);
        }

        SyncSynergies();
    }
    public void UnlockNextSlot()
    {
        if (currentUnlockedSlots < maxTotalSlots)
        {
            currentUnlockedSlots++;
            Debug.Log($"<color=cyan>[Inventory]</color> 무기 슬롯 해금! 현재 슬롯: {currentUnlockedSlots}");
        }
    }

    public void EquipWeaponFromPrefab(GameObject prefab)
    {
        if (prefab == null) return;

        // 1. 프리팹에 붙어있는 Weapon 스크립트와 거기 할당된 데이터를 미리 확인
        Weapon weaponComp = prefab.GetComponent<Weapon>();
        if (weaponComp == null || weaponComp.weaponData == null)
        {
            Debug.LogError($"{prefab.name}에 Weapon 컴포넌트나 weaponData가 없습니다!");
            return;
        }

        ProjectileData data = weaponComp.weaponData;

        // 2. 이미 가지고 있는지 데이터(SO) 기준으로 체크
        if (HasWeapon(prefab)) return;

        // 3. 슬롯 체크
        if (activeWeapons.Count >= currentUnlockedSlots) return;

        // 4. 무기 생성 및 설정
        GameObject weaponObj = Instantiate(prefab, this.transform); // 전달받은 프리팹 생성
        weaponObj.name = data.weaponName;

        Weapon newWeapon = weaponObj.GetComponent<Weapon>();

        // 5. 관리 리스트 등록 (데이터와 스크립트 연결)
        activeWeapons.Add(newWeapon);
        equippedWeapons.Add(data, newWeapon);

        newWeapon.SetLevel(1);
        SyncSynergies();
    }

    // 현재 장착된 무기 태그 다시 계산
    public void SyncSynergies()
    {
        if (PlayerStatusManager.Instance == null) return;

        List<ProjectileData> currentDatas = activeWeapons.Select(w => w.weaponData).ToList();
        PlayerStatusManager.Instance.RefreshSynergies(currentDatas);
    }

    // --- [초기화 및 정리] ---
    [ContextMenu("Remove All Weapons")]
    public void ResetInventory()
    {
        foreach (var weapon in activeWeapons)
        {
            if (weapon != null)
            {
                weapon.ClearAllProjectiles();
                Destroy(weapon.gameObject);
            }
        }

        activeWeapons.Clear();
        equippedWeapons.Clear();

        // 시너지 초기화
        PlayerStatusManager.Instance?.RefreshSynergies(new List<ProjectileData>());
    }
}
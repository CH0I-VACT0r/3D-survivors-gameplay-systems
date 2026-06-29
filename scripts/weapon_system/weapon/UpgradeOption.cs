using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public enum UpgradeRarity
{
    Tier_I,
    Tier_II,
    Tier_III,
    Tier_IV,
    Tier_V
}

[System.Serializable]
public class UpgradeOption
{
    public StatType statType;      // 어떤 스탯을 올리는가
    public UpgradeRarity rarity;   // 등급
    public float minValue;         // 최소 수치 (예: 5.0)
    public float maxValue;         // 최대 수치 (예: 8.0)
    public int weight;             // 선택될 확률 가중치
}

// 실제로 무기에 적용되어 저장될 데이터 (결과값)
[System.Serializable]
public struct AppliedUpgrade
{
    public bool isNewWeapon;        // 새 무기 획득 여부
    public GameObject weaponPrefab; // 새 무기일 경우의 데이터
    public ProjectileData weaponData;
    public List<WeaponTag> weaponTags;
    public Weapon targetWeapon;     // 강화 대상 무기 (기존 무기 강화일 때)
    public StatType statType;
    public float value;
    public UpgradeRarity rarity;
}

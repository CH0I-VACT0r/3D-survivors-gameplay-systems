using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewSynergy", menuName = "ScriptableObjects/Synergy")]
public class SynergyData : ScriptableObject
{
    public WeaponTag targetTag;

    [System.Serializable]
    public struct SynergyLevel
    {
        public int requiredCount;        // 필요한 태그 개수
        public List<TagModifier> bonuses; // 도달 시 주는 보너스
        public GameObject ultimateWeaponPrefab; // 단계에 도달했을 때 자동으로 소환될 무기 프리팹
    }

    public List<SynergyLevel> levels; // 단계별 설정 (1단계: 3개, 2단계: 4개 등)
}
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AbilityNode
{
    public string nodeId;           // 고유 ID (예: "Tier1_Center")
    public int tier = 1;
    public string nodeNameKey;      // 이름 (로컬라이제이션 키)
    [TextArea] public string description;
    public Sprite icon;

    [Header("Stats")]
    public StatType targetStat;     // 강화할 스탯
    public float valuePerLevel;     // 레벨당 증가 수치
    public int maxLevel = 3;        // 최대 레벨
    public WeaponTag grantTag = WeaponTag.None;

    [Header("Special Logic")]
    public NodeSpecialEffectSO specialEffectSO;

    [Header("Visuals")]
    [Tooltip("시작 노드")]
    public bool isStartNode = false;
    [Tooltip("왕 노드")]
    public bool isMajorNode = false;
    [Tooltip("공격=빨강, 생존=초록, 유틸=파랑 계열 색상 지정")]
    public Color themeColor = Color.white;

    [Header("Special Grants (New)")]
    [Tooltip("이 태그를 장착한 것으로 간주하여 시너지 카운트 증가")]
    public WeaponTag startWithTag = WeaponTag.None;

    [Tooltip("캐릭터의 기본 무기를 이 프리팹으로 완전히 교체")]
    public GameObject replaceDefaultWeaponPrefab;

    [Header("Layout")]
    public Vector2 position;        // 트리 내 좌표 (중앙 0,0 기준)

    [Header("Flow Control")]
    public List<string> prerequisiteNodes;    // 선행 노드 ID (이게 있어야 해금됨)
    public List<string> exclusiveNodes;   // [1택 빌드용] 이 ID의 노드와는 동시에 찍을 수 없음
}
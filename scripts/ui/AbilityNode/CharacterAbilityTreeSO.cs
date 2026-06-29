using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewAbilityTree", menuName = "SO/AbilityTree")]
public class CharacterAbilityTreeSO : ScriptableObject
{
    public List<AbilityNode> nodes = new List<AbilityNode>();
}
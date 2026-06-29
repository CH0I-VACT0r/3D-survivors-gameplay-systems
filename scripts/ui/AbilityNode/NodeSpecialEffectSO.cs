using UnityEngine;

public abstract class NodeSpecialEffectSO : ScriptableObject
{
    public abstract void ApplyEffect(PlayerStatusManager player, int nodeLevel);
}

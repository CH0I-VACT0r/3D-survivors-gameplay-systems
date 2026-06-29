using UnityEngine;

[CreateAssetMenu(fileName = "HpToShieldEffect", menuName = "Ability Effects/Hp To Shield (CI)")]
public class HpToShieldEffectSO : NodeSpecialEffectSO
{
    [Header("Settings")]
    public float shieldMultiplier = 3.0f;

    public override void ApplyEffect(PlayerStatusManager player, int nodeLevel)
    {
        player.maxHealth.isLocked = true;
        player.maxHealth.lockedValue = 1f;

        player.hpRegen.isLocked = true;
        player.hpRegen.lockedValue = 0f;

        player.lifesteal.isLocked = true;
        player.lifesteal.lockedValue = 0f;

        player.shield.AddPercentModifier(shieldMultiplier);
    }
}
public interface IEnemy
{
    bool IsElite { get; }
    void TakeDamage(float amount, bool isCritical = false, UnityEngine.GameObject attacker = null);
    void TakeStatusDamage(StatusDamageInfo info);
    void ApplyKnockback(UnityEngine.Vector3 direction, float force);
}
using UnityEngine;
public interface IDamageable
{
    void TakeDamage(float amount, bool isCritical, UnityEngine.GameObject attacker = null);
    void ApplyKnockback(Vector3 direction, float force);
    void TakeStatusDamage(StatusDamageInfo info);
    Vector3 GetCenterPosition(); // 투사체가 조준할 위치
}

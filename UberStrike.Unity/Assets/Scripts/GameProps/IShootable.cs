
using UnityEngine;

public interface IShootable
{
    void ApplyDamage(DamageInfo shot);
    void ApplyForce(Vector3 position, Vector3 force);

    bool IsVulnerable { get; }
    bool IsLocal { get; }
}
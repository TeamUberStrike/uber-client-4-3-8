
using UnityEngine;
using System;

public interface IGrenadeProjectile : IProjectile
{
    Vector3 Position { get; }
    Vector3 Velocity { get; }

    IGrenadeProjectile Throw(Vector3 position, Vector3 velocity);

    event Action<IGrenadeProjectile> OnProjectileEmitted;
    event Action<IGrenadeProjectile> OnProjectileExploded;

    void SetLayer(UberstrikeLayer layer);
}

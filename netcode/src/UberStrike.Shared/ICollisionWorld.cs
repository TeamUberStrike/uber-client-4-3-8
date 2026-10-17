using System.Numerics;

namespace UberStrike.Shared;

/// <summary>
/// Abstraction over the static collision geometry. The SAME implementation (or a
/// faithfully equivalent one) must be used by client prediction and server simulation,
/// otherwise movement diverges. In production, back this with your baked Unity collision
/// mesh exported to a server-readable format.
/// </summary>
public interface ICollisionWorld
{
    /// <summary>Move <paramref name="from"/> by <paramref name="delta"/>, resolving against geometry.</summary>
    Vector3 CollideAndSlide(Vector3 from, Vector3 delta);

    bool CheckGrounded(Vector3 position);

    /// <summary>Can a ducked player stand up here? (original CharacterMoveController.HasCollision check)</summary>
    bool HasHeadroom(Vector3 position);

    /// <summary>Unobstructed sightline test between two points (used for server hit LOS).</summary>
    bool LineOfSight(Vector3 a, Vector3 b);

    /// <summary>Nearest static-geometry ray hit (projectile travel). Returns hit distance.</summary>
    bool Raycast(Vector3 origin, Vector3 dir, float maxDist, out float t);

    bool Contains(Vector3 p);
    Vector3 ClampToBounds(Vector3 p);
}

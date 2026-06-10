using System.Numerics;

namespace UberStrike.Shared;

/// <summary>
/// Minimal deterministic stand-in: a ground plane + an axis-aligned world box.
/// Good enough to exercise prediction/reconciliation and the anti-cheat invariants.
/// Replace with real geometry for production (see IMPLEMENTATION_PLAN.md, Phase 4).
/// </summary>
public sealed class FlatCollisionWorld : ICollisionWorld
{
    public float   GroundY = 0f;
    public Vector3 Min = new(-100f, -10f, -100f);
    public Vector3 Max = new( 100f,  50f,  100f);

    public Vector3 CollideAndSlide(Vector3 from, Vector3 delta)
    {
        Vector3 to = from + delta;
        if (to.Y < GroundY) to.Y = GroundY; // floor
        return Vector3.Clamp(to, Min, Max);   // walls (box)
    }

    public bool CheckGrounded(Vector3 p) => p.Y <= GroundY + 0.01f;

    // No ceilings in the flat world; a ducked player can always stand.
    public bool HasHeadroom(Vector3 p) => true;

    // Flat world has no occluders other than the ground plane; sightlines above ground are clear.
    public bool LineOfSight(Vector3 a, Vector3 b) => true;

    // Ray vs the ground plane only (enough for the in-process demo/tests).
    public bool Raycast(Vector3 origin, Vector3 dir, float maxDist, out float t)
    {
        t = 0f;
        if (dir.Y >= -1e-6f) return false;          // not heading down toward the plane
        float dist = (GroundY - origin.Y) / dir.Y;  // > 0 when origin is above the plane
        if (dist < 0f || dist > maxDist) return false;
        t = dist;
        return true;
    }

    public bool Contains(Vector3 p) =>
        p.X >= Min.X && p.X <= Max.X &&
        p.Y >= Min.Y && p.Y <= Max.Y &&
        p.Z >= Min.Z && p.Z <= Max.Z;

    public Vector3 ClampToBounds(Vector3 p) => Vector3.Clamp(p, Min, Max);
}

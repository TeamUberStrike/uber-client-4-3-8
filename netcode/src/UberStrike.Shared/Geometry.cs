using System.Numerics;

namespace UberStrike.Shared;

/// <summary>Pure deterministic ray/volume intersection helpers used by server hit detection.</summary>
public static class Geometry
{
    /// <summary>Ray vs sphere. Returns nearest forward hit distance in <paramref name="t"/>.</summary>
    public static bool RaySphere(Vector3 origin, Vector3 dir, Vector3 center, float radius, float maxDist, out float t)
    {
        t = 0f;
        Vector3 oc = origin - center;
        float b = Vector3.Dot(oc, dir);
        float c = Vector3.Dot(oc, oc) - radius * radius;
        float disc = b * b - c;
        if (disc < 0f) return false;
        float sqrt = MathF.Sqrt(disc);
        float t0 = -b - sqrt;
        float t1 = -b + sqrt;
        float hit = t0 >= 0f ? t0 : t1;
        if (hit < 0f || hit > maxDist) return false;
        t = hit;
        return true;
    }

    /// <summary>
    /// Ray vs a vertical capsule (segment a->b with radius). Approximated as the closest
    /// approach between the ray and the segment; a hit occurs when that distance is within
    /// the radius and forward of the origin. Returns approximate hit distance in <paramref name="t"/>.
    /// </summary>
    public static bool RayCapsule(Vector3 origin, Vector3 dir, Vector3 a, Vector3 b, float radius, float maxDist, out float t)
    {
        t = 0f;
        Vector3 seg = b - a;
        float segLen2 = Vector3.Dot(seg, seg);

        // Solve closest points between ray (origin + s*dir, s>=0) and segment (a + u*seg, u in [0,1]).
        float dDotSeg = Vector3.Dot(dir, seg);
        Vector3 r = origin - a;
        float dDotR = Vector3.Dot(dir, r);
        float segDotR = Vector3.Dot(seg, r);

        float denom = segLen2 - dDotSeg * dDotSeg; // >= 0
        float s, u;
        if (denom > 1e-6f)
        {
            s = (dDotSeg * segDotR - segLen2 * dDotR) / denom;
            if (s < 0f) s = 0f;
            u = (dDotSeg * s + segDotR) / segLen2;
        }
        else
        {
            s = -dDotR; // parallel-ish
            u = 0f;
        }
        u = Math.Clamp(u, 0f, 1f);
        // refine s for the clamped u
        s = Vector3.Dot(a + seg * u - origin, dir);
        if (s < 0f) s = 0f;
        if (s > maxDist) return false;

        Vector3 pRay = origin + dir * s;
        Vector3 pSeg = a + seg * u;
        float dist2 = Vector3.DistanceSquared(pRay, pSeg);
        if (dist2 > radius * radius) return false;

        t = s;
        return true;
    }

    // ---------------------------------------------------------------------------------------
    // Triangle primitives for the baked collision world (Phase 4). These run in the movement
    // path (CollideAndSlide / CheckGrounded), so they use determinism-safe ops only — explicit
    // Dot3/Cross3 from SharedMovement, fixed association order, no Vector3.Dot/Normalize.
    // ---------------------------------------------------------------------------------------

    /// <summary>
    /// Möller–Trumbore ray vs triangle (single-sided OFF — hits either face). Returns the
    /// forward hit distance in <paramref name="t"/> and the geometric face normal.
    /// </summary>
    public static bool RayTriangle(Vector3 origin, Vector3 dir, Vector3 v0, Vector3 v1, Vector3 v2,
                                   float maxDist, out float t, out Vector3 normal)
    {
        t = 0f; normal = default;
        Vector3 e1 = v1 - v0;
        Vector3 e2 = v2 - v0;
        Vector3 p = SharedMovement.Cross3(dir, e2);
        float det = SharedMovement.Dot3(e1, p);
        if (det > -1e-8f && det < 1e-8f) return false;     // ray parallel to triangle
        float invDet = 1f / det;

        Vector3 tv = origin - v0;
        float u = SharedMovement.Dot3(tv, p) * invDet;
        if (u < 0f || u > 1f) return false;

        Vector3 q = SharedMovement.Cross3(tv, e1);
        float v = SharedMovement.Dot3(dir, q) * invDet;
        if (v < 0f || u + v > 1f) return false;

        float hit = SharedMovement.Dot3(e2, q) * invDet;
        if (hit < 0f || hit > maxDist) return false;

        t = hit;
        normal = SharedMovement.Normalize3(SharedMovement.Cross3(e1, e2));
        return true;
    }

    /// <summary>
    /// Closest point on triangle (v0,v1,v2) to point <paramref name="p"/> (Ericson, Real-Time
    /// Collision Detection — the Voronoi-region method). Deterministic; used for sphere
    /// depenetration in CollideAndSlide.
    /// </summary>
    public static Vector3 ClosestPointOnTriangle(Vector3 p, Vector3 v0, Vector3 v1, Vector3 v2)
    {
        Vector3 ab = v1 - v0;
        Vector3 ac = v2 - v0;
        Vector3 ap = p - v0;
        float d1 = SharedMovement.Dot3(ab, ap);
        float d2 = SharedMovement.Dot3(ac, ap);
        if (d1 <= 0f && d2 <= 0f) return v0;                       // vertex region v0

        Vector3 bp = p - v1;
        float d3 = SharedMovement.Dot3(ab, bp);
        float d4 = SharedMovement.Dot3(ac, bp);
        if (d3 >= 0f && d4 <= d3) return v1;                       // vertex region v1

        float vc = d1 * d4 - d3 * d2;
        if (vc <= 0f && d1 >= 0f && d3 <= 0f)
        {
            float w = d1 / (d1 - d3);
            return v0 + ab * w;                                    // edge ab
        }

        Vector3 cp = p - v2;
        float d5 = SharedMovement.Dot3(ab, cp);
        float d6 = SharedMovement.Dot3(ac, cp);
        if (d6 >= 0f && d5 <= d6) return v2;                       // vertex region v2

        float vb = d5 * d2 - d1 * d6;
        if (vb <= 0f && d2 >= 0f && d6 <= 0f)
        {
            float w = d2 / (d2 - d6);
            return v0 + ac * w;                                    // edge ac
        }

        float va = d3 * d6 - d5 * d4;
        if (va <= 0f && (d4 - d3) >= 0f && (d5 - d6) >= 0f)
        {
            float w = (d4 - d3) / ((d4 - d3) + (d5 - d6));
            return v1 + (v2 - v1) * w;                             // edge bc
        }

        // interior (barycentric)
        float denom = 1f / (va + vb + vc);
        float vbw = vb * denom;
        float vcw = vc * denom;
        return v0 + ab * vbw + ac * vcw;
    }
}

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
}

using System.Numerics;

namespace UberStrike.Shared;

/// <summary>
/// Phase 4: the real collision world. A baked map mesh drives movement collision and the LOS
/// raycast that finally makes wallbang detection real. The SAME mesh + code run on the client
/// (prediction) and the server (authority), so movement stays in sync and the server's hit-LOS
/// agrees with what the shooter saw.
///
/// The player is approximated for movement as a stack of 3 spheres along the body capsule
/// (feet / mid / head), depenetrated against triangles and slid — robust, allocation-light, and
/// deterministic (resolves the deepest penetration, tie-broken by triangle index).
/// </summary>
public sealed class BakedCollisionWorld : ICollisionWorld
{
    public readonly TriangleMesh Mesh;
    private readonly Vector3 _min, _max;

    // movement proxy
    private const float Radius      = GameConstants.BodyRadius;
    private const float GroundProbe = 0.20f; // how far below the feet we still call "grounded"
    private const int   SlideIters  = 4;

    // sphere centers along the body, as feet-relative Y offsets
    private static readonly float[] SphereY =
    {
        GameConstants.BodyBottom + Radius,
        (GameConstants.BodyBottom + GameConstants.BodyTop) * 0.5f,
        GameConstants.BodyTop - Radius,
    };

    // a scratch list per instance; collision is single-threaded per simulation
    private readonly List<int> _cand = new(64);

    public BakedCollisionWorld(TriangleMesh mesh)
    {
        Mesh = mesh;
        mesh.Bounds(out _min, out _max);
    }

    // --- ICollisionWorld ------------------------------------------------------------------

    public Vector3 CollideAndSlide(Vector3 from, Vector3 delta)
    {
        // Substep so a fast move can't tunnel through a thin wall: each chunk advances at most
        // ~half a radius, then depenetrates. Substep count is a deterministic function of the
        // step length; capped (a teleport-sized delta is the movement guard's problem, not ours).
        float dist = SharedMovement.Len3(delta);
        int steps = (int)MathF.Ceiling(dist / (Radius * 0.5f));
        if (steps < 1) steps = 1;
        if (steps > 16) steps = 16;

        Vector3 pos = from;
        Vector3 chunk = delta * (1f / steps);
        for (int step = 0; step < steps; step++)
            pos = Depenetrate(pos + chunk);
        return pos;
    }

    private Vector3 Depenetrate(Vector3 pos)
    {
        for (int iter = 0; iter < SlideIters; iter++)
        {
            // Find the single deepest sphere/triangle penetration this pass.
            float deepest = 0f;
            Vector3 pushDir = default;
            bool hit = false;

            for (int sIdx = 0; sIdx < SphereY.Length; sIdx++)
            {
                Vector3 center = new(pos.X, pos.Y + SphereY[sIdx], pos.Z);
                Mesh.SphereCandidates(center, Radius, _cand);
                for (int i = 0; i < _cand.Count; i++)
                {
                    int tri = _cand[i];
                    Mesh.Tri(tri, out Vector3 a, out Vector3 b, out Vector3 c);
                    Vector3 cp = Geometry.ClosestPointOnTriangle(center, a, b, c);
                    Vector3 d = center - cp;
                    float dist = SharedMovement.Len3(d);
                    float pen = Radius - dist;
                    if (pen > deepest + 1e-6f)
                    {
                        deepest = pen;
                        pushDir = dist > 1e-6f
                            ? new Vector3(d.X / dist, d.Y / dist, d.Z / dist)
                            : Vector3.Zero;
                        hit = true;
                    }
                }
            }

            if (!hit || deepest <= 1e-5f) break;
            pos += pushDir * (deepest + 1e-4f);  // depenetrate; SharedMovement turns the changed
                                                 // displacement into a slide via velocity recompute
        }
        return pos;
    }

    public bool CheckGrounded(Vector3 position)
    {
        // Short ray straight down from just above the feet. Grounded if it hits a roughly
        // upward-facing surface within the probe distance.
        Vector3 o = new(position.X, position.Y + 0.10f, position.Z);
        if (Mesh.RayCast(o, new Vector3(0f, -1f, 0f), 0.10f + GroundProbe, out _, out Vector3 n))
            return n.Y > 0.5f || n.Y < -0.5f; // accept either winding's up-face
        return false;
    }

    public bool HasHeadroom(Vector3 position)
    {
        // Can a ducked player (HeightDucked) stand to full height here? Ray up from the ducked
        // head toward the standing head; blocked = no headroom.
        Vector3 o = new(position.X, position.Y + GameConstants.HeightDucked, position.Z);
        float need = GameConstants.EyeHeight - GameConstants.HeightDucked + Radius;
        return !Mesh.RayCast(o, new Vector3(0f, 1f, 0f), need, out _, out _);
    }

    /// <summary>The real LOS check — a clear sightline means no triangle blocks the segment.</summary>
    public bool LineOfSight(Vector3 a, Vector3 b)
    {
        Vector3 d = b - a;
        float len = SharedMovement.Len3(d);
        if (len <= 1e-5f) return true;
        Vector3 dir = new(d.X / len, d.Y / len, d.Z / len);
        // nudge the endpoints inward so a shot grazing the target's own surface isn't self-blocked
        float maxD = len - 1e-3f;
        return !Mesh.RayCast(a, dir, maxD, out _, out _);
    }

    public bool Contains(Vector3 p) =>
        p.X >= _min.X && p.X <= _max.X &&
        p.Y >= _min.Y && p.Y <= _max.Y &&
        p.Z >= _min.Z && p.Z <= _max.Z;

    public Vector3 ClampToBounds(Vector3 p) => Vector3.Clamp(p, _min, _max);

    // --- binary world file (.ubw) ---------------------------------------------------------
    //
    // Layout (little-endian): magic 'U','B','W','1' | int32 vertexCount | vertexCount*3 float32
    //                         | int32 triCount | triCount*3 int32

    private static readonly byte[] Magic = { (byte)'U', (byte)'B', (byte)'W', (byte)'1' };

    public static void Write(Stream s, Vector3[] verts, int[] indices)
    {
        using var w = new BinaryWriter(s, System.Text.Encoding.UTF8, leaveOpen: true);
        w.Write(Magic);
        w.Write(verts.Length);
        foreach (Vector3 v in verts) { w.Write(v.X); w.Write(v.Y); w.Write(v.Z); }
        w.Write(indices.Length / 3);
        foreach (int i in indices) w.Write(i);
    }

    public static BakedCollisionWorld Load(Stream s)
    {
        using var r = new BinaryReader(s, System.Text.Encoding.UTF8, leaveOpen: true);
        Span<byte> magic = stackalloc byte[4];
        if (r.Read(magic) != 4 || magic[0] != 'U' || magic[1] != 'B' || magic[2] != 'W' || magic[3] != '1')
            throw new InvalidDataException("not a UBW1 collision world");

        int vc = r.ReadInt32();
        var verts = new Vector3[vc];
        for (int i = 0; i < vc; i++) verts[i] = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());

        int tc = r.ReadInt32();
        var idx = new int[tc * 3];
        for (int i = 0; i < idx.Length; i++) idx[i] = r.ReadInt32();

        return new BakedCollisionWorld(new TriangleMesh(verts, idx));
    }

    public static BakedCollisionWorld Load(byte[] bytes)
    {
        using var ms = new MemoryStream(bytes, writable: false);
        return Load(ms);
    }
}

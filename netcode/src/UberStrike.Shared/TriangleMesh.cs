using System.Numerics;

namespace UberStrike.Shared;

/// <summary>
/// Static collision geometry as an indexed triangle soup with a median-split AABB BVH for
/// fast ray / sphere queries. Built ONCE from a map's baked colliders and used identically by
/// the client (prediction) and server (authority + LOS).
///
/// Determinism: the BVH only *accelerates* queries — it never changes the answer. Every query
/// resolves ties by triangle index, so the result is identical to a brute-force scan regardless
/// of how the tree was built. All math is the determinism-safe kind (explicit Dot3/Cross3).
/// </summary>
public sealed class TriangleMesh
{
    public readonly Vector3[] Vertices;
    public readonly int[]     Indices;   // 3 per triangle
    public int TriangleCount => Indices.Length / 3;

    private readonly Node[] _nodes;
    private readonly int[]  _triOrder;   // triangle indices reordered for leaf contiguity

    private readonly struct Node
    {
        public readonly Vector3 Min, Max;
        public readonly int Left, Right;  // child node indices, or Left == -1 for a leaf
        public readonly int Start, Count; // leaf triangle range into _triOrder
        public Node(Vector3 min, Vector3 max, int left, int right, int start, int count)
        { Min = min; Max = max; Left = left; Right = right; Start = start; Count = count; }
    }

    public TriangleMesh(Vector3[] vertices, int[] indices)
    {
        Vertices = vertices;
        Indices  = indices;

        int triCount = indices.Length / 3;
        _triOrder = new int[triCount];
        for (int i = 0; i < triCount; i++) _triOrder[i] = i;

        var nodes = new List<Node>(Math.Max(1, triCount));
        if (triCount > 0) Build(nodes, 0, triCount);
        else nodes.Add(new Node(Vector3.Zero, Vector3.Zero, -1, -1, 0, 0));
        _nodes = nodes.ToArray();
    }

    public void Tri(int tri, out Vector3 a, out Vector3 b, out Vector3 c)
    {
        a = Vertices[Indices[tri * 3 + 0]];
        b = Vertices[Indices[tri * 3 + 1]];
        c = Vertices[Indices[tri * 3 + 2]];
    }

    // --- queries --------------------------------------------------------------------------

    /// <summary>Nearest ray hit against the mesh. Returns hit distance + face normal.</summary>
    public bool RayCast(Vector3 origin, Vector3 dir, float maxDist, out float t, out Vector3 normal)
    {
        t = maxDist; normal = default;
        bool any = false;
        if (_nodes.Length == 0) return false;

        // Iterative stack traversal (no recursion → no per-platform stack quirks).
        Span<int> stack = stackalloc int[64];
        int sp = 0; stack[sp++] = 0;
        while (sp > 0)
        {
            Node n = _nodes[stack[--sp]];
            if (!RayHitsAabb(origin, dir, n.Min, n.Max, t)) continue;

            if (n.Left < 0)
            {
                for (int i = 0; i < n.Count; i++)
                {
                    int tri = _triOrder[n.Start + i];
                    Tri(tri, out Vector3 a, out Vector3 b, out Vector3 c);
                    if (Geometry.RayTriangle(origin, dir, a, b, c, t, out float th, out Vector3 nrm)
                        && th < t)
                    { t = th; normal = nrm; any = true; }
                }
            }
            else
            {
                if (sp + 2 <= stack.Length) { stack[sp++] = n.Left; stack[sp++] = n.Right; }
            }
        }
        return any;
    }

    /// <summary>
    /// Collect every triangle whose AABB overlaps the sphere into <paramref name="outTris"/>
    /// (cleared first). Caller resolves them (deterministic: outTris is in ascending tri index).
    /// </summary>
    public void SphereCandidates(Vector3 center, float radius, List<int> outTris)
    {
        outTris.Clear();
        if (_nodes.Length == 0) return;
        Vector3 lo = new(center.X - radius, center.Y - radius, center.Z - radius);
        Vector3 hi = new(center.X + radius, center.Y + radius, center.Z + radius);

        Span<int> stack = stackalloc int[64];
        int sp = 0; stack[sp++] = 0;
        while (sp > 0)
        {
            Node n = _nodes[stack[--sp]];
            if (!AabbOverlap(lo, hi, n.Min, n.Max)) continue;
            if (n.Left < 0)
                for (int i = 0; i < n.Count; i++) outTris.Add(_triOrder[n.Start + i]);
            else if (sp + 2 <= stack.Length) { stack[sp++] = n.Left; stack[sp++] = n.Right; }
        }
        outTris.Sort();   // ascending tri index → deterministic resolution order
    }

    public void Bounds(out Vector3 min, out Vector3 max)
    {
        if (_nodes.Length == 0) { min = max = Vector3.Zero; return; }
        min = _nodes[0].Min; max = _nodes[0].Max;
    }

    // --- build ----------------------------------------------------------------------------

    private const int LeafSize = 4;

    private int Build(List<Node> nodes, int start, int count)
    {
        ComputeBounds(start, count, out Vector3 min, out Vector3 max);
        int self = nodes.Count;
        nodes.Add(default); // placeholder

        if (count <= LeafSize)
        {
            nodes[self] = new Node(min, max, -1, -1, start, count);
            return self;
        }

        // split on the longest axis at the centroid median (stable: tie-break by tri index)
        Vector3 ext = max - min;
        int axis = ext.X >= ext.Y ? (ext.X >= ext.Z ? 0 : 2) : (ext.Y >= ext.Z ? 1 : 2);
        int s = start, e = start + count;
        Array.Sort(_triOrder, s, count, Comparer<int>.Create((ta, tb) =>
        {
            float ca = Centroid(ta, axis), cb = Centroid(tb, axis);
            int cmp = ca.CompareTo(cb);
            return cmp != 0 ? cmp : ta.CompareTo(tb);
        }));
        int mid = count / 2;

        // Each subtree may add many nodes, so store BOTH child roots explicitly (the right
        // child is NOT simply left+1 — that was a real bug that made triangles unreachable).
        int left  = Build(nodes, s, mid);
        int right = Build(nodes, s + mid, count - mid);
        nodes[self] = new Node(min, max, left, right, 0, 0);
        return self;
    }

    private float Centroid(int tri, int axis)
    {
        Tri(tri, out Vector3 a, out Vector3 b, out Vector3 c);
        Vector3 ctr = (a + b + c) * (1f / 3f);
        return axis == 0 ? ctr.X : axis == 1 ? ctr.Y : ctr.Z;
    }

    private void ComputeBounds(int start, int count, out Vector3 min, out Vector3 max)
    {
        min = new Vector3(float.MaxValue);
        max = new Vector3(float.MinValue);
        for (int i = 0; i < count; i++)
        {
            Tri(_triOrder[start + i], out Vector3 a, out Vector3 b, out Vector3 c);
            min = Vector3.Min(min, Vector3.Min(a, Vector3.Min(b, c)));
            max = Vector3.Max(max, Vector3.Max(a, Vector3.Max(b, c)));
        }
        // pad so coplanar (axis-aligned) triangles still have a non-degenerate slab to hit
        min -= new Vector3(1e-4f); max += new Vector3(1e-4f);
    }

    private static bool AabbOverlap(Vector3 aMin, Vector3 aMax, Vector3 bMin, Vector3 bMax)
        => aMin.X <= bMax.X && aMax.X >= bMin.X &&
           aMin.Y <= bMax.Y && aMax.Y >= bMin.Y &&
           aMin.Z <= bMax.Z && aMax.Z >= bMin.Z;

    private static bool RayHitsAabb(Vector3 o, Vector3 d, Vector3 min, Vector3 max, float maxDist)
    {
        float tmin = 0f, tmax = maxDist;
        for (int i = 0; i < 3; i++)
        {
            float oi = i == 0 ? o.X : i == 1 ? o.Y : o.Z;
            float di = i == 0 ? d.X : i == 1 ? d.Y : d.Z;
            float lo = i == 0 ? min.X : i == 1 ? min.Y : min.Z;
            float hi = i == 0 ? max.X : i == 1 ? max.Y : max.Z;
            if (di > -1e-9f && di < 1e-9f) { if (oi < lo || oi > hi) return false; }
            else
            {
                float inv = 1f / di;
                float t1 = (lo - oi) * inv, t2 = (hi - oi) * inv;
                if (t1 > t2) (t1, t2) = (t2, t1);
                if (t1 > tmin) tmin = t1;
                if (t2 < tmax) tmax = t2;
                if (tmin > tmax) return false;
            }
        }
        return true;
    }
}

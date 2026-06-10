#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Exports a loaded map's static collision into the .ubw triangle-soup format that the netcode
// server + client load via BakedCollisionWorld. Drop this file into the Unity project's
// Assets/Editor/ folder; run it with the map scene open.
//
// What it bakes:
//   * MeshColliders  -> their shared mesh triangles, transformed to world space.
//   * BoxColliders   -> 12 triangles of the oriented box.
//   * Terrain        -> (TODO) skipped for now; UberStrike maps are mesh/box based.
// Triggers are skipped (no collision). Only enabled colliders on non-ignored layers are baked.
//
// The output coordinate system matches the netcode (System.Numerics.Vector3 = Unity's x,y,z),
// so no axis remap is needed — the client and server share these exact numbers.
public static class CollisionWorldExporter
{
    // Layers that never block movement/LOS (tune per project). Players are excluded so the
    // baked world is static-only; dynamic hitboxes live in HitboxHistory, not here.
    private const string IgnoreLayers = "Player,Ignore Raycast,UI,Water";

    [MenuItem("File/Build UberStrike/Netcode/Export Collision World (.ubw)")]
    public static void Export()
    {
        int ignoreMask = LayerMask.GetMask(IgnoreLayers.Split(','));

        var verts = new List<Vector3>();
        var tris  = new List<int>();
        var vmap  = new Dictionary<Vector3, int>(); // dedupe identical world-space verts

        int meshColliders = 0, boxColliders = 0;
        foreach (Collider col in Object.FindObjectsOfType<Collider>())
        {
            if (!col.enabled || col.isTrigger) continue;
            if (((1 << col.gameObject.layer) & ignoreMask) != 0) continue;

            switch (col)
            {
                case MeshCollider mc when mc.sharedMesh != null:
                    AddMesh(mc.sharedMesh, mc.transform, verts, tris, vmap);
                    meshColliders++;
                    break;
                case BoxCollider bc:
                    AddBox(bc, verts, tris, vmap);
                    boxColliders++;
                    break;
                // CapsuleCollider / SphereCollider on static geometry are rare in these maps;
                // add tessellation here if a map needs them.
            }
        }

        string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string dir = Path.Combine(Application.dataPath, "..", "Netcode", "Worlds");
        Directory.CreateDirectory(dir);
        string path = Path.GetFullPath(Path.Combine(dir, scene + ".ubw"));

        WriteUbw(path, verts, tris);
        Debug.LogFormat("[CollisionWorld] {0}: {1} verts, {2} tris ({3} mesh + {4} box colliders) -> {5}",
            scene, verts.Count, tris.Count / 3, meshColliders, boxColliders, path);
    }

    private static int Add(Vector3 v, List<Vector3> verts, Dictionary<Vector3, int> vmap)
    {
        if (vmap.TryGetValue(v, out int idx)) return idx;
        idx = verts.Count; verts.Add(v); vmap[v] = idx; return idx;
    }

    private static void AddMesh(Mesh mesh, Transform t, List<Vector3> verts, List<int> tris,
                                Dictionary<Vector3, int> vmap)
    {
        Vector3[] mv = mesh.vertices;
        int[] mt = mesh.triangles;
        var world = new Vector3[mv.Length];
        for (int i = 0; i < mv.Length; i++) world[i] = t.TransformPoint(mv[i]);
        for (int i = 0; i < mt.Length; i += 3)
        {
            tris.Add(Add(world[mt[i]],     verts, vmap));
            tris.Add(Add(world[mt[i + 1]], verts, vmap));
            tris.Add(Add(world[mt[i + 2]], verts, vmap));
        }
    }

    private static void AddBox(BoxCollider bc, List<Vector3> verts, List<int> tris,
                               Dictionary<Vector3, int> vmap)
    {
        Vector3 c = bc.center, s = bc.size * 0.5f;
        Transform t = bc.transform;
        Vector3[] corner = new Vector3[8];
        int k = 0;
        for (int xi = -1; xi <= 1; xi += 2)
            for (int yi = -1; yi <= 1; yi += 2)
                for (int zi = -1; zi <= 1; zi += 2)
                    corner[k++] = t.TransformPoint(c + new Vector3(s.x * xi, s.y * yi, s.z * zi));

        // 12 triangles over the 8 corners (indices into `corner`).
        int[] face = { 0,1,3, 0,3,2, 4,6,7, 4,7,5, 0,4,5, 0,5,1, 2,3,7, 2,7,6, 0,2,6, 0,6,4, 1,5,7, 1,7,3 };
        foreach (int fi in face) tris.Add(Add(corner[fi], verts, vmap));
    }

    private static readonly byte[] Magic = { (byte)'U', (byte)'B', (byte)'W', (byte)'1' };

    private static void WriteUbw(string path, List<Vector3> verts, List<int> tris)
    {
        using var w = new BinaryWriter(File.Create(path));
        w.Write(Magic);
        w.Write(verts.Count);
        foreach (Vector3 v in verts) { w.Write(v.x); w.Write(v.y); w.Write(v.z); }
        w.Write(tris.Count / 3);
        foreach (int i in tris) w.Write(i);
    }
}
#endif

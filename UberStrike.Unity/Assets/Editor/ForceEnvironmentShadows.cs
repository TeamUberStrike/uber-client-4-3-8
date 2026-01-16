using UnityEngine;
using UnityEditor;

public class ForceEnvironmentShadows : MonoBehaviour
{
    [MenuItem("UberStrike/Shadows/Force Environment Shadows (Trees & Terrain)")]
    public static void ForceEnvShadows()
    {
        Debug.Log("=== FORCING ENVIRONMENT SHADOWS ===");
        
        // 1. Terrain
        Terrain[] terrains = FindObjectsOfType<Terrain>();
        foreach (var t in terrains)
        {
            t.castShadows = true;
            t.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            t.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Simple;
            EditorUtility.SetDirty(t);
            Debug.Log($"[TERRAIN] Shadows enabled on {t.name}");
        }
        
        // 2. Trees (Mesh Renderers)
        var renderers = FindObjectsOfType<MeshRenderer>();
        foreach (var r in renderers)
        {
            // Heuristic to find trees/buildings
            if (r.name.ToLower().Contains("tree") || r.name.ToLower().Contains("leaf") || r.name.ToLower().Contains("trunk") ||
                r.name.ToLower().Contains("building") || r.name.ToLower().Contains("structure"))
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                r.receiveShadows = true;
                EditorUtility.SetDirty(r);
            }
        }
        
        // 3. Update BillboardTree Shader (Brute force overwrite)
        Shader treeShader = Shader.Find("Hidden/TerrainEngine/BillboardTree");
        if (treeShader != null)
        {
            Debug.Log($"[SHADER] Found BillboardTree shader. Ensure 'Fallback \"Diffuse\"' is set!");
        }

        Debug.Log("=== ENVIRONMENT SHADOWS FORCED ===");
    }
}

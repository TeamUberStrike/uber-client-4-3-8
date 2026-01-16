using UnityEngine;
using UnityEditor;

public class FixExplosionMaterials : MonoBehaviour
{
    [MenuItem("UberStrike/Fix Explosion & Splat Materials")]
    public static void Fix()
    {
        // 1. Fix Explosion_rob.psd.mat
        string[] matPaths = new string[] {
            "Assets/Artwork/QuickItems/FPXRobExplosion/Materials/Explosion_rob.psd.mat",
            "Assets/Artwork/WeaponEffects/HaloCannonExplosion/Materials/Explosion_Debris.mat",
            "Assets/Artwork/WeaponEffects/Particles/Materials/TrailExplosion.mat",
            "Assets/Artwork/WeaponEffects/Particles/Materials/WaterExtraSplat.mat"
        };
        
        Shader particleShader = Shader.Find("Mobile/Particles/Alpha Blended");
        if(particleShader == null) particleShader = Shader.Find("Particles/Alpha Blended"); // Fallback
        
        foreach (string path in matPaths)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null)
            {
                mat.shader = particleShader;
                EditorUtility.SetDirty(mat);
                Debug.Log($"Fixed Shader for {mat.name} -> {particleShader.name}");
            }
            else
            {
                Debug.LogWarning($"Could not find material at {path}");
            }
        }
        
        // 2. Locate and Fix "Wood" / "Stone" / "Metal" particle materials
        // These are often children of the ParticleSystem prefab we imported.
        // We can iterate the ParticleEffectController children and check their renderers.
        
        var pec = FindObjectOfType<ParticleEffectController>();
        if(pec != null)
        {
            foreach(var r in pec.GetComponentsInChildren<ParticleSystemRenderer>(true))
            {
                if(r.sharedMaterial == null || r.sharedMaterial.name == "Default-Material" || r.sharedMaterial.shader.name == "Hidden/InternalErrorShader" || r.sharedMaterial.shader.name.Contains("Pink"))
                {
                    // Assign a default smoke/spark material?
                    // Let's try to load one.
                     Material smokeMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Standard Assets/Particles/Sources/Materials/Smoke2.mat");
                     if(smokeMat != null)
                     {
                         r.sharedMaterial = smokeMat;
                         EditorUtility.SetDirty(r);
                         Debug.Log($"Assigned fallback material 'Smoke2' to {r.gameObject.name}");
                     }
                }
            }
        }
    }
}

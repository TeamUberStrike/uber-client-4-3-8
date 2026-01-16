using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class FixMissingParticleMaterials : MonoBehaviour
{
    [MenuItem("UberStrike/Fix Missing Particle Materials")]
    public static void Fix()
    {
        var pec = FindObjectOfType<ParticleEffectController>();
        if (pec == null)
        {
            Debug.LogError("Could not find ParticleEffectController in the scene.");
            return;
        }

        Debug.Log($"Fixing particles on: {pec.name}");
        var renderers = pec.GetComponentsInChildren<Renderer>(true);
        
        // Load common materials
        Material spawnMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Artwork/Particles/SpwanParticles/SpawnParticles.mat");
        Material smokeMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Standard Assets/Particles/Sources/Materials/Smoke2.mat");
        Material flameMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Standard Assets/Particles/Sources/Materials/FlameA.mat");
        Material waterCircleMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Artwork/WeaponEffects/Particles/Materials/WaterCircle.mat");
        Material waterDropsMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Artwork/WeaponEffects/Particles/Materials/WaterDrops.mat");
        Material heatWaveMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Artwork/WeaponEffects/HeatWave/HeatDistort_3.0.mat"); 
        Material explosionMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Artwork/QuickItems/FPXRobExplosion/Materials/Explosion_rob.psd.mat");

        // NEW: Imported Materials from 4.7
        Material woodMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Imported/ParticleMaterials/WoodSplintersParticle.mat");
        Material stoneMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Imported/ParticleMaterials/TinyStonesParticle.mat");
        Material woodDecal = AssetDatabase.LoadAssetAtPath<Material>("Assets/Imported/ParticleMaterials/BulletDecalWood.mat"); // Might need a quad?

        foreach (var r in renderers)
        {
            // If it's already "Smoke2", we might want to upgrade it if it's named "Wood"
            bool isFallback = r.sharedMaterial != null && r.sharedMaterial.name == "Smoke2";
            if (r.sharedMaterial != null && r.sharedMaterial.name != "Default-Material" && !isFallback) continue; 

            string name = r.gameObject.name.ToLower();
            Material target = null;

            if (name.Contains("spawnparticles")) target = spawnMat;
            
            // SPECIFIC SURFACE TYPES
            else if (name.Contains("wood")) target = woodMat;
            else if (name.Contains("stone") || name.Contains("concrete")) target = stoneMat;
            else if (name.Contains("metal")) target = stoneMat; // Fallback to stone/sparks?
            else if (name.Contains("splat")) target = stoneMat; // Generic dust?

            else if (name.Contains("smoke")) target = smokeMat;
            else if (name.Contains("fire") || name.Contains("flame")) target = flameMat;
            else if (name.Contains("watercircle") || name.Contains("ripples")) target = waterCircleMat;
            else if (name.Contains("waterdrops") || name.Contains("splash") || name.Contains("water")) target = waterDropsMat;
            else if (name.Contains("heatwave")) target = heatWaveMat;
            else if (name.Contains("explosionblast") || name.Contains("blast")) target = explosionMat;
            else if (name.Contains("spark") || name.Contains("flash")) target = flameMat; 
            else if (name.Contains("dust") || name.Contains("debris")) target = smokeMat;
            
            // FORCE LAYER 0 (Default) for visibility
            r.gameObject.layer = 0;

            if (target != null)
            {
                r.sharedMaterial = target;
                
                // Ensure Shader is visible (Alpha Blended for decals, Additive for fire)
                if(target.shader.name != "Mobile/Particles/Alpha Blended" && target.shader.name != "Mobile/Particles/Additive" && target.shader.name != "Particles/Alpha Blended")
                {
                     // Decide based on type
                     if(name.Contains("wood") || name.Contains("stone") || name.Contains("splat"))
                        target.shader = Shader.Find("Mobile/Particles/Alpha Blended");
                     else
                        target.shader = Shader.Find("Mobile/Particles/Additive");
                }
                
                EditorUtility.SetDirty(r);
                EditorUtility.SetDirty(target);
                Debug.Log($"[FIXED] Assigned '{target.name}' to '{r.gameObject.name}' (Layer 0, Shader Fixed)");
            }
            else
            {
                if(r.sharedMaterial == null || r.sharedMaterial.name == "Default-Material") 
                {
                    r.sharedMaterial = smokeMat;
                    EditorUtility.SetDirty(r);
                    Debug.Log($"[FALLBACK] Assigned Smoke2 to unknown particle '{r.gameObject.name}'");
                }
            }
        }
    }
}

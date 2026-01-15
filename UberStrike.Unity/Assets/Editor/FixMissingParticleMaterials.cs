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

        foreach (var r in renderers)
        {
            if (r.sharedMaterial != null && r.sharedMaterial.name != "Default-Material") continue; // Skip if already has a valid material

            string name = r.gameObject.name.ToLower();
            Material target = null;

            if (name.Contains("spawnparticles")) target = spawnMat;
            else if (name.Contains("smoke")) target = smokeMat;
            else if (name.Contains("fire") || name.Contains("flame")) target = flameMat;
            else if (name.Contains("watercircle") || name.Contains("ripples")) target = waterCircleMat;
            else if (name.Contains("waterdrops") || name.Contains("splash")) target = waterDropsMat;
            else if (name.Contains("heatwave")) target = heatWaveMat;
            else if (name.Contains("explosionblast") || name.Contains("blast")) target = explosionMat;
            else if (name.Contains("spark") || name.Contains("flash")) target = flameMat; 
            else if (name.Contains("dust") || name.Contains("debris")) target = smokeMat;
            
            if (target != null)
            {
                r.sharedMaterial = target;
                EditorUtility.SetDirty(r);
                Debug.Log($"[FIXED] Assigned '{target.name}' to '{r.gameObject.name}'");
            }
            else
            {
                r.sharedMaterial = smokeMat;
                 EditorUtility.SetDirty(r);
                Debug.Log($"[FALLBACK] Assigned Smoke2 to unknown particle '{r.gameObject.name}'");
            }
        }
    }
}

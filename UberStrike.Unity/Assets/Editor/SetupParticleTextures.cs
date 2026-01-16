using UnityEngine;
using UnityEditor;
using System.IO;

public class SetupParticleTextures : MonoBehaviour
{
    [MenuItem("UberStrike/Setup Particle Textures (Final Fix)")]
    public static void Setup()
    {
        SetupExplosion();
        SetupBulletHoles();
    }

    static void SetupExplosion()
    {
        // 1. Find Texture
        string texPath = "Assets/Imported/ParticleMaterials/Explosion_Fireball_TexS.png";
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);

        if (tex == null)
        {
            Debug.LogError($"Could not find texture at {texPath}. Did the copy finish?");
            return;
        }

        // 2. Find Materials
        string[] matPaths = new string[] {
            "Assets/Artwork/QuickItems/FPXRobExplosion/Materials/Explosion_rob.psd.mat",
            "Assets/Artwork/WeaponEffects/HaloCannonExplosion/Materials/Explosion_Debris.mat",
            "Assets/Artwork/WeaponEffects/Particles/Materials/TrailExplosion.mat"
        };

        foreach (var path in matPaths)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null)
            {
                mat.mainTexture = tex;
                
                // Ensure Shader is valid
                if(mat.shader.name == "Hidden/InternalErrorShader" || mat.shader.name.Contains("Error"))
                {
                    mat.shader = Shader.Find("Mobile/Particles/Alpha Blended");
                }
                
                EditorUtility.SetDirty(mat);
                Debug.Log($"[FIX] Assigned Explosion Texture to {mat.name}");
            }
        }
    }

    static void SetupBulletHoles()
    {
        // 1. Find Texture (Using WoodHole as generic for now)
        string texPath = "Assets/Imported/ParticleMaterials/WoodBulletHoleAlbedo.tif";
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        
        if (tex == null)
        {
             // Try looking for the Wood one from 4.7 copy
             texPath = "Assets/Imported/ParticleMaterials/WoodAlbedo.tif"; // Fallback
             tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        }

        if (tex == null)
        {
            Debug.LogError("Could not find Bullet Hole texture.");
            return;
        }

        // 2. Create/Update Material
        string matPath = "Assets/Imported/ParticleMaterials/GeneralBulletHole.mat";
        Material holeMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (holeMat == null)
        {
            holeMat = new Material(Shader.Find("Mobile/Particles/Alpha Blended"));
            AssetDatabase.CreateAsset(holeMat, matPath);
        }
        else
        {
            holeMat.shader = Shader.Find("Mobile/Particles/Alpha Blended");
        }
        
        holeMat.mainTexture = tex;
        EditorUtility.SetDirty(holeMat);

        // 3. Assign to 'Splat' emitter
        var pec = FindObjectOfType<ParticleEffectController>();
        if (pec != null)
        {
            // Find "Splat" child
            Transform splat = FindRecursive(pec.transform, "Splat");
            if (splat != null)
            {
                 var r = splat.GetComponent<ParticleSystemRenderer>();
                 if(r != null)
                 {
                     r.sharedMaterial = holeMat;
                     EditorUtility.SetDirty(r);
                     Debug.Log("[FIX] Assigned BulletHole material to 'Splat' emitter.");
                 }
                 
                 // Also ensure it Doesn't loop and Stays on wall
                 var ps = splat.GetComponent<ParticleSystem>();
                 if(ps != null)
                 {
                     var main = ps.main;
                     main.simulationSpace = ParticleSystemSimulationSpace.World;
                     main.startSpeed = 0; // Stay on wall!
                     main.loop = false;
                     // Rotation?
                 }
            }
            else Debug.LogWarning("Could not find 'Splat' child in PEC.");
        }
    }

    private static Transform FindRecursive(Transform parent, string name)
    {
        if (parent.name.ToLower().Contains(name.ToLower())) return parent;
        foreach (Transform child in parent)
        {
            var result = FindRecursive(child, name);
            if (result != null) return result;
        }
        return null;
    }
}

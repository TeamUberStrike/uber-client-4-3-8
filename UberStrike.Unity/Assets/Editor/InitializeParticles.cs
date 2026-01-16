using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Collections;

public class InitializeParticles : MonoBehaviour
{
    [MenuItem("UberStrike/★ Initialize All Particles (Run Once Per Scene)")]
    public static void InitializeAll()
    {
        var pec = FindObjectOfType<ParticleEffectController>();
        if (pec == null)
        {
            Debug.LogError("ParticleEffectController not found!");
            return;
        }

        Debug.Log("=== INITIALIZING ALL PARTICLES ===");
        
        // STEP 1: Refill References
        Debug.Log("[1/4] Refilling particle references...");
        RefillReferences(pec);
        
        // STEP 2: Fix Materials
        Debug.Log("[2/4] Fixing materials and textures...");
        FixMaterials(pec);
        
        // STEP 3: Set Correct Sizes
        Debug.Log("[3/4] Setting particle sizes...");
        SetSizes(pec);
        
        // STEP 4: Force Ready State
        Debug.Log("[4/4] Forcing ready state...");
        ForceReady(pec);
        

        
        EditorUtility.SetDirty(pec);
        AssetDatabase.SaveAssets();
        
        Debug.Log("=== INITIALIZATION COMPLETE ===");
        Debug.Log("Particles are ready! Play the game now.");
    }

    private static void RefillReferences(ParticleEffectController pec)
    {
        var type = pec.GetType();
        var allWeaponDataField = type.GetField("_allWeaponData", BindingFlags.NonPublic | BindingFlags.Instance);
        var allWeaponData = allWeaponDataField.GetValue(pec) as IList;

        if (allWeaponData == null) return;

        foreach (var config in allWeaponData)
        {
            if (config == null) continue;
            
            var configType = config.GetType();
            var dataField = configType.GetField("Configuration");
            var data = dataField.GetValue(config);

            if (data != null)
            {
                var impactConfigField = data.GetType().GetField("_weaponImpactEffectConfiguration", BindingFlags.NonPublic | BindingFlags.Instance);
                var impactConfig = impactConfigField?.GetValue(data);
                
                if (impactConfig != null)
                {
                    var surfaces = impactConfig.GetType().GetField("SurfaceParameterSet").GetValue(impactConfig);
                    LinkSurface(pec.transform, surfaces, "WoodEffect", "Wood");
                    LinkSurface(pec.transform, surfaces, "StoneEffect", "Stone");
                    LinkSurface(pec.transform, surfaces, "MetalEffect", "Metal");
                    LinkSurface(pec.transform, surfaces, "GrassEffect", "Grass");
                    LinkSurface(pec.transform, surfaces, "SandEffect", "Sand");
                    LinkSurface(pec.transform, surfaces, "Splat", "Splat");
                    
                    var explosionParam = impactConfig.GetType().GetField("ExplosionParameterSet").GetValue(impactConfig);
                    if (explosionParam != null)
                    {
                        LinkExplosion(pec.transform, explosionParam, "BlastParameters", "ExplosionBlast");
                        LinkExplosion(pec.transform, explosionParam, "DustParameters", "ExplosionDust");
                        LinkExplosion(pec.transform, explosionParam, "SmokeParameters", "ExplosionSmoke");
                    }
                }
            }
        }
    }

    private static void FixMaterials(ParticleEffectController pec)
    {
        // Load textures
        Texture2D explosionTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Imported/ParticleMaterials/Explosion_Fireball_TexS.png");
        Texture2D woodTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Imported/ParticleMaterials/ParticleWood.png");
        Texture2D stoneTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Imported/ParticleMaterials/TinyStones.png");
        
        var allSystems = pec.GetComponentsInChildren<ParticleSystem>(true);
        
        foreach (var ps in allSystems)
        {
            string name = ps.name.ToLower();
            var rend = ps.GetComponent<ParticleSystemRenderer>();
            if (rend == null) continue;
            
            // Create materials based on particle type
            if (name.Contains("explosion") && name.Contains("blast") && explosionTex != null)
            {
                Material mat = new Material(Shader.Find("Mobile/Particles/Additive"));
                mat.name = "ExplosionBlastMat";
                mat.mainTexture = explosionTex;
                mat.SetColor("_TintColor", new Color(1f, 0.8f, 0.5f, 1f));
                rend.sharedMaterial = mat;
            }
            else if (name.Contains("wood") && woodTex != null)
            {
                Material mat = new Material(Shader.Find("Mobile/Particles/Alpha Blended"));
                mat.name = "WoodParticleMat";
                mat.mainTexture = woodTex;
                rend.sharedMaterial = mat;
            }
            else if (name.Contains("stone") && stoneTex != null)
            {
                Material mat = new Material(Shader.Find("Mobile/Particles/Alpha Blended"));
                mat.name = "StoneParticleMat";
                mat.mainTexture = stoneTex;
                rend.sharedMaterial = mat;
            }
            
            rend.enabled = true;
            EditorUtility.SetDirty(rend);
        }
    }

    private static void SetSizes(ParticleEffectController pec)
    {
        var allSystems = pec.GetComponentsInChildren<ParticleSystem>(true);
        
        foreach (var ps in allSystems)
        {
            string name = ps.name.ToLower();
            var main = ps.main;
            
            if (name.Contains("spawn") || name.Contains("pickup") || name.Contains("star"))
            {
                main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.05f); // Tiny
            }
            else if (name.Contains("wood") || name.Contains("stone") || name.Contains("metal"))
            {
                main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f); // Small debris
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
            }
            else if (name.Contains("explosion") && name.Contains("blast"))
            {
                main.startSize = new ParticleSystem.MinMaxCurve(3f, 6f); // Large explosion
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
            }
            
            EditorUtility.SetDirty(ps);
        }
    }

    private static void ForceReady(ParticleEffectController pec)
    {
        var allSystems = pec.GetComponentsInChildren<ParticleSystem>(true);
        
        foreach (var ps in allSystems)
        {
            ps.gameObject.SetActive(true);
            
            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.maxParticles = 1000;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            
            var emission = ps.emission;
            emission.enabled = true;
            
            if (!ps.isPlaying) ps.Play();
            
            EditorUtility.SetDirty(ps);
        }
    }

    // Helper methods
    private static void LinkSurface(Transform root, object surfacesObj, string fieldName, string childName)
    {
        var field = surfacesObj.GetType().GetField(fieldName);
        if (field == null) return;
        var effectConfig = field.GetValue(surfacesObj);
        if (effectConfig == null) return;
        LinkEmitter(root, effectConfig, childName);
    }

    private static void LinkExplosion(Transform root, object explObj, string fieldName, string childName)
    {
        var field = explObj.GetType().GetField(fieldName);
        if (field == null) return;
        var paramObj = field.GetValue(explObj);
        if (paramObj == null) return;
        LinkEmitter(root, paramObj, childName);
    }

    private static void LinkEmitter(Transform root, object configObj, string childName)
    {
        var emitterField = configObj.GetType().GetField("ParticleEmitter");
        if (emitterField == null) return;

        ParticleSystem current = emitterField.GetValue(configObj) as ParticleSystem;
        if (current == null)
        {
            Transform t = FindRecursive(root, childName);
            if (t != null)
            {
                var ps = t.GetComponent<ParticleSystem>();
                if (ps == null) ps = t.gameObject.AddComponent<ParticleSystem>();
                emitterField.SetValue(configObj, ps);
            }
        }
    }

    private static Transform FindRecursive(Transform parent, string name)
    {
        if (parent.name.Equals(name, System.StringComparison.OrdinalIgnoreCase)) return parent;
        foreach (Transform child in parent)
        {
            var result = FindRecursive(child, name);
            if (result != null) return result;
        }
        return null;
    }
}

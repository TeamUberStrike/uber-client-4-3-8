using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

public class RefillParticleReferences : MonoBehaviour
{
    [MenuItem("UberStrike/Refill Particle References (Linker)")]
    public static void Refill()
    {
        var pec = FindObjectOfType<ParticleEffectController>();
        if (pec == null)
        {
            Debug.LogError("No ParticleEffectController found!");
            return;
        }

        Debug.Log($"Refilling references for: {pec.name}");
        Undo.RecordObject(pec, "Refill Particle Refs");

        // 1. Get Access to Private Data
        var type = pec.GetType();
        var allWeaponDataField = type.GetField("_allWeaponData", BindingFlags.NonPublic | BindingFlags.Instance);
        var allWeaponData = allWeaponDataField.GetValue(pec) as IList;

        if (allWeaponData == null) { Debug.LogError("Weapon Data is Null"); return; }

        int fixes = 0;

        foreach (var config in allWeaponData)
        {
            // config is 'ParticleConfiguration'
            if (config == null) continue;
            
            var configType = config.GetType();
            var dataField = configType.GetField("Configuration");
            var data = dataField.GetValue(config); // ParticleCobfigurationPerWeapon

            if (data != null)
            {
                // Fix: Use NonPublic binding and correct field name "_weaponImpactEffectConfiguration"
                var impactConfigField = data.GetType().GetField("_weaponImpactEffectConfiguration", BindingFlags.NonPublic | BindingFlags.Instance);
                if (impactConfigField == null) 
                {
                    Debug.LogWarning($"Could not find _weaponImpactEffectConfiguration on {data}");
                    continue;
                }
                
                var impactConfig = impactConfigField.GetValue(data);
                if (impactConfig != null)
                {
                    // Relink Surfaces
                    var surfaces = impactConfig.GetType().GetField("SurfaceParameterSet").GetValue(impactConfig);
                    fixes += LinkSurface(pec.transform, surfaces, "WoodEffect", "Wood");
                    fixes += LinkSurface(pec.transform, surfaces, "StoneEffect", "Stone");
                    fixes += LinkSurface(pec.transform, surfaces, "MetalEffect", "Metal");
                    fixes += LinkSurface(pec.transform, surfaces, "GrassEffect", "Grass");
                    fixes += LinkSurface(pec.transform, surfaces, "SandEffect", "Sand");
                    fixes += LinkSurface(pec.transform, surfaces, "Splat", "Splat");
                    fixes += LinkSurface(pec.transform, surfaces, "WaterCircleEffect", "WaterCircle", true); // FireConfig type
                    fixes += LinkSurface(pec.transform, surfaces, "WaterExtraSplashEffect", "WaterExtra", true); // FireConfig type
                    
                    // Relink Explosion (if applicable)
                     var explosionParam = impactConfig.GetType().GetField("ExplosionParameterSet").GetValue(impactConfig);
                     if(explosionParam != null)
                     {
                         // ExplosionDust, ExplosionSmoke, etc.
                         fixes += LinkExplosion(pec.transform, explosionParam, "DustParameters", "ExplosionDust");
                         fixes += LinkExplosion(pec.transform, explosionParam, "SmokeParameters", "ExplosionSmoke");
                         fixes += LinkExplosion(pec.transform, explosionParam, "SparkParameters", "ExplosionSpark");
                          fixes += LinkExplosion(pec.transform, explosionParam, "BlastParameters", "ExplosionBlast");
                     }
                }
            }
        }
        
        Debug.Log($"Refill Complete. Re-linked {fixes} emitters.");
        EditorUtility.SetDirty(pec);
    }

    private static int LinkSurface(Transform root, object surfacesObj, string fieldName, string childName, bool isFireConfig = false)
    {
        var field = surfacesObj.GetType().GetField(fieldName);
        if (field == null) return 0;

        var effectConfig = field.GetValue(surfacesObj);
        if (effectConfig == null) return 0;

        return LinkEmitter(root, effectConfig, childName);
    }
    
    private static int LinkExplosion(Transform root, object explObj, string fieldName, string childName)
    {
         var field = explObj.GetType().GetField(fieldName);
         if(field == null) return 0;
         var paramObj = field.GetValue(explObj);
         if(paramObj == null) return 0;
         
         return LinkEmitter(root, paramObj, childName);
    }

    private static int LinkEmitter(Transform root, object configObj, string childName)
    {
        var emitterField = configObj.GetType().GetField("ParticleEmitter");
        if (emitterField == null) return 0;

        ParticleSystem current = emitterField.GetValue(configObj) as ParticleSystem;
        if (current == null)
        {
            // Find child
            Transform t = FindRecursive(root, childName);
            if (t != null)
            {
                var ps = t.GetComponent<ParticleSystem>();
                if (ps == null) ps = t.gameObject.AddComponent<ParticleSystem>();
                
                emitterField.SetValue(configObj, ps);
                //Debug.Log($"Linked '{childName}' to config.");
                return 1;
            }
        }
        return 0;
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

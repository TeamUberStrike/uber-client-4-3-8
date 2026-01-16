using UnityEngine;
using UnityEditor;

public class FixAllExplosionParts : MonoBehaviour
{
    [MenuItem("UberStrike/Particles/4. Fix ALL Explosion Parts (Blast, Dust, Smoke)")]
    public static void FixAll()
    {
        Debug.Log("=== FIXING ALL EXPLOSION PARTS ===");
        
        // 1. Get/Create Materials
        Material blastMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Imported/ParticleMaterials/Permanent_ExplosionBlast.mat");
        Material stoneMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Imported/ParticleMaterials/Permanent_StoneChips.mat");
        
        // Create a generic smoke/dust material if needed
        Material smokeMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Imported/ParticleMaterials/Smoke2.mat");
        if (smokeMat == null)
        {
            // Try to find ANY smoke material
            string[] guids = AssetDatabase.FindAssets("Smoke t:Material");
            if (guids.Length > 0) smokeMat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        if (blastMat == null || stoneMat == null)
        {
            Debug.LogError("Permanent materials missing! Run step 1 first.");
            return;
        }

        var pec = FindObjectOfType<ParticleEffectController>();
        if (pec == null) return;
        
        var allSystems = pec.GetComponentsInChildren<ParticleSystem>(true);
        int fixedCount = 0;
        
        foreach (var ps in allSystems)
        {
            string name = ps.name.ToLower();
            var rend = ps.GetComponent<ParticleSystemRenderer>();
            if (rend == null) continue;
            
            bool fixedItem = false;

            // 1. Blast (The main fireball)
            if (name.Contains("blast"))
            {
                rend.sharedMaterial = blastMat;
                rend.renderMode = ParticleSystemRenderMode.Billboard;
                fixedItem = true;
            }
            // 2. Dust / Smoke (The cloud)
            else if (name.Contains("dust") || name.Contains("smoke"))
            {
                if (smokeMat != null)
                {
                    rend.sharedMaterial = smokeMat;
                    fixedItem = true;
                }
                else
                {
                    // Fallback to stone material if no smoke found, better than pink box
                    rend.sharedMaterial = stoneMat; 
                    fixedItem = true;
                }
            }
            // 3. Spark / Debris
            else if (name.Contains("spark") || name.Contains("debris"))
            {
                rend.sharedMaterial = blastMat; // Sparks can use fireball texture (looks like fire)
                rend.renderMode = ParticleSystemRenderMode.Stretch;
                fixedItem = true;
            }
            // 4. Trail
            else if (name.Contains("trail"))
            {
                rend.sharedMaterial = blastMat;
                rend.renderMode = ParticleSystemRenderMode.Stretch;
                fixedItem = true;
            }

            if (fixedItem)
            {
                EditorUtility.SetDirty(rend);
                fixedCount++;
            }
        }
        
        Debug.Log($"Fixed {fixedCount} explosion parts across all weapons (Cannons, Splatterguns, Launchers).");
        Debug.Log("SAVE SCENE NOW!");
    }
}

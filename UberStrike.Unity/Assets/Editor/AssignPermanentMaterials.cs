using UnityEngine;
using UnityEditor;

public class AssignPermanentMaterials : MonoBehaviour
{
    [MenuItem("UberStrike/Particles/2. Assign Permanent Materials")]
    public static void AssignMaterials()
    {
        Debug.Log("Assigning permanent materials...");
        
        // Load our permanent materials
        Material blastMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Imported/ParticleMaterials/Permanent_ExplosionBlast.mat");
        Material stoneMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Imported/ParticleMaterials/Permanent_StoneChips.mat");
        
        if (blastMat == null || stoneMat == null)
        {
            Debug.LogError("Permanent materials not found! Run 'Create Permanent Materials' first.");
            return;
        }
        
        var pec = FindObjectOfType<ParticleEffectController>();
        if (pec == null) return;
        
        var allSystems = pec.GetComponentsInChildren<ParticleSystem>(true);
        int assigned = 0;
        
        foreach (var ps in allSystems)
        {
            string name = ps.name.ToLower();
            var rend = ps.GetComponent<ParticleSystemRenderer>();
            
            if (rend == null) continue;
            
            // Assign Explosion Material
            if (name.Contains("explosion") && name.Contains("blast"))
            {
                rend.sharedMaterial = blastMat;
                rend.renderMode = ParticleSystemRenderMode.Billboard;
                assigned++;
            }
            // Assign Stone Material (also used for Default/Terrain)
            else if (name.Contains("stone") || (name.Contains("default") && !name.Contains("particle")))
            {
                rend.sharedMaterial = stoneMat;
                assigned++;
            }
            
            EditorUtility.SetDirty(rend);
        }
        
        Debug.Log($"Assigned permanent materials to {assigned} particle systems. SAVE THE SCENE NOW!");
    }
}

using UnityEngine;

public class RuntimeParticleDiagnostic : MonoBehaviour
{
    void Start()
    {
        Debug.Log("=== RUNTIME PARTICLE DIAGNOSTIC ===");
        
        var pec = FindObjectOfType<ParticleEffectController>();
        if (pec == null)
        {
            Debug.LogError("No ParticleEffectController found!");
            return;
        }

        // Check explosion particles
        Transform blastTransform = FindRecursive(pec.transform, "ExplosionBlast");
        if (blastTransform != null)
        {
            var ps = blastTransform.GetComponent<ParticleSystem>();
            var rend = blastTransform.GetComponent<ParticleSystemRenderer>();
            
            Debug.Log($"[ExplosionBlast] Found: {ps != null}");
            if (rend != null)
            {
                Debug.Log($"  Material: {(rend.sharedMaterial != null ? rend.sharedMaterial.name : "NULL")}");
                if (rend.sharedMaterial != null)
                {
                    Debug.Log($"  Shader: {(rend.sharedMaterial.shader != null ? rend.sharedMaterial.shader.name : "NULL")}");
                    Debug.Log($"  Texture: {(rend.sharedMaterial.mainTexture != null ? rend.sharedMaterial.mainTexture.name : "NULL")}");
                }
            }
        }
        
        // Check stone particles
        Transform stoneTransform = FindRecursive(pec.transform, "Stone");
        if (stoneTransform != null)
        {
            var ps = stoneTransform.GetComponent<ParticleSystem>();
            var rend = stoneTransform.GetComponent<ParticleSystemRenderer>();
            
            Debug.Log($"[Stone] Found: {ps != null}");
            if (rend != null)
            {
                Debug.Log($"  Material: {(rend.sharedMaterial != null ? rend.sharedMaterial.name : "NULL")}");
                if (rend.sharedMaterial != null)
                {
                    Debug.Log($"  Shader: {(rend.sharedMaterial.shader != null ? rend.sharedMaterial.shader.name : "NULL")}");
                }
            }
        }
        
        Debug.Log("=== END DIAGNOSTIC ===");
    }

    private Transform FindRecursive(Transform parent, string name)
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

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class FixPinkMaterials : MonoBehaviour
{
    [MenuItem("UberStrike/Fix Pink Materials")]
    public static void Fix()
    {
        string[] matGuids = AssetDatabase.FindAssets("t:Material");
        int fixedCount = 0;

        // Load Shaders once
        Shader maskedShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/Masked.shader");
        Shader waterShader = Shader.Find("FX/Water"); 
        Shader alphaBlend = Shader.Find("Particles/Alpha Blended Cull Front");
        Shader additive = Shader.Find("Particles/AdditiveBlend"); // Generic additive
        
        if (maskedShader == null) Debug.LogError("Could not find Masked.shader!");

        foreach (string guid in matGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat != null)
            {
                bool changed = false;
                
                // 1. Fix Minimap (Masked)
                if (mat.name == "Minimap" || mat.name == "MaskedTexture")
                {
                    if (mat.shader.name != maskedShader.name)
                    {
                        mat.shader = maskedShader;
                        changed = true;
                    }
                }
                
                // 2. Fix Water
                if (mat.name.Contains("Water") && (mat.shader == null || mat.shader.name.Contains("Error") || mat.name.Contains("Daylight") || mat.name.Contains("Nighttime")))
                {
                    if (waterShader != null) {
                        mat.shader = waterShader;
                        changed = true;
                    }
                }

                // 3. Fix Smoke/Particles
                if (mat.name.Contains("Smoke") || mat.name.Contains("SpawnParticles"))
                {
                     // Smoke usually needs Alpha Blended
                     // SpawnParticles needs Additive?
                     // Let's safe bet on Particles/Alpha Blended
                     if(mat.shader == null || mat.shader.name.Contains("Error"))
                     {
                         mat.shader = alphaBlend;
                         changed = true;
                     }
                }

                if (changed)
                {
                    EditorUtility.SetDirty(mat);
                    fixedCount++;
                    Debug.Log($"Fixed Material: {mat.name} -> {mat.shader.name}");
                }
            }
        }
        
        // Force save
        AssetDatabase.SaveAssets();
        Debug.Log($"Completed Pink Material Fix. Fixed {fixedCount} materials.");
    }
}

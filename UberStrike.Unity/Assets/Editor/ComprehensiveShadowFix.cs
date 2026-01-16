using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

public class ComprehensiveShadowFix : MonoBehaviour
{
    [MenuItem("UberStrike/★ Fix Shadows (Comprehensive)")]
    public static void FixShadows()
    {
        Debug.Log("=== COMPREHENSIVE SHADOW FIX ===");
        
        // STEP 1: Configure Quality Settings
        Debug.Log("[1/4] Configuring Quality Settings...");
        QualitySettings.shadows = ShadowQuality.All;
        QualitySettings.shadowResolution = ShadowResolution.High;
        QualitySettings.shadowDistance = 150f;
        QualitySettings.shadowCascades = 2;
        QualitySettings.shadowProjection = ShadowProjection.StableFit;
        Debug.Log("  ✓ Shadow quality: All, Distance: 150, Cascades: 2");
        
        // STEP 2: Configure ALL Lights
        Debug.Log("[2/4] Configuring all lights...");
        Light[] lights = FindObjectsOfType<Light>();
        int lightsFixed = 0;
        
        foreach (var light in lights)
        {
            if (light.type == LightType.Directional)
            {
                light.shadows = LightShadows.Soft;
                light.shadowStrength = 1.0f;
                light.shadowBias = 0.05f;
                light.shadowNormalBias = 0.4f;
                light.shadowNearPlane = 0.2f;
                light.cullingMask = -1; // Render all layers
                
                EditorUtility.SetDirty(light);
                Debug.Log($"  ✓ Directional Light: {light.name}");
                lightsFixed++;
            }
            else if (light.type == LightType.Point || light.type == LightType.Spot)
            {
                light.shadows = LightShadows.Soft;
                light.shadowStrength = 0.8f;
                
                EditorUtility.SetDirty(light);
                lightsFixed++;
            }
        }
        
        Debug.Log($"  ✓ Fixed {lightsFixed} lights");
        
        // STEP 3: Enable Shadow Casting on ALL MeshRenderers
        Debug.Log("[3/4] Enabling shadow casting on all mesh renderers...");
        MeshRenderer[] renderers = FindObjectsOfType<MeshRenderer>();
        int renderersFixed = 0;
        
        foreach (var renderer in renderers)
        {
            // Skip UI and particle renderers
            if (renderer.gameObject.layer == 5) continue; // UI layer
            
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
            
            EditorUtility.SetDirty(renderer);
            renderersFixed++;
        }
        
        Debug.Log($"  ✓ Fixed {renderersFixed} mesh renderers");
        
        // STEP 4: Enable Shadow Casting on SkinnedMeshRenderers (characters)
        Debug.Log("[4/4] Enabling shadow casting on skinned mesh renderers...");
        SkinnedMeshRenderer[] skinnedRenderers = FindObjectsOfType<SkinnedMeshRenderer>();
        int skinnedFixed = 0;
        
        foreach (var renderer in skinnedRenderers)
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
            
            EditorUtility.SetDirty(renderer);
            skinnedFixed++;
        }
        
        Debug.Log($"  ✓ Fixed {skinnedFixed} skinned mesh renderers");
        
        // Save changes
        EditorUtility.SetDirty(QualitySettings.GetQualitySettings());
        
        Debug.Log("=== SHADOW FIX COMPLETE ===");
        Debug.Log($"Summary: {lightsFixed} lights, {renderersFixed} meshes, {skinnedFixed} characters");
        Debug.Log("IMPORTANT: Save the scene (Ctrl+S) and play the game to see shadows!");
    }
}

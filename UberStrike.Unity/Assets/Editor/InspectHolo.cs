using UnityEngine;
using UnityEditor;

public class InspectHolo : MonoBehaviour
{
    [MenuItem("UberStrike/Inspect Holo Prefab")]
    public static void Inspect()
    {
        string guids = "62ebc583406ffa641b98de4b5d11fdc7"; // HoloGearItem.cs
        // Actually we want to find the PREFAB that renders it.
        // It's likely dynamically built.
        
        Debug.Log("--- Inspecting Holo Candidates ---");
        var renderers = FindObjectsOfType<Renderer>();
        foreach(var r in renderers)
        {
            // If it looks like a Holo (Materials with 'Holo' or 'Julia' in name?)
            // Or if it is Pink
            if(r.sharedMaterial == null || r.sharedMaterial.name.Contains("Default") || r.sharedMaterial.shader.name.Contains("Error"))
            {
                if(r.name.Contains("Holo") || r.transform.root.name.Contains("Julia"))
                {
                    Debug.Log($"[POSSIBLE HOLO] {r.name} (Root: {r.transform.root.name}) uses Broken Material. Shader: {r.sharedMaterial?.shader?.name}");
                    EditorGUIUtility.PingObject(r.gameObject);
                }
            }
        }
    }
}

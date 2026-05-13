using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

/// <summary>
/// Auto-generates per-map BeastLightmapMapData ScriptableObjects in Assets/Resources/BeastLightmaps/.
/// These SOs hold references to the Beast lightmap EXR textures, allowing BeastLightmapLoader to
/// load them at runtime via Resources.Load (works in both Editor and standalone builds).
///
/// Runs automatically on editor load if any SOs are missing.
/// Manual rebuild: Tools > Rebuild Beast Lightmap Config
/// </summary>
public static class BeastLightmapConfigBuilder
{
    const string OutputFolder = "Assets/Resources/BeastLightmaps";

    [InitializeOnLoadMethod]
    static void AutoBuild()
    {
        // Check if any SO is missing — if so, rebuild all
        bool anyMissing = false;
        foreach (var kvp in BeastLightmapLoader.SceneLightmapFolders)
        {
            string assetPath = OutputFolder + "/" + kvp.Key + ".asset";
            if (!File.Exists(assetPath))
            {
                anyMissing = true;
                break;
            }
        }

        if (anyMissing)
        {
            // Defer to avoid running during import
            EditorApplication.delayCall += BuildAll;
        }
    }

    [MenuItem("Tools/Rebuild Beast Lightmap Config")]
    public static void BuildAll()
    {
        // Ensure output folder exists
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(OutputFolder))
            AssetDatabase.CreateFolder("Assets/Resources", "BeastLightmaps");

        int totalMaps = 0;
        int totalTextures = 0;

        foreach (var kvp in BeastLightmapLoader.SceneLightmapFolders)
        {
            string sceneName = kvp.Key;
            string folder = kvp.Value;

            if (!Directory.Exists(folder))
            {
                Debug.LogWarning($"[BeastLightmapConfigBuilder] Folder not found: {folder} (skipping {sceneName})");
                continue;
            }

            var files = Directory.GetFiles(folder, "LightmapFar-*.exr")
                .Select(f => f.Replace("\\", "/"))
                .OrderBy(f =>
                {
                    string name = Path.GetFileNameWithoutExtension(f);
                    return int.Parse(name.Replace("LightmapFar-", ""));
                })
                .ToArray();

            if (files.Length == 0)
            {
                Debug.LogWarning($"[BeastLightmapConfigBuilder] No LightmapFar-*.exr in {folder} (skipping {sceneName})");
                continue;
            }

            // Load textures via AssetDatabase
            var textures = new Texture2D[files.Length];
            for (int i = 0; i < files.Length; i++)
            {
                textures[i] = AssetDatabase.LoadAssetAtPath<Texture2D>(files[i]);
                if (textures[i] == null)
                    Debug.LogWarning($"[BeastLightmapConfigBuilder] Failed to load: {files[i]}");
            }

            // Create or update the SO
            string assetPath = OutputFolder + "/" + sceneName + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<BeastLightmapMapData>(assetPath);

            if (existing != null)
            {
                existing.lightmaps = textures;
                EditorUtility.SetDirty(existing);
            }
            else
            {
                var so = ScriptableObject.CreateInstance<BeastLightmapMapData>();
                so.lightmaps = textures;
                AssetDatabase.CreateAsset(so, assetPath);
            }

            totalMaps++;
            totalTextures += files.Length;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[BeastLightmapConfigBuilder] Built {totalMaps} map configs with {totalTextures} total lightmap textures in {OutputFolder}/");
    }
}

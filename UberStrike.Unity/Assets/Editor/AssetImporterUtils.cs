using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

internal static class AssetImporterUtils
{
    #region Models

    [MenuItem("Cmune Tools/Asset Importer/M: Disable Mesh compression")]
    static void AssetImporterModelsDisableMeshCompression()
    {
        foreach (string path in GetModelAssetPathOfSelection())
        {
            ModelImporter i = ModelImporter.GetAtPath(path) as ModelImporter;
            if (i.meshCompression != ModelImporterMeshCompression.Off)
            {
                i.meshCompression = ModelImporterMeshCompression.Off;
                Debug.Log("Disabling Meshcompression for asset: " + path);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
        }
    }

    [MenuItem("Cmune Tools/Asset Importer/M: Enable Mesh compression")]
    static void AssetImporterModelsEnableMeshCompression()
    {
        foreach (string path in GetModelAssetPathOfSelection())
        {
            ModelImporter i = ModelImporter.GetAtPath(path) as ModelImporter;
            if (i.meshCompression != ModelImporterMeshCompression.High)
            {
                i.meshCompression = ModelImporterMeshCompression.High;
                Debug.Log("Enabling Meshcompression for asset: " + path);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
        }
    }

    private static IEnumerable<string> GetModelAssetPathOfSelection()
    {
        Object[] selection = Selection.GetFiltered(typeof(MeshFilter), SelectionMode.DeepAssets | SelectionMode.Deep);
        HashSet<string> assetPath = new HashSet<string>();
        foreach (MeshFilter t in selection)
        {
            string p = AssetDatabase.GetAssetPath(t.sharedMesh);
            if (!string.IsNullOrEmpty(p) && p.EndsWith(".fbx"))
            {
                assetPath.Add(p);
            }
        }
        //Debug.Log("Assets to process: " + assetPath.Count + " (of " + selection.Length + " models found)");

        foreach (string path in assetPath)
        {
            yield return path;
        }
    }

    #endregion

    #region Textures

    [MenuItem("Cmune Tools/Asset Importer/T: Select Without Standalone Override")]
    static void noStandaloneOverride()
    {
        Object[] selection = Selection.GetFiltered(typeof(Texture2D), SelectionMode.DeepAssets);
        foreach (Texture2D t in selection)
        {
            TextureImporter ti = TextureImporter.GetAtPath(AssetDatabase.GetAssetPath(t)) as TextureImporter;
            int maxTextureSize = 0;
            TextureImporterFormat textureImporterFormat;
            if (ti.GetPlatformTextureSettings(RuntimePlatform.WindowsPlayer.ToString(), out maxTextureSize, out textureImporterFormat))
                Debug.Log(maxTextureSize.ToString() + " : " + textureImporterFormat.ToString());
            else
                Debug.Log("Failed to get Texture Settings for Standalone");
        }
    }

    [MenuItem("Cmune Tools/Asset Importer/T: Item Icons")]
    static void UpdateTextureImporterItemIcons()
    {
        UpdateTextureImporter(64, 64, TextureImporterFormat.DXT5, false);
    }

    [MenuItem("Cmune Tools/Asset Importer/T: Item Texture - BIG")]
    static void UpdateTextureImporterItemIconsBig()
    {
        UpdateTextureImporter(256, 512, TextureImporterFormat.DXT1, true);
    }

    [MenuItem("Cmune Tools/Asset Importer/T: Item Texture - small")]
    static void UpdateTextureImporterItemIconsSmall()
    {
        UpdateTextureImporter(128, 256, TextureImporterFormat.DXT1, true);
    }

    //[MenuItem("Cmune Tools/Asset Importer/Texture: 256 DXT1 Mipmaps")]
    //static void UpdateTextureImporter256()
    //{
    //    UpdateTextureImporter(256, TextureImporterFormat.DXT1, true);
    //}

    //[MenuItem("Cmune Tools/Asset Importer/Texture: 128 DXT1 Mipmaps")]
    //static void UpdateTextureImporter128()
    //{
    //    UpdateTextureImporter(128, TextureImporterFormat.DXT1, true);
    //}

    //[MenuItem("Cmune Tools/Asset Importer/Texture: 64 DXT5 No Mipmaps")]
    //static void UpdateTextureImporter64Icons()
    //{
    //    UpdateTextureImporter(64, TextureImporterFormat.DXT5, false);
    //}

    static void UpdateTextureImporter(int sdSize, int hdSize, TextureImporterFormat format, bool useMipmaps)
    {
        Object[] all = Selection.GetFiltered(typeof(Texture2D), SelectionMode.DeepAssets);
        foreach (Object o in all)
        {
            if (o is Texture2D)
            {
                string path = AssetDatabase.GetAssetPath(o.GetInstanceID());
                //Debug.Log(string.Format("Import Texture {0}\n{1}", o.name, path));


                TextureImporter i = TextureImporter.GetAtPath(path) as TextureImporter;
                if (i.normalmap)
                {
                    i.textureCompression = TextureImporterCompression.Compressed;
                }
                else
                {
                    // Use compression based on the original format parameter intent
                    i.textureCompression = TextureImporterCompression.Compressed;
                }

                i.textureType = TextureImporterType.Default;
                i.mipmapEnabled = useMipmaps;
                i.mipmapFilter = TextureImporterMipFilter.BoxFilter;
                // textureFormat property replaced with textureCompression
                i.maxTextureSize = hdSize;
                i.anisoLevel = 0;
                if (!useMipmaps) i.npotScale = TextureImporterNPOTScale.None;
                i.ClearPlatformTextureSettings("Standalone");
                i.ClearPlatformTextureSettings("WebGL");
                if (sdSize != hdSize)
                {
                    var platformSettings = i.GetPlatformTextureSettings("WebGL");
                    platformSettings.maxTextureSize = sdSize;
                    platformSettings.compressionQuality = 50;
                    i.SetPlatformTextureSettings(platformSettings);
                }

                AssetDatabase.ImportAsset(path);
            }
        }
    }

    [MenuItem("Cmune Tools/Asset Importer/T: Check Large Textures")]
    static void CheckLargeTextures()
    {
        Texture[] all = (Texture[])Object.FindObjectsOfTypeIncludingAssets(typeof(Texture));

        //List<Texture> textures = new List<Texture>();
        foreach (Texture t in all)
        {
            if (t.width > 255 || t.height > 255)
            {
                Debug.LogWarning(string.Format("Texture {0} is unusually large. ({1}x{2})", AssetDatabase.GetAssetPath(t.GetInstanceID()), t.width.ToString(), t.height.ToString()));
            }
        }
    }

    #endregion

    #region Sound

    [MenuItem("Cmune Tools/Asset Importer/A: Import 2D - 56 kbs")]
    static void SoundImporter2D()
    {
        Object[] all = Selection.GetFiltered(typeof(AudioClip), SelectionMode.DeepAssets);
        foreach (Object o in all)
        {
            if (o is AudioClip)
            {
                string path = AssetDatabase.GetAssetPath(o.GetInstanceID());

                Debug.LogWarning(string.Format("Import Sound {0}\n{1}", o.name, path));

                AudioImporter i = AudioImporter.GetAtPath(path) as AudioImporter;
                var sampleSettings = i.defaultSampleSettings;
                sampleSettings.compressionFormat = AudioCompressionFormat.Vorbis;
                sampleSettings.loadType = AudioClipLoadType.CompressedInMemory;
                sampleSettings.quality = 0.2f; // Equivalent to ~56 kbs
                i.defaultSampleSettings = sampleSettings;
                i.threeD = false;
                i.forceToMono = true;
                //i.stream = false;

                AssetDatabase.ImportAsset(path);
            }
        }
    }

    [MenuItem("Cmune Tools/Asset Importer/A: Import 3D - 56 kbs")]
    static void SoundImporter3D()
    {
        Object[] all = Selection.GetFiltered(typeof(AudioClip), SelectionMode.DeepAssets);
        foreach (Object o in all)
        {
            if (o is AudioClip)
            {
                string path = AssetDatabase.GetAssetPath(o.GetInstanceID());

                Debug.LogWarning(string.Format("Import Sound {0}\n{1}", o.name, path));

                AudioImporter i = AudioImporter.GetAtPath(path) as AudioImporter;
                var sampleSettings = i.defaultSampleSettings;
                sampleSettings.compressionFormat = AudioCompressionFormat.Vorbis;
                sampleSettings.loadType = AudioClipLoadType.DecompressOnLoad;
                sampleSettings.quality = 0.2f; // Equivalent to ~56 kbs
                i.defaultSampleSettings = sampleSettings;
                i.threeD = true;
                i.forceToMono = true;
                //i.stream = false;

                AssetDatabase.ImportAsset(path);
            }
        }
    }

    #endregion
}
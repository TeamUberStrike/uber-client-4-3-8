
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public class AudioTools
{
    private const float KBytes = 1024;

    /// <summary>
    /// Prints out the sheet "AudioMap.csv" containing all information about the selected audio assets
    /// </summary>
    [MenuItem("Cmune/Audio/Update Audio Map")]
    static void UpdateAudioMap()
    {
        var builder = GetAudioMapForSelection(Selection.GetFiltered(typeof(AudioClip), SelectionMode.DeepAssets));

        TextWriter writer = null;
        try
        {
            writer = File.CreateText("AudioMap.csv");
            writer.Write(builder.ToString());
        }
        finally
        {
            if (writer != null)
                writer.Close();
        }
    }

    static StringBuilder GetAudioMapForSelection(Object[] selection)
    {
        StringBuilder builder = new StringBuilder();
        WriteAudioMapHeader(builder);

        var usedAssets = CheckAudioReferencedMyMaps();

        Debug.Log("Update Audio Map");
        foreach (string path in GetPathOfSelectedAssets<AudioClip>(selection))
        {
            AudioImporter i = AudioImporter.GetAtPath(path) as AudioImporter;
            WriteAudioMapInformation(i, builder, usedAssets);
        }

        return builder;
    }

    /// <summary>
    /// Debug out all used audio assets of the selected scene object (or other asset)
    /// </summary>
    //[MenuItem("Cmune/Audio/Check Audio usage for map")]
    static Dictionary<string, HashSet<int>> CheckAudioReferencedMyMaps()
    {
        Debug.Log("CheckAudioForMap");

        Dictionary<string, HashSet<int>> audioUsedByMaps = new Dictionary<string, HashSet<int>>();
        foreach (var m in SceneExporter.MapsToExport)
        {
            string path = "Assets/Scenes/" + m.Value + ".unity";
            Object scene = AssetDatabase.LoadAssetAtPath(path, typeof(Object));
            if (scene != null)
            {
                foreach (var a in GetPathOfSelectedAssets<AudioClip>(EditorUtility.CollectDependencies(new Object[] { scene })))
                {
                    if (!audioUsedByMaps.ContainsKey(a))
                        audioUsedByMaps[a] = new HashSet<int>();

                    audioUsedByMaps[a].Add(m.Key);
                }
            }
            else
            {
                Debug.LogError("CheckReferencedAudio " + m.Value + " not found at: " + path);
            }
        }

        return audioUsedByMaps;

        //var instances = new HashSet<string>(GetPathOfSelectedAssets<AudioClip>(EditorUtility.CollectDependencies(Selection.objects)));
        //foreach (var i in instances)
        //{
        //    Debug.Log(i);
        //}
    }

    static void WriteAudioMapHeader(StringBuilder builder)
    {
        builder.Append("NAME").Append(',');
        builder.Append("RAW SIZE").Append(',');
        builder.Append("COMP SIZE").Append(',');
        builder.Append("COMP %").Append(',');
        builder.Append("BITRATE").Append(',');
        builder.Append("FORMAT").Append(',');
        builder.Append("DIM").Append(',');
        builder.Append("LOAD").Append(',');
        builder.Append("MONO").Append(',');
        builder.Append("LOOP").Append(',');
        builder.AppendLine("MAPS");
    }

    static void WriteAudioMapInformation(AudioImporter file, StringBuilder builder, Dictionary<string, HashSet<int>> sceneUsage)
    {
        long compressedLength = GetCompressedFileSize(file.assetPath);

        FileInfo info = new FileInfo(file.assetPath);
        var sampleSettings = file.defaultSampleSettings;
        builder.Append(info.Name).Append(',');
        builder.Append((info.Length / KBytes).ToString("F1")).Append(',');
        builder.Append((compressedLength / KBytes).ToString("F1")).Append(',');
        builder.Append((1 - (compressedLength / (double)info.Length)).ToString("P0")).Append(',');
        builder.Append((sampleSettings.quality * 100).ToString("F0")).Append(','); // Show quality instead of bitrate
        builder.Append(sampleSettings.compressionFormat == AudioCompressionFormat.Vorbis ? "OGG" : info.Extension).Append(',');
        builder.Append(file.threeD ? "3D" : "2D").Append(',');
        builder.Append(sampleSettings.loadType).Append(',');
        builder.Append(file.forceToMono ? "x" : "").Append(',');
        builder.Append(file.loopable ? "x" : "").Append(',');
        if (sceneUsage.ContainsKey(file.assetPath))
        {
            foreach (var s in sceneUsage[file.assetPath])
                builder.Append(s).Append('|');
        }
        builder.AppendLine();
    }

    private static IEnumerable<string> GetPathOfSelectedAssets<T>(Object[] selection)
    {
        HashSet<string> assetPath = new HashSet<string>();
        foreach (var t in selection)
        {
            if (t.GetType() == typeof(T))
            {
                string p = AssetDatabase.GetAssetPath(t);
                if (!string.IsNullOrEmpty(p))
                {
                    assetPath.Add(p);
                }
            }
        }
        Debug.Log("Assets to process: " + assetPath.Count + " (of " + selection.Length + " assets found)");

        foreach (string path in assetPath)
        {
            yield return path;
        }
    }

    private static long GetCompressedFileSize(string path)
    {
        if (!string.IsNullOrEmpty(path))
        {
            string guid = AssetDatabase.AssetPathToGUID(path);
            string p = Path.GetFullPath(Application.dataPath + "../../Library/cache/" + guid.Substring(0, 2) + "/" + guid);
            if (File.Exists(p))
            {
                return new FileInfo(p).Length;
            }
            else
            {
                Debug.LogWarning("No file found at: " + p);
                return 0;
            }
        }
        else
        {
            Debug.LogWarning("No asset found at: " + path);
            return 0;
        }
    }
}
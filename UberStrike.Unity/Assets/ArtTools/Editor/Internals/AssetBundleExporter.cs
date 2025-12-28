using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace UberStrike.Unity.ArtTools
{
    public static class UberstrikeMapExporter
    {
        public static void ExportMapAssetBundle(string bundleName, BuildTarget target)
        {
            CheckDirectory(ArtAssetDefines.MapAssetBundlePath);

            //EditorUtility.
            UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath(EditorApplication.currentScene, typeof(UnityEngine.Object));
            if (obj != null)
            {
                Debug.Log("Build Map: " + EditorApplication.currentScene);
                string buildResult = BuildPipeline.BuildPlayer(new string[] { EditorApplication.currentScene }, ArtAssetDefines.MapAssetBundlePath + bundleName, target, BuildOptions.BuildAdditionalStreamedScenes);

                if (!string.IsNullOrEmpty(buildResult))
                {
                    Debug.LogError("BuildPlayer: " + buildResult);
                }
                else
                {
                    Debug.Log("Built level at: " + ArtAssetDefines.MapAssetBundlePath + bundleName);
                }
            }
            else
            {
                Debug.LogWarning("BuildPlayer Error: Map not found with name = " + EditorApplication.currentScene);
            }
        }

        public static void ExportMapPackage(string packageName)
        {
            CheckDirectory(ArtAssetDefines.MapPackagePath);

            AssetDatabase.ExportPackage(EditorApplication.currentScene, ArtAssetDefines.MapPackagePath + packageName, ExportPackageOptions.Default | ExportPackageOptions.Interactive | ExportPackageOptions.IncludeDependencies);
        }

        static void CheckDirectory(string directory)
        {
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
    }
}
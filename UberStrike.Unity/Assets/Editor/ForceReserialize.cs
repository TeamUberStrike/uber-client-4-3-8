using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class ForceReserialize : MonoBehaviour
{
    [MenuItem("UberStrike/Force Reserialize All Assets")]
    public static void Reserialize()
    {
        if (!EditorUtility.DisplayDialog("Reserialize Assets", 
            "This will scan the entire project and rewrite all assets to the current Unity version format. This may take a long time (5-10+ minutes). Do you want to proceed?", 
            "Yes, Do it", "Cancel"))
        {
            return;
        }

        Debug.Log("Starting Force Reserialize... finding assets...");
        
        string[] allAssets = AssetDatabase.GetAllAssetPaths(); 
        
        Debug.Log($"Found {allAssets.Length} assets. Reserializing...");
        
        AssetDatabase.ForceReserializeAssets(allAssets, ForceReserializeAssetsOptions.ReserializeAssetsAndMetadata);
        
        Debug.Log("Force Reserialize COMPLETED. You should now see many changed files in Git (lines starting with 'serializedVersion'). This is normal and good!");
    }
}

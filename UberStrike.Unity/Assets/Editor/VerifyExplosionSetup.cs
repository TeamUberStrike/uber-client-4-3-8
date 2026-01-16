using UnityEngine;
using UnityEditor;

public class VerifyExplosionSetup : MonoBehaviour
{
    [MenuItem("UberStrike/Verify Explosion Setup")]
    public static void Verify()
    {
        string matPath = "Assets/Artwork/QuickItems/FPXRobExplosion/Materials/Explosion_rob.psd.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);

        if (mat == null)
        {
             Debug.LogError("Material not found!");
             return;
        }

        Debug.Log($"Material: {mat.name}");
        Debug.Log($"Shader: {mat.shader.name}");
        Debug.Log($"Main Texture: {(mat.mainTexture != null ? mat.mainTexture.name : "NULL")}");
        
        // Check Import Settings of Texture
        if(mat.mainTexture != null)
        {
            string texPath = AssetDatabase.GetAssetPath(mat.mainTexture);
            TextureImporter importer = AssetImporter.GetAtPath(texPath) as TextureImporter;
            if(importer != null)
            {
                Debug.Log($"Texture Type: {importer.textureType}");
                Debug.Log($"Alpha Is Transparency: {importer.alphaIsTransparency}");
            }
        }
    }
}

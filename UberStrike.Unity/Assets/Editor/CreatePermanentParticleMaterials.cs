using UnityEngine;
using UnityEditor;
using System.IO;

public class CreatePermanentParticleMaterials : MonoBehaviour
{
    [MenuItem("UberStrike/Particles/1. Create Permanent Materials")]
    public static void CreateMaterials()
    {
        string path = "Assets/Imported/ParticleMaterials";
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        
        // 1. Explosion Blast (Fireball)
        Texture2D blastTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Imported/ParticleMaterials/Explosion_Fireball_TexS.png");
        if (blastTex != null)
        {
            Material mat = new Material(Shader.Find("Mobile/Particles/Alpha Blended"));
            mat.name = "Permanent_ExplosionBlast";
            mat.mainTexture = blastTex;
            mat.SetColor("_TintColor", new Color(1f, 1f, 1f, 1f)); // White base for full texture color
            
            if (CreateAsset(mat, path + "/Permanent_ExplosionBlast.mat"))
                Debug.Log("Created Permanent_ExplosionBlast.mat");
        }
        
        // 2. Stone Impact (Debris)
        Texture2D stoneTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Imported/ParticleMaterials/TinyStones.png");
        if (stoneTex != null)
        {
            Material mat = new Material(Shader.Find("Mobile/Particles/Alpha Blended"));
            mat.name = "Permanent_StoneChips";
            mat.mainTexture = stoneTex;
            
            if (CreateAsset(mat, path + "/Permanent_StoneChips.mat"))
                Debug.Log("Created Permanent_StoneChips.mat");
        }
        
        AssetDatabase.Refresh();
    }
    
    private static bool CreateAsset(Material mat, string path)
    {
        // Delete if exists to ensure clean state
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(mat, path);
        return true;
    }
}

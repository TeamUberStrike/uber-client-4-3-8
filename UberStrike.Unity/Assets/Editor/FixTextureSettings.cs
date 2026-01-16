using UnityEngine;
using UnityEditor;

public class FixTextureSettings : MonoBehaviour
{
    [MenuItem("UberStrike/Fix Texture Import Settings (Alpha)")]
    public static void Fix()
    {
        string[] texPaths = new string[] {
            "Assets/Imported/ParticleMaterials/Explosion_Fireball_TexS.png",
            "Assets/Imported/ParticleMaterials/ParticleWood.png",
            "Assets/Imported/ParticleMaterials/WoodBulletHoleAlbedo.tif",
            "Assets/Imported/ParticleMaterials/TinyStones.png"
        };

        foreach (string path in texPaths)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                bool changed = false;
                if (!importer.alphaIsTransparency)
                {
                    importer.alphaIsTransparency = true;
                    changed = true;
                }
                
                // Ensure default type (not Sprite usually, but Default is fine for Particles)
                if(importer.textureType != TextureImporterType.Default)
                {
                    importer.textureType = TextureImporterType.Default;
                    changed = true;
                }

                if (changed)
                {
                    importer.SaveAndReimport();
                    Debug.Log($"[FIXED] Updated settings for {path}");
                }
                else
                {
                    Debug.Log($"[OK] Settings already correct for {path}");
                }
            }
            else
            {
                Debug.LogWarning($"Could not find texture at {path}");
            }
        }
    }
}

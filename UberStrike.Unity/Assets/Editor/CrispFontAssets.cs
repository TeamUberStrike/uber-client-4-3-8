using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Creates the font "slots" CrispMenuFonts fills at runtime: one custom font per menu font (the
/// Interpark TTFs imported at a fixed size), with the same metrics (line spacing, ascent, size) but no
/// texture of its own, so it renders with the material it is given. TTF-imported fonts always render
/// with their built-in texture, which is why they can't be used for this directly.
/// Run once after adding or changing a menu font: Tools > UberStrike > Create Crisp Font Slots
/// (or -executeMethod CrispFontAssets.Create).
/// </summary>
public static class CrispFontAssets
{
    private const string SourceFolder = "Assets/Artwork/Fonts/Interpark";
    private const string SlotFolder = "Assets/Artwork/GUI/Resources/CrispFonts/Slots";

    [MenuItem("Tools/UberStrike/Create Crisp Font Slots")]
    public static void Create()
    {
        Directory.CreateDirectory(SlotFolder);
        foreach (string guid in AssetDatabase.FindAssets("t:Font", new[] { SourceFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Font source = AssetDatabase.LoadAssetAtPath<Font>(path);
            if (source == null || source.dynamic) continue;

            string slotPath = SlotFolder + "/" + source.name + ".fontsettings";
            Font slot = AssetDatabase.LoadAssetAtPath<Font>(slotPath);
            if (slot == null)
            {
                slot = new Font(source.name);
                AssetDatabase.CreateAsset(slot, slotPath);
            }

            EditorUtility.CopySerialized(source, slot);
            slot.name = source.name;
            SerializedObject so = new SerializedObject(slot);
            so.FindProperty("m_Texture").objectReferenceValue = null;
            so.FindProperty("m_DefaultMaterial").objectReferenceValue = null;
            SerializedProperty data = so.FindProperty("m_FontData");
            if (data != null) data.arraySize = 0;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(slot);
            Debug.Log("CrispFontAssets: " + slotPath + " size " + source.fontSize + " lineHeight " + source.lineHeight + " ascent " + source.ascent);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}

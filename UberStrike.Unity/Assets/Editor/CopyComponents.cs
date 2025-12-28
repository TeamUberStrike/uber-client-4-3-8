using UnityEditor;
using UnityEngine;
using Cmune.Realtime.Common;
using System.Reflection;
using Cmune.Util;

/// <summary>
/// # shift
/// % ctrl
/// & alt
/// </summary>
public static class CopyComponents
{
    private static Component[] _monos = new Component[0];

    [MenuItem("Cmune Tools/Copy + Paste/Component: Copy %#[")]
    static void attachCopyComponent()
    {
        foreach (GameObject go in Selection.gameObjects)
        {
            _monos = go.GetComponents<Component>();
        }
    }

    [MenuItem("Cmune Tools/Copy + Paste/Component: Paste %#]")]
    static void attachPasteComponent()
    {
        if (Selection.activeGameObject != null)
        {
            foreach (Component m in _monos)
            {
                if (m is Transform) continue;

                System.Type type = m.GetType();

                Component c = Selection.activeGameObject.GetComponent(type);

                if (!c) c = Selection.activeGameObject.AddComponent(type);

                if (c)
                    CopyValuesOfComponent(m, c);
                else
                    Debug.Log("Failed to AddComponent: " + m.GetType().ToString());
            }
        }
    }

    private static BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    static void CopyValuesOfComponent(Component a, Component b)
    {
        FieldInfo[] infoA = a.GetType().GetFields(Flags);
        FieldInfo[] infoB = b.GetType().GetFields(Flags);

        CmuneDebug.Assert(infoA.Length == infoB.Length, " Reflected Field Array Length not the same in CopyValuesOfComponent");

        for (int i = 0; i < infoA.Length; i++)
        {
            CmuneDebug.Assert(infoA[i].FieldType == infoB[i].FieldType, "Field Types not the same!");

            infoB[i].SetValue(b, infoA[i].GetValue(a));
        }
    }
}
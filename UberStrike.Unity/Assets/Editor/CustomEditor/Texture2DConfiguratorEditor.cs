using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using System;

[CustomEditor(typeof(Texture2DConfigurator))]
public class Texture2DConfiguratorEditor : Editor
{
    SerializedObject serObj;
    SerializedProperty textures;

    public static readonly string Space = "    ";

    private void OnEnable()
    {
        serObj = new SerializedObject(target);
        textures = serObj.FindProperty("Textures");
    }

    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("Update", GUILayout.Height(30)))
        {
            UpdateTexture2DManager();
            serObj.ApplyModifiedProperties();
        }

        GUILayout.Space(10);

        if (textures.arraySize == 0)
        {
            if (GUILayout.Button("+", GUILayout.Width(30)))
            {
                textures.arraySize = 1;
                serObj.ApplyModifiedProperties();
            }
        }

        for (int i = 0; i < textures.arraySize; i++)
        {
            var t = textures.GetArrayElementAtIndex(i);

            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("-", GUILayout.Width(30)))
                {
                    textures.DeleteArrayElementAtIndex(i);
                    serObj.ApplyModifiedProperties();
                    continue;
                }
                if (GUILayout.Button("+", GUILayout.Width(30)))
                {
                    textures.InsertArrayElementAtIndex(i);
                    var newItem = textures.GetArrayElementAtIndex(i + 1);
                    newItem.objectReferenceValue = null;
                    serObj.ApplyModifiedProperties();
                }
                var item = EditorGUILayout.ObjectField(t.objectReferenceValue != null ? t.objectReferenceValue.name : "<not set>", t.objectReferenceValue, typeof(Texture2D), false);
                if (item != t.objectReferenceValue)
                {
                    t.objectReferenceValue = item;
                    serObj.ApplyModifiedProperties();
                }
            }
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Update", GUILayout.Height(30)))
        {
            UpdateTexture2DManager();
        }
    }

    private void UpdateTexture2DManager()
    {
        var configurator = target as Texture2DConfigurator;

        if (configurator != null)
        {
            HashSet<string> names = new HashSet<string>();

            Debug.Log("CREATE FILE Assets/Scripts/GUI/" + target.name + ".cs");

            //CREATE CLASS
            using (StreamWriter writer = new StreamWriter("Assets/Scripts/GUI/" + target.name + ".cs", false))
            {
                WriteClassHeader(writer);

                writer.WriteLine(Space + "static " + target.name + "()");
                writer.WriteLine(Space + "{");
                writer.WriteLine(Space + Space + "var configurator = ((GameObject)Resources.Load(\"" + target.name + "\", typeof(GameObject))).GetComponent<Texture2DConfigurator>();");
                for (int i = 0; i < configurator.Textures.Count; i++)
                {
                    if (configurator.Textures[i] != null)
                    {
                        string name = CreateFieldName(configurator.Textures[i].name);
                        writer.WriteLine(Space + Space + name + " = configurator.Textures[" + i + "];");
                    }
                }
                //writer.WriteLine(Space + Space + "Resources.UnloadUnusedAssets();");
                writer.WriteLine(Space + "}");

                writer.WriteLine();
                foreach (var t in configurator.Textures)
                {
                    if (t != null)
                    {
                        string name = CreateFieldName(t.name);
                        if (names.Contains(name))
                        {
                            Debug.LogError("Duplicate entry found: " + name);
                        }
                        else
                        {
                            names.Add(name);
                            writer.WriteLine(Space + "public static Texture2D {0} {{ get; private set; }}", name);
                        }
                    }
                }

                //class End
                writer.WriteLine("}");

                writer.Flush();
                writer.Close();
            }
        }
        else
        {
            Debug.LogWarning("this didn't work: " + target.GetType());
        }
    }

    private static string CreateFieldName(string name)
    {
        var array = new List<char>(name.ToCharArray());

        //remove trailing numbers
        while (System.Char.IsNumber(array[0]))
        {
            array.RemoveAt(0);
        }

        bool pascalize = true;
        for (int i = 0; i < array.Count; )
        {
            if (pascalize && Char.IsLower(array[i]))
                array[i] = Char.ToUpper(array[i]);

            if (array[i] == ' ' || array[i] == '-' || array[i] == '_')
            {
                array.RemoveAt(i);
                pascalize = true;
            }
            else
            {
                i++;
                pascalize = false;
            }
        }

        return new string(array.ToArray());
    }

    private void WriteClassHeader(StreamWriter writer)
    {
        writer.WriteLine("/////////////////////////////////////////////////");
        writer.WriteLine("/// This file was autogenerated by a Class Creator");
        writer.WriteLine("///");
        writer.WriteLine("/// WARNING! All changes in this file will be lost!");
        writer.WriteLine("/////////////////////////////////////////////////");
        writer.WriteLine();

        //usings
        writer.WriteLine("using UnityEngine;");
        writer.WriteLine();

        //class begin
        writer.WriteLine("public static class " + target.name);
        writer.WriteLine("{");
    }
}
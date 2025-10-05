using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using System;
using System.Xml;

[CustomEditor(typeof(StringConfigurator))]
public class StringConfiguratorEditor : Editor
{
    SerializedObject serObj;
    SerializedProperty textAssets;

    public static readonly string Space = "    ";

    private void OnEnable()
    {
        serObj = new SerializedObject(target);
        textAssets = serObj.FindProperty("TextAssets");
    }

    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("Update", GUILayout.Height(30)))
        {
            UpdateAudioManager();
            serObj.ApplyModifiedProperties();
        }

        GUILayout.Space(10);

        if (textAssets.arraySize == 0)
        {
            if (GUILayout.Button("+", GUILayout.Width(30)))
            {
                textAssets.arraySize = 1;
                serObj.ApplyModifiedProperties();
            }
        }

        for (int i = 0; i < textAssets.arraySize; i++)
        {
            var t = textAssets.GetArrayElementAtIndex(i);

            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("-", GUILayout.Width(30)))
                {
                    textAssets.DeleteArrayElementAtIndex(i);
                    serObj.ApplyModifiedProperties();
                    continue;
                }
                if (GUILayout.Button("+", GUILayout.Width(30)))
                {
                    textAssets.InsertArrayElementAtIndex(i);
                    var newItem = textAssets.GetArrayElementAtIndex(i + 1);
                    newItem.objectReferenceValue = null;
                    serObj.ApplyModifiedProperties();
                }
                var item = EditorGUILayout.ObjectField(t.objectReferenceValue != null ? t.objectReferenceValue.name : "<not set>", t.objectReferenceValue, typeof(TextAsset), false);
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
            UpdateAudioManager();
        }
    }

    private void UpdateAudioManager()
    {
        var configurator = target as StringConfigurator;

        if (configurator != null)
        {
            foreach (var asset in configurator.TextAssets)
            {
                if (asset != null)
                {
                    var className = CreateClassName(asset.name);

                    Debug.Log("CREATE FILE Assets/Scripts/Text/" + className + ".cs");

                    XmlReaderSettings settings = new XmlReaderSettings();
                    settings.IgnoreComments = true;
                    settings.IgnoreWhitespace = true;
                    XmlReader reader = XmlReader.Create(new StringReader(asset.text), settings);
                    var fields = GetAllStrings(reader);

                    //CREATE CLASS
                    using (StreamWriter writer = new StreamWriter("Assets/Scripts/Text/" + className + ".cs", false))
                    {
                        WriteClassHeader(writer, className);

                        foreach (var s in fields)
                        {
                            writer.WriteLine(Space + "public static string " + s.Key + " = @\"" + s.Value.Replace("\"", "\"\"").Trim() + "\";");
                        }

                        //class End
                        writer.Write("}");

                        writer.Flush();
                        writer.Close();
                    }
                }

                CreateVisualStudioProject.UpdateProject();
            }
        }
        else
        {
            Debug.LogWarning("this didn't work: " + target.GetType());
        }
    }

    Dictionary<string, string> GetAllStrings(XmlReader reader)
    {
        var fields = new Dictionary<string, string>();

        if (reader != null)
        {
            try
            {
                // Read all localized strings from the Resx XML
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.Name.Equals("data"))
                    {
                        string name = reader.GetAttribute("name");
                        string value = string.Empty;
                        if (reader.Read() && reader.Read())
                            value = reader.ReadString();

                        fields[name] = value;
                    }
                }
            }
            finally
            {
                reader.Close();
            }
        }

        return fields;
    }

    private static string CreateClassName(string name)
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

            if (array[i] == ' ' || array[i] == '-' || array[i] == '_' || array[i] == '.')
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

    private void WriteClassHeader(StreamWriter writer, string className)
    {
        writer.WriteLine("/////////////////////////////////////////////////");
        writer.WriteLine("/// This file was autogenerated by a Class Creator");
        writer.WriteLine("///");
        writer.WriteLine("/// WARNING! All changes in this file will be lost!");
        writer.WriteLine("/////////////////////////////////////////////////");
        writer.WriteLine();

        //usings
        writer.WriteLine();

        //class begin
        writer.WriteLine("public static class " + className);
        writer.WriteLine("{");
    }
}
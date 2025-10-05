using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using System;

[CustomEditor(typeof(AudioConfigurator))]
public class AudioConfiguratorEditor : Editor
{
    SerializedObject serObj;
    SerializedProperty clips;

    public static readonly string Space = "    ";

    private void OnEnable()
    {
        serObj = new SerializedObject(target);
        clips = serObj.FindProperty("Clips");
    }

    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("Update", GUILayout.Height(30)))
        {
            UpdateAudioManager();
            serObj.ApplyModifiedProperties();
        }

        GUILayout.Space(10);

        if (clips.arraySize == 0)
        {
            if (GUILayout.Button("+", GUILayout.Width(30)))
            {
                clips.arraySize = 1;
                serObj.ApplyModifiedProperties();
            }
        }

        for (int i = 0; i < clips.arraySize; i++)
        {
            var t = clips.GetArrayElementAtIndex(i);

            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("-", GUILayout.Width(30)))
                {
                    clips.DeleteArrayElementAtIndex(i);
                    serObj.ApplyModifiedProperties();
                    continue;
                }
                if (GUILayout.Button("+", GUILayout.Width(30)))
                {
                    clips.InsertArrayElementAtIndex(i);
                    var newItem = clips.GetArrayElementAtIndex(i + 1);
                    newItem.objectReferenceValue = null;
                    serObj.ApplyModifiedProperties();
                }
                var item = EditorGUILayout.ObjectField(t.objectReferenceValue != null ? t.objectReferenceValue.name : "<not set>", t.objectReferenceValue, typeof(AudioClip), false);
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
        var configurator = target as AudioConfigurator;

        if (configurator != null)
        {
            Debug.Log("CREATE FILE Assets/Scripts/Audio/" + target.name + ".cs");

            //CREATE CLASS
            using (StreamWriter writer = new StreamWriter("Assets/Scripts/Audio/" + target.name + ".cs", false))
            {
                WriteClassHeader(writer);

                writer.WriteLine(Space + "static " + target.name + "()");
                writer.WriteLine(Space + "{");
                writer.WriteLine(Space + Space + "var configurator = ((GameObject)Resources.Load(\"" + target.name + "\", typeof(GameObject))).GetComponent<AudioConfigurator>();");
                writer.WriteLine(Space + Space + "_allClips = new Dictionary<Clips, AudioClip>(configurator.Clips.Count);");
                writer.WriteLine();

                for (int i = 0; i < configurator.Clips.Count; i++)
                {
                    if (configurator.Clips[i] != null)
                    {
                        string name = CreateFieldName(configurator.Clips[i].name);
                        writer.WriteLine(Space + Space + "_allClips[Clips." + name + "] = configurator.Clips[" + i + "];");
                    }
                }
                writer.WriteLine(Space + "}");
                writer.WriteLine();

                writer.WriteLine(Space + "public static AudioClip Get(Clips clip)");
                writer.WriteLine(Space + "{");
                writer.WriteLine(Space + Space + "return _allClips[clip];");
                writer.WriteLine(Space + "}");
                writer.WriteLine();

                writer.WriteLine(Space + "public enum Clips");
                writer.WriteLine(Space + "{");
                for (int i = 0; i < configurator.Clips.Count; i++)
                {
                    if (configurator.Clips[i] != null)
                    {
                        string name = CreateFieldName(configurator.Clips[i].name);
                        writer.WriteLine(Space + Space + name + ",");
                    }
                }
                writer.WriteLine(Space + "}");

                //class End
                writer.Write("}");

                writer.Flush();
                writer.Close();

                CreateVisualStudioProject.UpdateProject();
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
        writer.WriteLine("using System.Collections.Generic;");
        writer.WriteLine();

        //class begin
        writer.WriteLine("public static class " + target.name);
        writer.WriteLine("{");
        writer.WriteLine(Space + "private static Dictionary<Clips, AudioClip> _allClips;");
        writer.WriteLine();
    }
}
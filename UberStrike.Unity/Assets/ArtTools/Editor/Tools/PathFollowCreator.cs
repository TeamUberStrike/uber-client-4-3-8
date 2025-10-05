using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UberStrike.Unity.ArtTools;

public class PathFollowCreator : EditorWindow
{
    [MenuItem("Cmune/Audio/Path Follow")]
    static void Init()
    {
        EditorWindow.GetWindow<PathFollowCreator>();
    }

    Object clip;
    Object prefab;
    float speed = 1f;
    List<GameObject> instances = new List<GameObject>();

    public void OnGUI()
    {
        GUILayout.Label("Instantiate Objects", EditorStyles.boldLabel);

        clip = EditorGUILayout.ObjectField("Animation", clip, typeof(AnimationClip), false);
        prefab = EditorGUILayout.ObjectField("Prefab", prefab, typeof(GameObject), true);
        speed = EditorGUILayout.Slider("Speed", speed, 0.1f, 5);

        if (clip == null) GUILayout.Label("No Animation Clip assigned", CmuneEditorStyles.RedLabel);
        if (prefab == null) GUILayout.Label("No Prefab Clip assigned", CmuneEditorStyles.RedLabel);
        GUILayout.Space(20);

        GUI.enabled = Application.isPlaying && clip != null && prefab != null;
        if (GUILayout.Button("Create"))
        {
            GameObject instance = GameObject.Instantiate(prefab) as GameObject;
            Animation a = instance.AddComponent<Animation>();
            a.AddClip(clip as AnimationClip, "clip");
            a["clip"].speed = speed;
            a.Play("clip");

            instances.Add(instance);
        }
        GUILayout.Space(20);
        if (Application.isPlaying)
        {
            if (GUILayout.Button("Delete All"))
            {
                foreach (var i in instances)
                {
                    if (i != null)
                    {
                        GameObject.Destroy(i);
                    }
                }
            }
        }
        else
        {
            GUILayout.Label("Press 'Play' to instantiate objects");
        }
    }
}
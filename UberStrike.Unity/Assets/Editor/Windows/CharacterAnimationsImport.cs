using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class CharacterAnimationsImport : EditorWindow
{
    [MenuItem("Cmune Tools/Character/Setup Animation in Prefab")]
    static void Init()
    {
        Debug.LogError("ImportAnimationsInPrefab");
        EditorWindow.GetWindow(typeof(CharacterAnimationsImport), true, "CharacterAnimationsImport", true);
    }

    public CharacterAnimationsImport()
    {
        Debug.LogWarning("CharacterAnimationsImport");
    }

    GameObject fbx = null;
    List<GameObject> prefabs = new List<GameObject>();

    void OnGUI()
    {
        fbx = EditorGUILayout.ObjectField("FBX", fbx, typeof(GameObject), false, null) as GameObject;

        if (prefabs.Count == 0) prefabs.Add(null);

        GameObject[] all = prefabs.ToArray();
        for (int i = 0; i < all.Length; i++)
        {
            EditorGUILayout.BeginHorizontal();
            {
                if (i != 0 && GUILayout.Button("-", GUILayout.Width(25)))
                    prefabs.RemoveAt(i);
                else
                    GUILayout.Space(25);
                prefabs[i] = EditorGUILayout.ObjectField("Prefab " + (i + 1), all[i], typeof(GameObject), false) as GameObject;
                if (i == all.Length - 1 && GUILayout.Button("+", GUILayout.Width(25)))
                    prefabs.Add(null);
                else
                    GUILayout.Space(25);
            }
            EditorGUILayout.EndHorizontal();
        }

        if (GUILayout.Button("Import"))
        {
            ImportAnimations();
        }
    }

    void ImportAnimations()
    {
        if (fbx != null)
        {
            if (PrefabUtility.GetPrefabType(fbx) == PrefabType.ModelPrefab)
            {
                Animation animation = fbx.GetComponent<Animation>();

                if (animation)
                {
                    List<AnimationClip> sourceClips = new List<AnimationClip>();
                    foreach (AnimationState c in animation)
                        sourceClips.Add(animation.GetClip(c.name));

                    foreach (var p in prefabs)
                    {
                        if (p != null)
                            ImportAnimations(sourceClips, p.GetComponent<Animation>());
                    }
                }
                else
                {
                    Debug.LogError("The selected object does not have an Animation component attached to it");
                }
            }
            else
            {
                Debug.LogError("The selected object is not a prefab");
            }
        }
    }

    void ImportAnimations(List<AnimationClip> sourceClips, Animation animation)
    {
        if (animation)
        {
            ClearAnimationStateArray(ref animation);

            //import clips from fbx
            foreach (AnimationClip c in sourceClips)
                animation.AddClip(c, c.name);

            EditorUtility.SetDirty(animation);
        }
        else
        {
            Debug.LogError("The selected prefab does not have an Animation component attached to it");
        }
    }

    void ClearAnimationStateArray(ref Animation animation)
    {
        if (animation)
        {
            WrapMode savedWrapMode = animation.wrapMode;
            bool savedPlayAutomaticallyState = animation.playAutomatically;
            bool savedAnimatePhysicsState = animation.animatePhysics;
#if UNITY_3_0
            bool savedAnimateOnlyIfVisibleState = animation.animateOnlyIfVisible;
#endif
            GameObject gameObject = animation.gameObject;

            //destroy the old
            DestroyImmediate(animation, true);
            //create the new
            animation = gameObject.AddComponent<Animation>();
            animation.wrapMode = savedWrapMode;
            animation.playAutomatically = savedPlayAutomaticallyState;
            animation.animatePhysics = savedAnimatePhysicsState;
#if UNITY_3_0
            animation.animateOnlyIfVisible = savedAnimateOnlyIfVisibleState;
#endif
        }
    }
}

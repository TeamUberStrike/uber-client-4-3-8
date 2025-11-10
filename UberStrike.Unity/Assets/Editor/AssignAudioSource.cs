using UnityEditor;
using UnityEngine;

public class AssignAudioSource : MonoBehaviour
{
    [MenuItem("Cmune Tools/Batch Routines/Audio: Add Source to ForceFields")]
    public static void PopulateItemList()
    {
        if (!Selection.activeGameObject)
        {
            Debug.LogError("Please select a GameObject!");
            return;
        }

        ForceField[] fields = Selection.activeGameObject.GetComponentsInChildren<ForceField>();
        foreach (var f in fields)
        {
            AudioSource audio = f.GetComponent<AudioSource>();
            if (!audio)
            {
                //DestroyImmediate(source);
                AudioSource source = f.gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
            }
        }
    }
}

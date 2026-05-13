using UnityEngine;
using UnityEngine.SceneManagement;

public static class ManagersBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void MarkPersistentRoots()
    {
        MarkDontDestroyOnLoad("Managers");
        MarkDontDestroyOnLoad("GUICamera");
        MarkDontDestroyOnLoad("ActiveLevelCamera");
    }

    private static void MarkDontDestroyOnLoad(string rootName)
    {
        GameObject go = GameObject.Find(rootName);
        if (go == null)
        {
            Debug.LogWarning("[ManagersBootstrap] Root not found: " + rootName);
            return;
        }

        if (go.scene.name == "DontDestroyOnLoad")
        {
            return;
        }

        if (go.transform.parent != null)
        {
            Debug.LogWarning("[ManagersBootstrap] '" + rootName + "' is not scene-root; DontDestroyOnLoad requires root.");
            return;
        }

        Object.DontDestroyOnLoad(go);
        Debug.Log("[ManagersBootstrap] DontDestroyOnLoad: " + rootName);
    }
}

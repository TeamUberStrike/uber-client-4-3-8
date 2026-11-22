using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

public static class WebGLAudioUnlock
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void UberWebAudioSetup();
#endif

#if UNITY_WEBGL && !UNITY_EDITOR
    // Clips import with preloadAudioData off; WebGL decodes async, so first play would be silent.
    private static void PreloadClips(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        foreach (AudioClip clip in Resources.FindObjectsOfTypeAll<AudioClip>())
            if (clip.loadState == AudioDataLoadState.Unloaded) clip.LoadAudioData();
    }
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        UberWebAudioSetup();
        PreloadClips(default(UnityEngine.SceneManagement.Scene), UnityEngine.SceneManagement.LoadSceneMode.Single);
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += PreloadClips;
#endif
    }
}

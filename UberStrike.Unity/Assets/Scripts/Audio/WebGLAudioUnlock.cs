using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System;
using System.Runtime.InteropServices;
using AOT;
#endif

public static class WebGLAudioUnlock
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void UberWebAudioSetup();

    [DllImport("__Internal")]
    private static extern int UberAudioDebugEnabled();

    [DllImport("__Internal")]
    private static extern void UberAudioDebugInit(Action playGameSound);

    [DllImport("__Internal")]
    private static extern void UberAudioDebugSetUnity(string text);

    [MonoPInvokeCallback(typeof(Action))]
    private static void PlayGameSound()
    {
        SfxManager.Play2dAudioClip(SoundEffectType.UIEndOfRound);
    }

    // ?audiodebug=1 only
    private class DebugReporter : MonoBehaviour
    {
        private void Start()
        {
            UberAudioDebugInit(PlayGameSound);
            InvokeRepeating("Report", 0.5f, 0.5f);
        }

        private void Report()
        {
            int playing = 0;
            foreach (AudioSource s in FindObjectsOfType<AudioSource>())
                if (s.isPlaying) playing++;

            string text = "listener vol=" + AudioListener.volume.ToString("0.00") + " pause=" + AudioListener.pause +
                " out=" + AudioSettings.outputSampleRate + "Hz playing=" + playing;

            if (ApplicationDataManager.Exists && ApplicationDataManager.ApplicationOptions != null)
            {
                ApplicationOptions o = ApplicationDataManager.ApplicationOptions;
                text += "\n  enabled=" + o.AudioEnabled + " master=" + o.AudioMasterVolume.ToString("0.00") +
                    " fx=" + o.AudioEffectsVolume.ToString("0.00") + " music=" + o.AudioMusicVolume.ToString("0.00");
            }

            if (SfxManager.Exists)
            {
                AudioClip clip = SfxManager.GetAudioClip(SoundEffectType.UIEndOfRound);
                if (clip != null) text += "\n  testclip " + clip.loadType + " " + clip.loadState;
            }

            UberAudioDebugSetUnity(text);
        }
    }
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

        if (UberAudioDebugEnabled() == 1)
        {
            GameObject go = new GameObject("WebGLAudioDebug");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<DebugReporter>();
        }
#endif
    }
}

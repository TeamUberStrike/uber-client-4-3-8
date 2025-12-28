using UnityEngine;

public class DebugAudio : IDebugPage
{
    public DebugAudio(AudioSource audio)
    {
        this.audio = audio;
    }

    public string Title
    {
        get { return "Audio"; }
    }

    private Vector2 scroll;
    private AudioSource audio;

    // Use this for initialization
    public void Draw()
    {
        GUILayout.Label("AudioListener.volume " + AudioListener.volume);
        GUILayout.Label("AudioListener.pause " + AudioListener.pause);
        if (GUILayout.Button("audio.bypassEffects " + audio.bypassEffects))
        {
            audio.bypassEffects = !audio.bypassEffects;
        }

        scroll = GUILayout.BeginScrollView(scroll);
        foreach (var s in AudioLoader.Instance.AllClips)
        {
            if (s.Value != null)
            {
                if (GUILayout.Button(s.Key))
                {
                    audio.PlayOneShot(s.Value, 2);
                }
            }
            else
            {
                GUILayout.Label(s.Key + " not loaded");
            }
        }

        GUILayout.Space(30);

        foreach (var s in SfxManager.Instance.AllSounds)
        {
            if (s.Audio != null)
            {
                if (GUILayout.Button(s.ID.ToString()))
                {
                    audio.PlayOneShot(s.Audio, 2);
                }
            }
            else
            {
                GUILayout.Label(s.ID.ToString() + " not loaded");
            }
        }
        GUILayout.EndScrollView();
    }
}
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AudioLoader : Singleton<AudioLoader>
{
    private Dictionary<string, AudioClip> cachedAudioClips;

    #region Private Methods

    private AudioLoader()
    {
        cachedAudioClips = new Dictionary<string, AudioClip>();
    }

    public IEnumerable<KeyValuePair<string, AudioClip>> AllClips { get { return cachedAudioClips; } }

    // Old method which is simple but buggy
    private void LoadAudioClipFromWWW(string name)
    {
        WWW www = new WWW(ApplicationDataManager.BaseAudioURL + name);

        cachedAudioClips[name] = www.GetAudioClip(false, true);

        MonoRoutine.Start(DownloadAudioClip(www, name));
    }

    private void CreateStreamedAudioClip(string name)
    {
        StreamedAudioClip clip = new StreamedAudioClip(name);
        cachedAudioClips[name] = clip.Clip;
    }

    private IEnumerator DownloadAudioClip(WWW www, string name)
    {
        yield return www;

        if (!string.IsNullOrEmpty(www.error))
        {
            Debug.LogError(www.error +"\n" + www.url);
            cachedAudioClips.Remove(name);
        }
    }

    #endregion

    #region Public Methods

    public AudioClip Get(string name)
    {
        //Debug.Log("Getting AudioClip: " + name);
#if !UNITY_ANDROID && !UNITY_IPHONE
        if (!cachedAudioClips.ContainsKey(name))
        {
            LoadAudioClipFromWWW(name);
            //CreateStreamedAudioClip(name);
        }

        return cachedAudioClips[name];
#else
        Debug.LogWarning("Skipping streaming of Ogg file. Not supported on mobile.");
        return null;
#endif
    }

    #endregion
}

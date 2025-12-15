#if !UNITY_ANDROID && !UNITY_IPHONE
using UnityEngine;
using UnityEngine.Video;
using System.Collections.Generic;

public class MovieLoader : Singleton<MovieLoader>
{
    private Dictionary<string, VideoClip> cachedMovieTextures;

    private MovieLoader()
    {
        cachedMovieTextures = new Dictionary<string, VideoClip>();
    }

    public IEnumerable<KeyValuePair<string, VideoClip>> AllMovies 
    { 
        get { return cachedMovieTextures; } 
    }

    public VideoClip Get(string name)
    {
        if (!cachedMovieTextures.ContainsKey(name))
        {
            VideoClip clip = Resources.Load<VideoClip>(name);
            cachedMovieTextures[name] = clip;
            
            if (clip == null)
            {
                Debug.LogWarning($"VideoClip '{name}' not found in Resources");
            }
        }
        
        return cachedMovieTextures[name];
    }
}
#endif
#if !UNITY_ANDROID && !UNITY_IPHONE
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MovieLoader : Singleton<MovieLoader>
{
    private Dictionary<string, MovieTexture> cachedMovieTextures;

    #region Private Methods

    private MovieLoader()
    {
        cachedMovieTextures = new Dictionary<string, MovieTexture>();
    }

    public IEnumerable<KeyValuePair<string, MovieTexture>> AllMovies { get { return cachedMovieTextures; } }

    private IEnumerator DownloadMovie(WWW www)
    {
        yield return www;

        if (!string.IsNullOrEmpty(www.error))
        {
            Debug.LogError("failed to load the movie");
            Debug.LogError(www.error);
        }
    }

    #endregion

    #region Public Methods

    public MovieTexture Get(string name)
    {
        if (!cachedMovieTextures.ContainsKey(name))
        {
            //WWW www = new WWW(ApplicationDataManager.BaseMovieURL + name);
            string absName = Application.absoluteURL.Replace(ApplicationDataManager.HeaderFilename + ".unity3d", name);
            Debug.LogError("absolute url = " + Application.absoluteURL);
            Debug.LogError("abs file name = " + absName);
            WWW www = new WWW(absName);
            Debug.LogError(www.movie.duration);
            cachedMovieTextures[name] = www.movie;
            MonoRoutine.Start(DownloadMovie(www));
        }
        return cachedMovieTextures[name];
    }

    #endregion
}
#endif
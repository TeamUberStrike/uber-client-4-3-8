#if !UNITY_ANDROID && !UNITY_IPHONE
using System.Collections;
using UnityEngine;

public class StreamedMoviePlayer : MonoBehaviour
{
    public static StreamedMoviePlayer Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    public void PlayMovie(MovieTexture movie, bool loop = false)
    {
        if (movie)
        {
            StartCoroutine(PlayMovieCoroutine(movie, loop));
        }
    }

    public void StopMovie(MovieTexture movie)
    {
        if (movie)
        {
            movie.Stop();
        }
    }

    private IEnumerator PlayMovieCoroutine(MovieTexture movie, bool loop)
    {
        bool isStreamed = !(movie && movie.isReadyToPlay);

        while (movie && !movie.isReadyToPlay)
        {
            yield return new WaitForEndOfFrame();
        }

        if (isStreamed)
            yield return new WaitForSeconds(1);

        if (movie != null)
        {
            movie.loop = loop;
            movie.Play();
        }
    }
}
#endif
using System.Collections;
using UnityEngine;
using UnityEngine.Video;

public class StreamedMoviePlayer : MonoBehaviour
{
    public static StreamedMoviePlayer Instance { get; private set; }
    
    private VideoPlayer videoPlayer;

    void Awake()
    {
        Instance = this;
        
        // Get or create VideoPlayer component
        videoPlayer = GetComponent<VideoPlayer>();
        if (videoPlayer == null)
        {
            videoPlayer = gameObject.AddComponent<VideoPlayer>();
        }
    }

    public void PlayMovie(VideoClip movie, bool loop = false)
    {
        if (movie)
        {
            StartCoroutine(PlayMovieCoroutine(movie, loop));
        }
    }

    public void StopMovie(VideoClip movie)
    {
        if (videoPlayer && videoPlayer.clip == movie)
        {
            videoPlayer.Stop();
        }
    }

    private IEnumerator PlayMovieCoroutine(VideoClip movie, bool loop)
    {
        videoPlayer.clip = movie;
        videoPlayer.isLooping = loop;
        
        // Wait for the video to be prepared
        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared)
        {
            yield return new WaitForEndOfFrame();
        }

        if (videoPlayer.clip != null)
        {
            videoPlayer.Play();
        }
    }
}
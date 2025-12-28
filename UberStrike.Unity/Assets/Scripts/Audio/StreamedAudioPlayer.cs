using System.Collections;
using UnityEngine;

public class StreamedAudioPlayer : MonoBehaviour
{
    public static StreamedAudioPlayer Instance { get; private set; }

    private static int _playCounter = 0;

    private void Awake()
    {
        Instance = this;
    }

    public void PlayMusic(AudioSource source, string clipName)
    {
        if (!string.IsNullOrEmpty(clipName))
        {
            StartCoroutine(PlayMusic(source, AudioLoader.Instance.Get(clipName)));
        }
        else
        {
            StopMusic(source);
        }
    }

    public void StopMusic(AudioSource source)
    {
        ++_playCounter;
        source.Stop();
    }

    private IEnumerator PlayMusic(AudioSource source, AudioClip clip)
    {
        int id = ++_playCounter;
        bool isStreamed = !(clip && clip.isReadyToPlay);

        // Wait for the AudioClip to be ready to play
        while (clip && !clip.isReadyToPlay)
        {
            // Need to check if we want to exit the wait
            yield return new WaitForEndOfFrame();
        }

        if (isStreamed)
            yield return new WaitForSeconds(1);

        // If we haven't canceled this AudioClip download or started playing other music, start playing
        if (clip != null)
        {
            source.clip = clip;
            source.Play();
        }

        //just keep playing the same song over and over (remember: streamed audio doesn't loop by itself)
        //remember to keep loop ENABLED in the inspector
        while (id == _playCounter)// musicAudioSource.loop)
        {
            //wait until track finished playing
            while (source.isPlaying && id == _playCounter)
            {
                yield return new WaitForEndOfFrame();
            }

            //repeat track if it didn't change
            if (id == _playCounter)
            {
                source.Play();
                yield return new WaitForEndOfFrame();
            }
            else
            {
                source.Stop();
            }
        }
    }
}
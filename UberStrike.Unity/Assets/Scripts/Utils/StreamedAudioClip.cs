using UnityEngine;
using System.Collections;

public class StreamedAudioClip
{
    private AudioClip _clip;
    private AudioClip _streamedAudioClip;

    private int _lastPosition = 0;
    private bool _downloadingFinished = false;
    private float[] _samples;

    public AudioClip Clip
    {
        get { return _clip; }
    }

    public bool IsDownloaded
    {
        get { return _downloadingFinished; }
    }

    public StreamedAudioClip(string name)
    {
        WWW www = new WWW(ApplicationDataManager.BaseAudioURL + name);
        MonoRoutine.Start(StartDownloadingAudioClip(www));

        _streamedAudioClip = www.GetAudioClip(false, true, AudioType.OGGVORBIS);
        _samples = new float[_streamedAudioClip.samples * _streamedAudioClip.channels];
        _clip = AudioClip.Create(name, _streamedAudioClip.samples, _streamedAudioClip.channels, _streamedAudioClip.frequency, false, true, OnPCMRead, OnPCMSetPosition);
    }

    /// <summary>
    /// For testing purpose
    /// </summary>
    /// <param name="name">name of the streamed audio clip</param>
    /// <param name="source">original audio clip to test</param>
    public StreamedAudioClip(string name, AudioClip source)
    {
        _samples = new float[source.samples * source.channels];
        
        source.GetData(_samples, 0);

        _clip = AudioClip.Create(name, source.samples, source.channels, source.frequency, false, true, OnPCMRead, OnPCMSetPosition);
        _downloadingFinished = true;
    }

    private void OnPCMRead(float[] data)
    {
        for (int i = 0; _downloadingFinished && i < data.Length; i++)
        {
            int index = _lastPosition + i;

            if (index < _samples.Length)
            {
                data[i] = _samples[index];
            }
            else
            {
                data[i] = 0;
            }
        }

        _lastPosition += data.Length;
    }

    private void OnPCMSetPosition(int position)
    {
        _lastPosition = position;
    }

    private IEnumerator StartDownloadingAudioClip(WWW www)
    {
        while (!www.isDone)
        {
            yield return new WaitForEndOfFrame();
        }

        if (!string.IsNullOrEmpty(www.error))
        {
            Debug.LogError(www.error + "\n" + www.url);
        }

        if (_samples != null)
        {
            AudioClip clip = www.GetAudioClip(false, false, AudioType.OGGVORBIS);
            if (clip)
            {
            }
            else
            {
                Debug.LogError("Failed to GetAudioClip from WWW");
            }
            //_streamedAudioClip.GetData(_samples, 0);
            //_downloadingFinished = true;
        }
        else
        {
            Debug.LogError("Failed to GetData from " + _streamedAudioClip.name);
        }
    }
}
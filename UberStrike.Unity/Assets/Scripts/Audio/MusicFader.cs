using UnityEngine;
using System.Collections;

public class MusicFader
{
    private bool _isFading;
    private float _targetVolume;
    private AudioSource _audioSource;

    public MusicFader(AudioSource audio)
    {
        _audioSource = audio;
    }

    public void FadeIn(float volume)
    {
        _targetVolume = volume;

        if (!_isFading)
        {
            if (!_audioSource.isPlaying)
            {
                _audioSource.Play();
            }
            MonoRoutine.Start(StartFading());
        }
    }

    public void FadeOut()
    {
        _targetVolume = 0;

        if (!_isFading)
        {
            MonoRoutine.Start(StartFading());
        }
    }

    private IEnumerator StartFading()
    {
        const float threshold = 0.05f;

        _isFading = true;

        while (Mathf.Abs(_audioSource.volume - _targetVolume) > threshold)
        {
            _audioSource.volume = Mathf.Lerp(_audioSource.volume, _targetVolume, Time.deltaTime * 3);
            yield return new WaitForEndOfFrame();
        }

        if (_targetVolume == 0)
        {
            _audioSource.volume = 0;
            _audioSource.Stop();
        }

        _isFading = false;
    }
}
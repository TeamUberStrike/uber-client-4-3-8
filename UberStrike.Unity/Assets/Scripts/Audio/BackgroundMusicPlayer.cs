using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Collections;

public class BackgroundMusicPlayer : Singleton<BackgroundMusicPlayer>
{
    private MusicFader _musicFader;
    private AudioSource _audioSource;

    private BackgroundMusicPlayer()
    {
        GameObject obj = new GameObject("BGM Player");
        _audioSource = obj.AddComponent<AudioSource>();
        _audioSource.loop = true;
        _audioSource.clip = SfxManager.GetAudioClip(SoundEffectType.BGMSeletronRadio);

        _musicFader = new MusicFader(_audioSource);
    }

    public float Volume
    {
        set { _audioSource.volume = value; }
    }

    public void Play()
    {
        _musicFader.FadeIn(SfxManager.MusicAudioVolume);
    }

    public void Stop()
    {
        _musicFader.FadeOut();
    }
}
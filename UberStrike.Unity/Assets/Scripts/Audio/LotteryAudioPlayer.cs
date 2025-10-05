using UnityEngine;
using System.Collections;

public class LotteryAudioPlayer : Singleton<LotteryAudioPlayer>
{
    private AudioSource _source;
    private MusicFader _musicFader;

    private LotteryAudioPlayer()
    {
        GameObject player = new GameObject("LotteryAudioPlayer");
        _source = player.AddComponent<AudioSource>();
        _source.loop = true;
        _source.priority = 0;
        _source.clip = SfxManager.GetAudioClip(SoundEffectType.UIMysteryBoxMusic);
        _musicFader = new MusicFader(_source);
    }

    public void Play()
    {
        _musicFader.FadeIn(SfxManager.MusicAudioVolume * 1.3f);
    }

    public void Stop()
    {
        _musicFader.FadeOut();
    }
}
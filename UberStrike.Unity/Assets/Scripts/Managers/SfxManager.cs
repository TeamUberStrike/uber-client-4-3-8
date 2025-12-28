using System.Collections.Generic;
using UnityEngine;
using UberStrike.DataCenter.Common.Entities;
using Cmune.Util;
using System.Collections;

public class SfxManager : MonoSingleton<SfxManager>
{
    #region Inspector

    public SoundValuePair[] AllSounds;

    [SerializeField]
    private AudioSource uiAudioSource;

    [SerializeField]
    private AudioSource musicAudioSource;

    #endregion

    #region Fields

    private Dictionary<SoundEffectType, AudioClip> _sounds;

    private static SoundEffectType _lastFootStep;

    private readonly static SoundEffectType[] _footStepDirt = new SoundEffectType[] { SoundEffectType.PcFootStepDirt1, SoundEffectType.PcFootStepDirt2, SoundEffectType.PcFootStepDirt3, SoundEffectType.PcFootStepDirt4 };
    private readonly static SoundEffectType[] _footStepGrass = new SoundEffectType[] { SoundEffectType.PcFootStepGrass1, SoundEffectType.PcFootStepGrass2, SoundEffectType.PcFootStepGrass3, SoundEffectType.PcFootStepGrass4 };
    private readonly static SoundEffectType[] _footStepMetal = new SoundEffectType[] { SoundEffectType.PcFootStepMetal1, SoundEffectType.PcFootStepMetal2, SoundEffectType.PcFootStepMetal3, SoundEffectType.PcFootStepMetal4 };
    private readonly static SoundEffectType[] _footStepHeavyMetal = new SoundEffectType[] { SoundEffectType.PcFootStepHeavyMetal1, SoundEffectType.PcFootStepHeavyMetal2, SoundEffectType.PcFootStepHeavyMetal3, SoundEffectType.PcFootStepHeavyMetal4 };
    private readonly static SoundEffectType[] _footStepRock = new SoundEffectType[] { SoundEffectType.PcFootStepRock1, SoundEffectType.PcFootStepRock2, SoundEffectType.PcFootStepRock3, SoundEffectType.PcFootStepRock4 };
    private readonly static SoundEffectType[] _footStepSand = new SoundEffectType[] { SoundEffectType.PcFootStepSand1, SoundEffectType.PcFootStepSand2, SoundEffectType.PcFootStepSand3, SoundEffectType.PcFootStepSand4 };
    private readonly static SoundEffectType[] _footStepWater = new SoundEffectType[] { SoundEffectType.PcFootStepWater1, SoundEffectType.PcFootStepWater2, SoundEffectType.PcFootStepWater3 };
    private readonly static SoundEffectType[] _footStepWood = new SoundEffectType[] { SoundEffectType.PcFootStepWood1, SoundEffectType.PcFootStepWood2, SoundEffectType.PcFootStepWood3, SoundEffectType.PcFootStepWood4 };
    private readonly static SoundEffectType[] _swimAboveWater = new SoundEffectType[] { SoundEffectType.PcSwimAboveWater1, SoundEffectType.PcSwimAboveWater2, SoundEffectType.PcSwimAboveWater3, SoundEffectType.PcSwimAboveWater4 };
    private readonly static SoundEffectType[] _swimUnderWater = new SoundEffectType[] { SoundEffectType.PcSwimUnderWater };
    private readonly static SoundEffectType[] _footStepSnow = new SoundEffectType[] { SoundEffectType.PcFootStepSnow1, SoundEffectType.PcFootStepSnow2, SoundEffectType.PcFootStepSnow3, SoundEffectType.PcFootStepSnow4 };
    private readonly static SoundEffectType[] _footStepGlass = new SoundEffectType[] { SoundEffectType.PcFootStepGlass1, SoundEffectType.PcFootStepGlass2, SoundEffectType.PcFootStepGlass3, SoundEffectType.PcFootStepGlass4 };

    private readonly static SoundEffectType[] _impactCement = new SoundEffectType[] { SoundEffectType.EnvImpactCement1, SoundEffectType.EnvImpactCement2, SoundEffectType.EnvImpactCement3, SoundEffectType.EnvImpactCement4 };
    private readonly static SoundEffectType[] _impactGlass = new SoundEffectType[] { SoundEffectType.EnvImpactGlass1, SoundEffectType.EnvImpactGlass2, SoundEffectType.EnvImpactGlass3, SoundEffectType.EnvImpactGlass4, SoundEffectType.EnvImpactGlass5 };
    private readonly static SoundEffectType[] _impactGrass = new SoundEffectType[] { SoundEffectType.EnvImpactGrass1, SoundEffectType.EnvImpactGrass2, SoundEffectType.EnvImpactGrass3, SoundEffectType.EnvImpactGrass4 };
    private readonly static SoundEffectType[] _impactMetal = new SoundEffectType[] { SoundEffectType.EnvImpactMetal1, SoundEffectType.EnvImpactMetal2, SoundEffectType.EnvImpactMetal3, SoundEffectType.EnvImpactMetal4, SoundEffectType.EnvImpactMetal5 };
    private readonly static SoundEffectType[] _impactSand = new SoundEffectType[] { SoundEffectType.EnvImpactSand1, SoundEffectType.EnvImpactSand2, SoundEffectType.EnvImpactSand3, SoundEffectType.EnvImpactSand4, SoundEffectType.EnvImpactSand5 };
    private readonly static SoundEffectType[] _impactStone = new SoundEffectType[] { SoundEffectType.EnvImpactStone1, SoundEffectType.EnvImpactStone2, SoundEffectType.EnvImpactStone3, SoundEffectType.EnvImpactStone4, SoundEffectType.EnvImpactStone5 };
    private readonly static SoundEffectType[] _impactWater = new SoundEffectType[] { SoundEffectType.EnvImpactWater1, SoundEffectType.EnvImpactWater2, SoundEffectType.EnvImpactWater3, SoundEffectType.EnvImpactWater4, SoundEffectType.EnvImpactWater5 };
    private readonly static SoundEffectType[] _impactWood = new SoundEffectType[] { SoundEffectType.EnvImpactWood1, SoundEffectType.EnvImpactWood2, SoundEffectType.EnvImpactWood3, SoundEffectType.EnvImpactWood4, SoundEffectType.EnvImpactWood5 };

    private readonly static Dictionary<string, SoundEffectType[]> _surfaceImpactSoundMap = new Dictionary<string, SoundEffectType[]>();

    #endregion

    #region Properties

    public static float EffectsAudioVolume
    {
        get { return ApplicationDataManager.Exists ? ApplicationDataManager.ApplicationOptions.AudioEffectsVolume : 0.7f; }
    }

    public static float MusicAudioVolume
    {
        get { return ApplicationDataManager.Exists ? ApplicationDataManager.ApplicationOptions.AudioMusicVolume : 0.3f; }
    }

    public static float MasterAudioVolume
    {
        get { return ApplicationDataManager.Exists ? ApplicationDataManager.ApplicationOptions.AudioMasterVolume : 0.5f; }
    }

    #endregion

    #region Classes

    [System.Serializable]
    public class SoundValuePair
    {
        public SoundValuePair(SoundEffectType id, AudioClip clip)
        {
            _name = id.ToString();
            ID = id;
            Audio = clip;
        }

        //leave this field because it indicates the name in the inspector
#pragma warning disable 0414
        [SerializeField]
        private string _name;
#pragma warning restore 0414

        public SoundEffectType ID;
        public AudioClip Audio;
    }

    #endregion

    private void Awake()
    {
        _sounds = new Dictionary<SoundEffectType, AudioClip>(AllSounds.Length);

        foreach (SoundValuePair pair in AllSounds)
        {
            if (_sounds.ContainsKey(pair.ID))
            {
                Debug.LogError("SfxManager already contains a sound with ID " + pair.ID.ToString());
            }
            else
            {
                _sounds.Add(pair.ID, pair.Audio);

                if (pair.Audio == null)
                {
                    throw new CmuneException("Missing Audio Clip in SfxManager for '{0}'", pair.ID.ToString());
                }
            }
        }

        _surfaceImpactSoundMap.Add("Wood", _impactWood);
        _surfaceImpactSoundMap.Add("SolidWood", _impactWood);
        _surfaceImpactSoundMap.Add("Stone", _impactStone);
        _surfaceImpactSoundMap.Add("Metal", _impactMetal);
        _surfaceImpactSoundMap.Add("Sand", _impactSand);
        _surfaceImpactSoundMap.Add("Grass", _impactGrass);
        _surfaceImpactSoundMap.Add("Glass", _impactGlass);
        _surfaceImpactSoundMap.Add("Cement", _impactCement);
        _surfaceImpactSoundMap.Add("Water", _impactWater);
    }

    public static AudioClip GetAudioClip(SoundEffectType soundEffect)
    {
        AudioClip clip = null;
        Instance._sounds.TryGetValue(soundEffect, out clip);
        return clip;
    }

    public static void StopAll2dAudio()
    {
        Instance.uiAudioSource.Stop();
    }

    public static void Play2dAudioClip(SoundEffectType soundEffect, float delay)
    {
        MonoRoutine.Start(Play2dAudioClipInSeconds(soundEffect, delay));
    }

    private static IEnumerator Play2dAudioClipInSeconds(SoundEffectType soundEffect, float delay)
    {
        yield return new WaitForSeconds(delay);
        Play2dAudioClip(soundEffect);
    }

    public static void Play2dAudioClip(SoundEffectType soundEffect)
    {
        try
        {
            Instance.uiAudioSource.PlayOneShot(Instance._sounds[soundEffect]);
        }
        catch
        {
            Debug.LogError("Play2dAudioClip: " + soundEffect + " failed.");
        }
    }

    public static void Play2dAudioClip(AudioClip audioClip)
    {
        try
        {
            Instance.uiAudioSource.PlayOneShot(audioClip);
        }
        catch
        {
            Debug.LogError("Play2dAudioClip: failed.");
        }
    }

    public static void Play3dAudioClip(AudioClip audioClip, Vector3 position)
    {
        try
        {
            AudioSource.PlayClipAtPoint(audioClip, position, Instance.uiAudioSource.volume);
        }
        catch
        {
            Debug.LogError("Play3dAudioClip: failed.");
        }
    }

    public static void Play3dAudioClip(SoundEffectType soundEffect, Vector3 position)
    {
        try
        {
            AudioSource.PlayClipAtPoint(Instance._sounds[soundEffect], position, Instance.uiAudioSource.volume);
        }
        catch
        {
            Debug.LogError("Play3dAudioClip: " + soundEffect + " failed.");
        }
    }

    public static void Play3dAudioClip(SoundEffectType soundEffect, float volume, float minDistance, float maxDistance, AudioRolloffMode rolloffMode, Vector3 position)
    {
        if (minDistance <= 0)
        {
            return;
        }

        GameObject audioGo = new GameObject("One Shot Audio", typeof(AudioSource));
        float clipLength = 0;
        try
        {
            audioGo.transform.position = position;
            audioGo.audio.clip = Instance._sounds[soundEffect];
            clipLength = audioGo.audio.clip.length;

            // Custom AudioSource Parameters here
            audioGo.audio.volume = volume;
            audioGo.audio.rolloffMode = rolloffMode;
            audioGo.audio.minDistance = minDistance;
            audioGo.audio.maxDistance = maxDistance;

            audioGo.audio.Play();
        }
        catch
        {
            Debug.LogError("Play3dAudioClip: " + soundEffect + " failed.");
        }
        finally
        {
            Destroy(audioGo, clipLength);
        }
    }

#if UNITY_ANDROID || UNITY_IPHONE
    public void PlayMusicMobile(AudioClip clip, float volume)
    {
        if (clip != null)
        {
            musicAudioSource.volume = MusicAudioVolume * volume;
            musicAudioSource.clip = clip;
            musicAudioSource.loop = true;
            musicAudioSource.Play();
        }
    }
#endif

    public static void PlayFootStepAudioClip(FootStepSoundType footStep, Vector3 position)
    {
        SoundEffectType[] currentSoundEffectTypes = null;

        switch (footStep)
        {
            case FootStepSoundType.Dirt:
                currentSoundEffectTypes = _footStepDirt;
                break;

            case FootStepSoundType.Grass:
                currentSoundEffectTypes = _footStepGrass;
                break;

            case FootStepSoundType.Metal:
                currentSoundEffectTypes = _footStepMetal;
                break;

            case FootStepSoundType.HeavyMetal:
                currentSoundEffectTypes = _footStepHeavyMetal;
                break;

            case FootStepSoundType.Rock:
                currentSoundEffectTypes = _footStepRock;
                break;

            case FootStepSoundType.Sand:
                currentSoundEffectTypes = _footStepSand;
                break;

            case FootStepSoundType.Water:
                currentSoundEffectTypes = _footStepWater;
                break;

            case FootStepSoundType.Wood:
                currentSoundEffectTypes = _footStepWood;
                break;

            case FootStepSoundType.Swim:
                currentSoundEffectTypes = _swimAboveWater;
                break;

            case FootStepSoundType.Dive:
                currentSoundEffectTypes = _swimUnderWater;
                break;

            case FootStepSoundType.Snow:
                currentSoundEffectTypes = _footStepSnow;
                break;

            case FootStepSoundType.Glass:
                currentSoundEffectTypes = _footStepGlass;
                break;
        }

        if (currentSoundEffectTypes != null && currentSoundEffectTypes.Length > 0)
        {
            SoundEffectType soundEffectType = SoundEffectType.None;

            if (currentSoundEffectTypes.Length > 1)
            {
                do
                {
                    soundEffectType = currentSoundEffectTypes[Random.Range(0, currentSoundEffectTypes.Length)];
                } while (soundEffectType == _lastFootStep);
            }

            else if (currentSoundEffectTypes.Length > 0)
            {
                soundEffectType = currentSoundEffectTypes[0];
            }

            if (soundEffectType != SoundEffectType.None)
            {
                _lastFootStep = soundEffectType;
                Play3dAudioClip(soundEffectType, position);
            }
        }
        else
        {
            Debug.LogWarning("FootStep type not supported: " + footStep);
        }
    }

    public static void PlayImpactSound(string surfaceType, Vector3 position)
    {
        SoundEffectType[] soundEffectTypes = null;

        if (_surfaceImpactSoundMap.TryGetValue(surfaceType, out soundEffectTypes))
        {
            Play3dAudioClip(soundEffectTypes[Random.Range(0, soundEffectTypes.Length)], position);
        }
    }

    public static void EnableAudio(bool enabled)
    {
        AudioListener.volume = enabled ? ApplicationDataManager.ApplicationOptions.AudioMasterVolume : 0;
    }

    public static void UpdateMasterVolume()
    {
        if (ApplicationDataManager.ApplicationOptions.AudioEnabled)
        {
            AudioListener.volume = ApplicationDataManager.ApplicationOptions.AudioMasterVolume;
        }
    }

    public static void UpdateMusicVolume()
    {
        if (ApplicationDataManager.ApplicationOptions.AudioEnabled)
        {
            Instance.musicAudioSource.volume = ApplicationDataManager.ApplicationOptions.AudioMusicVolume;
            BackgroundMusicPlayer.Instance.Volume = ApplicationDataManager.ApplicationOptions.AudioMusicVolume;
        }
    }

    public static void UpdateEffectsVolume()
    {
        if (ApplicationDataManager.ApplicationOptions.AudioEnabled)
        {
            Instance.uiAudioSource.volume = ApplicationDataManager.ApplicationOptions.AudioEffectsVolume;
        }
    }

    public static float GetSoundLength(SoundEffectType soundEffectType)
    {
        try
        {
            return GetAudioClip(soundEffectType).length;
        }
        catch
        {
            Debug.LogError("GetSoundLength: Failed to get AudioClip");
            return 0;
        }
    }
}
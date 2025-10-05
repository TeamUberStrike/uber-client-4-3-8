using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PopulateUnitySoundsList : MonoBehaviour
{
    [MenuItem("Cmune Tools/Managers/Populate Unity Sounds")]
    public static void PopulateItemList()
    {
        if (!Selection.activeGameObject)
        {
            Debug.LogError("Please select a GameObject in the scene!");
            return;
        }

        SfxManager sfxManager = Selection.activeGameObject.GetComponentInChildren<SfxManager>();

        if (sfxManager == null)
        {
            Debug.LogError("No 'SfxManager' script attached!");
            return;
        }

        sfxManager.AllSounds = AddSounds().ToArray();

        Debug.Log("Successfully added all sounds to SfxManager!");
    }

    private static List<SfxManager.SoundValuePair> AddSounds()
    {
        List<SfxManager.SoundValuePair> sounds = new List<SfxManager.SoundValuePair>();

        foreach (SoundEffectType sound in Enum.GetValues(typeof(SoundEffectType)))
        {
            if (sound == SoundEffectType.None) continue;

            string s = sound.ToString();

            if (s.StartsWith("Ambient", true, System.Globalization.CultureInfo.InvariantCulture))
            {
                AudioClip asset = GetSound("Ambient/" + s);
                if (asset) AddAudioClip(sounds, sound, asset);
            }
            else if (s.StartsWith("BGM", true, System.Globalization.CultureInfo.InvariantCulture))
            {
                AudioClip asset = GetSound("BGM/" + s.Substring(3));
                if (asset) AddAudioClip(sounds, sound, asset);
                else Debug.LogError("No Sound Asset found for " + s);
            }
            else if (s.StartsWith("Env", true, System.Globalization.CultureInfo.InvariantCulture))
            {
                AudioClip asset = GetSound("Environment/" + s.Substring(3));
                if (asset) AddAudioClip(sounds, sound, asset);
            }
            else if (s.StartsWith("Game", true, System.Globalization.CultureInfo.InvariantCulture))
            {
                AudioClip asset = GetSound("Game/" + s.Substring(4));
                if (asset) AddAudioClip(sounds, sound, asset);
            }
            else if (s.StartsWith("PC", true, System.Globalization.CultureInfo.InvariantCulture))
            {
                AudioClip asset = GetSound("PC/" + s.Substring(2));
                if (asset) AddAudioClip(sounds, sound, asset);
            }
            else if (s.StartsWith("Props", true, System.Globalization.CultureInfo.InvariantCulture))
            {
                AudioClip asset = GetSound("Props/" + s.Substring(5));
                if (asset) AddAudioClip(sounds, sound, asset);
            }
            else if (s.StartsWith("Weapon", true, System.Globalization.CultureInfo.InvariantCulture))
            {
                AudioClip asset = GetSound("Weapons/" + s.Substring(6));
                if (asset) AddAudioClip(sounds, sound, asset);
            }
            else if (s.StartsWith("QuickItems", true, System.Globalization.CultureInfo.InvariantCulture))
            {
                AudioClip asset = GetSound("QuickItems/" + s.Substring(10));
                if (asset) AddAudioClip(sounds, sound, asset);
            }
            else if (s.StartsWith("UI", true, System.Globalization.CultureInfo.InvariantCulture))
            {
                AudioClip asset = GetSound("UI/" + s.Substring(2));
                if (asset) AddAudioClip(sounds, sound, asset);
            }
            else if (s.StartsWith("HUD", true, System.Globalization.CultureInfo.InvariantCulture))
            {
                AudioClip asset = GetSound("HUD/" + s.Substring(3));
                if (asset) AddAudioClip(sounds, sound, asset);
            }
            else if (s.StartsWith("Voice", true, System.Globalization.CultureInfo.InvariantCulture))
            {
                AudioClip asset = GetSound("Voice/" + s.Substring(5));
                if (asset) AddAudioClip(sounds, sound, asset);
            }
            else
            {
                Debug.LogError("No Sound Asset found for " + s);
            }
        }

        return sounds;
    }

    private static void AddAudioClip(List<SfxManager.SoundValuePair> list, SoundEffectType sound, AudioClip audio)
    {
        list.Add(new SfxManager.SoundValuePair(sound, audio));
    }

    private static AudioClip GetSound(string fileName)
    {
        AudioClip sound = null;

        UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath("Assets/Audio/" + fileName + ".wav", typeof(AudioClip));
        if (asset != null)
            sound = asset as AudioClip;
        else
        {
            asset = AssetDatabase.LoadAssetAtPath("Assets/Audio/" + fileName + ".mp3", typeof(AudioClip));
            if (asset != null)
                sound = asset as AudioClip;
            else
                Debug.LogError(string.Format("{0} Sound Not Found!", fileName));
        }


        return sound;
    }
}

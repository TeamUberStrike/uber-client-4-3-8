using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TutorialAirlockDoorAnimEvent : MonoBehaviour
{
    private Dictionary<string, AudioSource> _doorAudioSources;
    private Dictionary<string, float> _doorLockTiming;

    private void Awake()
    {
        _doorLockTiming = new Dictionary<string, float>();
        _doorAudioSources = new Dictionary<string, AudioSource>();

        _doorLockTiming.Add("AirlockDoor", 0);
        _doorLockTiming.Add("Gear4", 30 / 60f);
        _doorLockTiming.Add("Gear3", 40 / 60f);
        _doorLockTiming.Add("Gear2", 50 / 60f);
        _doorLockTiming.Add("Gear1", 60 / 60f);
        _doorLockTiming.Add("Gear10", 70 / 60f);
        _doorLockTiming.Add("Gear9", 80 / 60f);
        _doorLockTiming.Add("Gear8", 90 / 60f);

        AudioSource[] sources = GetComponentsInChildren<AudioSource>(true);

        foreach (AudioSource s in sources)
        {
            float time;
            AnimationEvent ev = new AnimationEvent();

            ev.functionName = "OnDoorUnlock";
            ev.stringParameter = s.gameObject.name;

            if (_doorLockTiming.TryGetValue(ev.stringParameter, out time))
            {
                ev.time = time;

                animation.clip.AddEvent(ev);

                _doorAudioSources.Add(ev.stringParameter, s);
            }
            else
            {
                Debug.LogError("Failed to get door lock: " + ev.stringParameter);
            }
        }
    }

    private void OnDoorUnlock(string lockName)
    {
        AudioSource source;

        if (_doorAudioSources.TryGetValue(lockName, out source))
        {
            source.Play();

            _doorAudioSources.Remove(lockName);
        }
    }
}

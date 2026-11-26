using UnityEngine;
using System.Collections;
using System;

public class AudioFadeArea : MonoBehaviour
{
	private static AudioFadeArea Current;
	
    [SerializeField]
    private AudioArea outdoorAudio;

    [SerializeField]
    private AudioArea indoorAudio;
	
    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void Update()
    {
        if (Current == this)
        {
            outdoorAudio.Update();
			indoorAudio.Update();
        }
    }

    void OnTriggerEnter(Collider collider)
    {
        if (collider.tag == "Player")
        {
			Current = this;
			
            if (outdoorAudio != null)
                outdoorAudio.Enabled = false;
            if (indoorAudio != null)
                indoorAudio.Enabled = true;
        }
    }

    void OnTriggerExit(Collider collider)
    {
        if (collider.tag == "Player")
        {
            if (outdoorAudio != null)
                outdoorAudio.Enabled = Current == this;
            if (indoorAudio != null)
                indoorAudio.Enabled = false;
        }
    }

    /// <summary>Sources this area has already faded and now sets itself (MapAudioVolume leaves them to it).</summary>
    public void AddControlledSources(System.Collections.Generic.HashSet<AudioSource> set)
    {
        foreach (AudioArea area in new[] { outdoorAudio, indoorAudio })
            if (area != null && area.Touched && area.Sources != null)
                foreach (AudioSource s in area.Sources)
                    if (s != null) set.Add(s);
    }

    /// <summary>Re-apply the current fade volume after the "Map Volume" option changed.</summary>
    public void ApplyMapVolume()
    {
        if (outdoorAudio != null && outdoorAudio.Touched) outdoorAudio.ApplyVolume();
        if (indoorAudio != null && indoorAudio.Touched) indoorAudio.ApplyVolume();
    }

    [System.Serializable]
    private class AudioArea
    {
        [SerializeField]
        private AudioSource[] sources;
        [SerializeField]
        private float maxVolume = 1;
		[SerializeField]
        private float currentVolume = 1;
        [SerializeField]
        private float fadeSpeed = 3;
        [SerializeField]
        private bool enabled = false;
		
		public AudioArea()
		{
			currentVolume = enabled ? maxVolume : 0;	
		}

        public bool Enabled
        {
            get { return enabled; }
            set
            {
                enabled = value;
            }
        }

        public AudioSource[] Sources { get { return sources; } }

        // Set once this area has faded its sources; before that they keep the volume set in the map.
        public bool Touched { get; private set; }

        // The fade volume times the "Map Volume" option.
        public void ApplyVolume()
        {
            if (sources == null) return;
            Touched = true;
            float volume = currentVolume * SfxManager.WorldAudioVolume;
            Array.ForEach(sources, s => { if (s != null) s.volume = volume; });
        }

        public bool Update()
        {
            if (enabled && currentVolume < maxVolume)
            {
                currentVolume = Mathf.Lerp(currentVolume, maxVolume, Time.deltaTime * fadeSpeed);
                if (Mathf.Abs(currentVolume - maxVolume) < 0.01f) currentVolume = maxVolume;

                ApplyVolume();
                return true;
            }
            else if (!enabled && currentVolume > 0)
            {
                currentVolume = Mathf.Lerp(currentVolume, 0, Time.deltaTime * fadeSpeed);
                if (currentVolume < 0.01f) currentVolume = 0;

                ApplyVolume();
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
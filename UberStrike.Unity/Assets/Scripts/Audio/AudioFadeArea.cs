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
        collider.isTrigger = true;
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

        public bool Update()
        {
            if (enabled && currentVolume < maxVolume)
            {
                currentVolume = Mathf.Lerp(currentVolume, maxVolume, Time.deltaTime * fadeSpeed);
                if (Mathf.Abs(currentVolume - maxVolume) < 0.01f) currentVolume = maxVolume;

                Array.ForEach(sources, s => s.volume = currentVolume);
                return true;
            }
            else if (!enabled && currentVolume > 0)
            {
                currentVolume = Mathf.Lerp(currentVolume, 0, Time.deltaTime * fadeSpeed);
                if (currentVolume < 0.01f) currentVolume = 0;

                Array.ForEach(sources, s => s.volume = currentVolume);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(AudioSource))]
public class AudioTriggerArea : MonoBehaviour
{
    [SerializeField]
    private bool loopClip;

    [SerializeField]
    private float maxVolume;

    private AudioSource audioSource;
    private float wishVolume = 0.0f;

    private void Awake()
    {
        audioSource = this.GetComponent<AudioSource>();
        audioSource.volume = 0.0f;
    }

    private void Update()
    {
        if (audioSource.isPlaying)
        {
            if (audioSource.volume < wishVolume)
                audioSource.volume += Time.deltaTime;
            else
                audioSource.volume -= Time.deltaTime;

            if (audioSource.volume <= 0.0f)
                audioSource.Stop();
        }
    }

    private void OnTriggerEnter(Collider collider)
    {
        if (collider.tag == "Player")
        {
            audioSource.loop = loopClip;
            wishVolume = maxVolume;
            audioSource.Play();
        }
    }

    private void OnTriggerExit(Collider collider)
    {
        if (collider.tag == "Player" && audioSource.isPlaying)
        {
            wishVolume = 0.0f;
        }
    }
}

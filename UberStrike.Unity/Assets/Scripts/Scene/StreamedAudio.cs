using UnityEngine;

[RequireComponent(typeof(AudioSource))]
class StreamedAudio : MonoBehaviour
{
    [SerializeField]
    private string _clipName;

    private void OnEnable()
    {
        StreamedAudioPlayer.Instance.PlayMusic(GetComponent<AudioSource>(), _clipName);
    }

    private void OnDisable()
    {
        StreamedAudioPlayer.Instance.StopMusic(GetComponent<AudioSource>());
    }
}

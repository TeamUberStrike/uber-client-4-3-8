using UnityEngine;

[RequireComponent(typeof(AudioSource))]
class StreamedAudio : MonoBehaviour
{
    [SerializeField]
    private string _clipName;

    private void OnEnable()
    {
        StreamedAudioPlayer.Instance.PlayMusic(audio, _clipName);
    }

    private void OnDisable()
    {
        StreamedAudioPlayer.Instance.StopMusic(audio);
    }
}

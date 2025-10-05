using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class Teleport : MonoBehaviour
{
    [SerializeField]
    private Transform _spawnPoint;

    [SerializeField]
    private AudioClip _sound;

    private AudioSource _source;

    private void Awake()
    {
        _source = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider c)
    {
        if (c.tag == "Player")
        {
            if (_source)
                _source.PlayOneShot(_sound);

            GameState.LocalPlayer.SpawnPlayerAt(_spawnPoint.position, _spawnPoint.rotation);
        }
        else if (c.tag == "Prop")
        {
            c.transform.position = _spawnPoint.position;
        }
    }
}
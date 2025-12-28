using UnityEngine;
using System.Collections;

public class WeaponTailSound : BaseWeaponEffect
{
    [SerializeField]
    private AudioClip _tailSound;

    [SerializeField]
    private WeaponHeadAnimation _headAnimation;

    private AudioSource _tailAudioSource;
    private float _tailSoundLength = 0;
    private float _tailSoundMaxLength;

    private void Awake()
    {
        if (_tailSound)
        {
            _tailAudioSource = gameObject.AddComponent<AudioSource>();
            if (_tailAudioSource)
            {
                _tailAudioSource.clip = _tailSound;
                _tailAudioSource.playOnAwake = false;
            }

            _tailSoundMaxLength = _tailSound.length * 0.8f;
        }
        else
        {
            Debug.LogError("There is no audio clip signed for WeaponTailSound!");
        }
    }

    private void Update()
    {
        if (_tailSoundLength > 0)
        {
            if (_headAnimation)
            {
                _headAnimation.OnShoot();
            }

            _tailSoundLength -= Time.deltaTime;
        }
    }

    public override void OnShoot()
    {
        if (_tailAudioSource)
            _tailAudioSource.Stop();

        _tailSoundLength = _tailSoundMaxLength;
    }

    public override void OnPostShoot()
    {
        if (_tailAudioSource)
        {
            _tailAudioSource.Stop();
            _tailAudioSource.Play();
        }
    }

    public override void Hide()
    {
        if (_tailAudioSource)
            _tailAudioSource.Stop();
    }
}
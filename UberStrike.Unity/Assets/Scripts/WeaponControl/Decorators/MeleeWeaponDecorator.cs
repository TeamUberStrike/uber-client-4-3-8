using UnityEngine;
using System.Collections;

public sealed class MeleeWeaponDecorator : BaseWeaponDecorator
{
    [SerializeField]
    private Animation _animation;

    [SerializeField]
    private AnimationClip[] _shootAnimClips;

    [SerializeField]
    private AudioClip[] _impactSounds;

    [SerializeField]
    private AudioClip _equipSound;


    protected override void Awake()
    {
        base.Awake();

        IsMelee = true;
    }

    public override void ShowShootEffect(RaycastHit[] hits)
    {
        base.ShowShootEffect(hits);

        if (EnableShootAnimation)
        {
            if (_animation && _shootAnimClips.Length > 0)
            {
                int index = Random.Range(0, _shootAnimClips.Length);

                _animation.clip = _shootAnimClips[index];
                _animation.Rewind();
                _animation.Play();
            }
        }
    }

    public override void PlayHitSound()
    {
        
    }

    public override void PlayEquipSound()
    {
        if (_mainAudioSource && _equipSound)
            _mainAudioSource.PlayOneShot(_equipSound);
    }

    /* Melee weaopn tends to have its own impact sound */
    protected override void EmitImpactSound(string impactType, Vector3 position)
    {
        if (_impactSounds != null && _impactSounds.Length > 0)
        {
            int index = Random.Range(0, _impactSounds.Length);
            AudioClip sound = _impactSounds[index];

            if (sound)
            {
                SfxManager.Play3dAudioClip(sound, position);
            }
            else
            {
                Debug.LogError("Missing impact sound for melee weapon!");
            }
        }
        else
        {
            Debug.LogError("Melee impact sound is not signed!");
        }
    }

    protected override void ShowImpactEffects(RaycastHit hit, Vector3 direction, Vector3 muzzlePosition, float distance, bool playSound)
    {
        StartCoroutine(StartShowImpactEffects(hit, direction, muzzlePosition, distance, playSound));
    }

    private IEnumerator StartShowImpactEffects(RaycastHit hit, Vector3 direction, Vector3 muzzlePosition, float distance, bool playSound)
    {
        yield return new WaitForSeconds(0.2f);

        //base.ShowImpactEffects(hit, direction, muzzlePosition, distance, playSound);
        EmitImpactParticles(hit, direction, muzzlePosition, distance, playSound);
    }
}

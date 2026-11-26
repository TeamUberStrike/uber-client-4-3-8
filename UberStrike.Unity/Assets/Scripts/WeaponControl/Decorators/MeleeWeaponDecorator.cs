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


    // Rest pose of the parts the swing animation moves (the decorator's own transform is left out:
    // the weapon switch places it right before showing the weapon).
    private Transform[] _restTransforms;
    private Vector3[] _restPositions;
    private Quaternion[] _restRotations;
    private Vector3[] _restScales;

    protected override void Awake()
    {
        base.Awake();

        IsMelee = true;
        CaptureRestPose();
    }

    // Switching away mid-swing deactivates the weapon with the swing half played, and the legacy
    // Animation leaves the parts in that pose. Back to the rest pose whenever the weapon shows again.
    private void OnEnable()
    {
        ResetToRestPose();
    }

    private void CaptureRestPose()
    {
        if (!_animation) return;
        var parts = new System.Collections.Generic.List<Transform>();
        foreach (Transform t in _animation.GetComponentsInChildren<Transform>(true))
            if (t != transform) parts.Add(t);
        _restTransforms = parts.ToArray();
        _restPositions = new Vector3[_restTransforms.Length];
        _restRotations = new Quaternion[_restTransforms.Length];
        _restScales = new Vector3[_restTransforms.Length];
        for (int i = 0; i < _restTransforms.Length; i++)
        {
            _restPositions[i] = _restTransforms[i].localPosition;
            _restRotations[i] = _restTransforms[i].localRotation;
            _restScales[i] = _restTransforms[i].localScale;
        }
    }

    private void ResetToRestPose()
    {
        if (!_animation || _restTransforms == null) return;
        _animation.Stop();
        for (int i = 0; i < _restTransforms.Length; i++)
        {
            if (!_restTransforms[i]) continue;
            _restTransforms[i].localPosition = _restPositions[i];
            _restTransforms[i].localRotation = _restRotations[i];
            _restTransforms[i].localScale = _restScales[i];
        }
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

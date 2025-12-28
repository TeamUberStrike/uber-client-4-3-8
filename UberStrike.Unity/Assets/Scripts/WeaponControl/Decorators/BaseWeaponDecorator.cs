using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public abstract class BaseWeaponDecorator : MonoBehaviour
{
    [SerializeField]
    private Transform _muzzlePosition;

    [SerializeField]
    private AudioClip[] _shootSounds;

    private Vector3 _defaultPosition;
    private Vector3 _ironSightPosition;

    private ParticleConfigurationType _effectType;
    private MoveTrailrendererObject _trailRenderer;
    private Transform _parent;
    private ParticleSystem _particles;

    private bool _isEnabled = true;
    private bool _isShootAnimationEnabled;

    protected AudioSource _mainAudioSource;

    private Dictionary<string, SurfaceEffectType> _effectMap;

    private List<BaseWeaponEffect> _effects = new List<BaseWeaponEffect>();

    public bool IsEnabled
    {
        get { return _isEnabled; }
        set
        {
            if (gameObject.active != value)
            {
                _isEnabled = value;
                gameObject.SetActiveRecursively(_isEnabled);
                HideAllWeaponEffect();
            }
        }
    }

    public void HideAllWeaponEffect()
    {
        if (_effects != null)
        {
            foreach (BaseWeaponEffect effect in _effects)
            {
                effect.Hide();
            }
        }
    }

    public bool EnableShootAnimation
    {
        get
        {
            return _isShootAnimationEnabled;
        }
        set
        {
            _isShootAnimationEnabled = value;

            if (!_isShootAnimationEnabled)
            {
                WeaponShootAnimation anim = _effects.Find((p) => p is WeaponShootAnimation) as WeaponShootAnimation;
                if (anim)
                {
                    _effects.Remove(anim);

                    Destroy(anim);
                }
            }
        }
    }
    public bool HasShootAnimation { get; private set; }
    public Vector3 MuzzlePosition
    {
        get { return _muzzlePosition ? _muzzlePosition.position : Vector3.zero; }
    }
    public Vector3 DefaultPosition
    {
        get { return _defaultPosition; }
        set
        {
            _defaultPosition = value;
            transform.localPosition = _defaultPosition;
        }
    }
    public Vector3 CurrentPosition
    {
        get { return transform.localPosition; }
        set { transform.localPosition = value; }
    }
    public Quaternion CurrentRotation
    {
        get { return transform.localRotation; }
        set
        {
            transform.localRotation = value;
        }
    }
    public Vector3 IronSightPosition
    {
        get { return _ironSightPosition; }
        set { _ironSightPosition = value; }
    }
    public Vector3 DefaultAngles
    {
        get;
        set;
    }

    public MoveTrailrendererObject TrailRenderer { get { return _trailRenderer; } }

    public bool IsMelee { get; protected set; }

    protected virtual void Awake()
    {
        _parent = transform.parent;

        _mainAudioSource = GetComponent<AudioSource>();
        if (_mainAudioSource)
            _mainAudioSource.priority = 0;

        _effects.AddRange(GetComponentsInChildren<BaseWeaponEffect>(true));

        if (_muzzlePosition)
            _particles = _muzzlePosition.GetComponent<ParticleSystem>();

        HasShootAnimation = _effects.Exists(e => e is WeaponShootAnimation);

        InitEffectMap();
    }

    protected virtual void Start()
    {
        HideAllWeaponEffect();
    }

    public BaseWeaponDecorator Clone()
    {
        return GameObject.Instantiate(this) as BaseWeaponDecorator;
    }

    public virtual void ShowShootEffect(RaycastHit[] hits)
    {
        if (IsEnabled)
        {
            if (_muzzlePosition)
            {
                Vector3 muzzlePosition = _muzzlePosition.position;

                for (int i = 0; i < hits.Length; i++)
                {
                    Vector3 direction = (hits[i].point - muzzlePosition).normalized;
                    float distance = Vector3.Distance(muzzlePosition, hits[i].point);

                    ShowImpactEffects(hits[i], direction, muzzlePosition, distance, i == 0);
                }
            }

            foreach (BaseWeaponEffect effect in _effects)
            {
                effect.OnShoot();
            }

            if (_particles)
            {
                _particles.Stop();
                _particles.Play(_isShootAnimationEnabled);
            }

            PlayShootSound();
        }
    }

    public virtual void PostShoot()
    {
        if (IsEnabled && _effects != null)
        {
            foreach (BaseWeaponEffect e in _effects)
                e.OnPostShoot();
        }
    }

    protected virtual void ShowImpactEffects(RaycastHit hit, Vector3 direction, Vector3 muzzlePosition, float distance, bool playSound)
    {
        EmitImpactParticles(hit, direction, muzzlePosition, distance, playSound);
    }

    private static void Play3dAudioClip(AudioSource audioSource, SoundEffectType soundEffect)
    {
        Play3dAudioClip(audioSource, soundEffect, 0);
    }

    private static void Play3dAudioClip(AudioSource audioSource, SoundEffectType soundEffect, float delay)
    {
        try
        {
            audioSource.clip = SfxManager.GetAudioClip(soundEffect);
            ulong delayInHz = (ulong)(delay * audioSource.clip.frequency);
            audioSource.Play(delayInHz);
        }
        catch
        {
            Debug.LogError("Play3dAudioClip: " + soundEffect + " failed.");
        }
    }

    public virtual void StopSound()
    {
        _mainAudioSource.Stop();
    }

    public void PlayShootSound()
    {
        if (_mainAudioSource && _shootSounds != null && _shootSounds.Length > 0)
        {
            int index = Random.Range(0, _shootSounds.Length);

            AudioClip clip = _shootSounds[index];
            if (clip)
            {
                _mainAudioSource.volume = ApplicationDataManager.ApplicationOptions.AudioEffectsVolume;
                _mainAudioSource.PlayOneShot(clip);
            }
        }
    }

    private void InitEffectMap()
    {
        _effectMap = new Dictionary<string, SurfaceEffectType>();

        _effectMap.Add("Wood", SurfaceEffectType.WoodEffect);
        _effectMap.Add("SolidWood", SurfaceEffectType.WoodEffect);
        _effectMap.Add("Stone", SurfaceEffectType.StoneEffect);
        _effectMap.Add("Metal", SurfaceEffectType.MetalEffect);
        _effectMap.Add("Sand", SurfaceEffectType.SandEffect);
        _effectMap.Add("Grass", SurfaceEffectType.GrassEffect);
        _effectMap.Add("Avatar", SurfaceEffectType.Splat);
        _effectMap.Add("Water", SurfaceEffectType.WaterEffect);
        _effectMap.Add("NoTarget", SurfaceEffectType.None);
        _effectMap.Add("Cement", SurfaceEffectType.StoneEffect);
    }

    public void SetSurfaceEffect(ParticleConfigurationType effect)
    {
        _effectType = effect;
    }

    public virtual void PlayEquipSound()
    {
        SfxManager.Play2dAudioClip(SoundEffectType.WeaponWeaponSwitch);
    }

    public virtual void PlayHitSound()
    {
        Debug.LogError("Not Implemented: Should play WeaponHit sound!");
    }

    public void PlayOutOfAmmoSound()
    {
        Play3dAudioClip(_mainAudioSource, SoundEffectType.WeaponOutOfAmmoClick);
    }

    public void PlayImpactSoundAt(HitPoint point)
    {
        if (point == null) return;

        // check if should play water hit sound
        if (GameState.HasCurrentSpace && GameState.CurrentSpace.HasWaterPlane &&
            ((_muzzlePosition.position.y > GameState.CurrentSpace.WaterPlaneHeight && point.Point.y < GameState.CurrentSpace.WaterPlaneHeight) ||
            (_muzzlePosition.position.y < GameState.CurrentSpace.WaterPlaneHeight && point.Point.y > GameState.CurrentSpace.WaterPlaneHeight)))
        {
            Vector3 impactPos = point.Point;
            impactPos.y = 0;

            SfxManager.PlayImpactSound("Water", impactPos);
        }
        else
        {
            EmitImpactSound(point.Tag, point.Point);
        }
    }

    protected virtual void EmitImpactSound(string impactType, Vector3 position)
    {
        SfxManager.PlayImpactSound(impactType, position);
    }

    protected void EmitImpactParticles(RaycastHit hit, Vector3 direction, Vector3 muzzlePosition, float distance, bool playSound)
    {
        string tag = TagUtil.GetTag(hit.collider);

        Vector3 point = hit.point;
        Vector3 normal = hit.normal;
        SurfaceEffectType t = SurfaceEffectType.Default;

        if (_effectMap.TryGetValue(tag, out t))
        {
            if (GameState.HasCurrentSpace && GameState.CurrentSpace.HasWaterPlane &&
            ((_muzzlePosition.position.y > GameState.CurrentSpace.WaterPlaneHeight && point.y < GameState.CurrentSpace.WaterPlaneHeight) ||
            (_muzzlePosition.position.y < GameState.CurrentSpace.WaterPlaneHeight && point.y > GameState.CurrentSpace.WaterPlaneHeight)))
            {
                t = SurfaceEffectType.WaterEffect;
                tag = "Water";
                normal = Vector3.up;
                point.y = GameState.CurrentSpace.WaterPlaneHeight;

                /* use the hit point & direction to calculate the water hit position */
                if (!Mathf.Approximately(direction.y, 0))
                {
                    point.x = (GameState.CurrentSpace.WaterPlaneHeight - hit.point.y) / direction.y * direction.x + hit.point.x;
                    point.z = (GameState.CurrentSpace.WaterPlaneHeight - hit.point.y) / direction.y * direction.z + hit.point.z;
                }
            }

            ParticleEffectController.ShowHitEffect(_effectType, t, direction, point, normal, muzzlePosition, distance, ref _trailRenderer, _parent);
        }
    }

    public void SetMuzzlePosition(Transform muzzle)
    {
        _muzzlePosition = muzzle;
    }

    public void SetWeaponSounds(AudioClip[] sounds)
    {
        if (sounds != null)
        {
            _shootSounds = new AudioClip[sounds.Length];
            sounds.CopyTo(_shootSounds, 0);
        }
    }
}
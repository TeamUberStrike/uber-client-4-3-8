using Cmune.Util;
using UberStrike.Realtime.Common;
using UnityEngine;

public class AvatarDecorator : MonoBehaviour
{
    #region Fields

    private Transform _transform;
    private Animation _animation;

    [SerializeField]
    private CharacterHitArea[] _hitAreas;
    [SerializeField]
    private Transform _weaponAttachPoint;

    public AvatarDecoratorConfig CurrentRagdoll { get; private set; }

    private FootStepSoundType _footStep;
    private LoadoutSlotType _currentWeaponSlot;
    private UberstrikeLayer _layer = UberstrikeLayer.Default;
    private float _nextFootStepTime;

    private BaseWeaponDecorator _meleeWeapon;
    private BaseWeaponDecorator _primaryWeapon;
    private BaseWeaponDecorator _secondaryWeapon;
    private BaseWeaponDecorator _tertiaryWeapon;
    private BaseWeaponDecorator _pickupWeapon;

    public const float DiveFootstep = 3.5f;
    public const float SwimFootstep = 1.7f;
    public const float WaterFootstep = 1.4f;

    private AvatarDecoratorConfig _configuration;
    public AvatarDecoratorConfig Configuration
    {
        get { if (_configuration == null)_configuration = GetComponent<AvatarDecoratorConfig>(); return _configuration; }
    }

    #endregion

    #region Properties

    public CharacterHitArea[] HitAreas
    {
        get { return _hitAreas; }
    }
    public Renderer MeshRenderer
    {
        get
        {
            return this.GetComponent<Renderer>();
        }
    }
    public Animation Animation
    {
        get { return _animation; }
    }
    public Transform WeaponAttachPoint
    {
        get { return _weaponAttachPoint; }
    }
    public LoadoutSlotType CurrentWeaponSlot
    {
        get { return _currentWeaponSlot; }
    }
    public BaseWeaponDecorator MeleeWeapon
    {
        get { return _meleeWeapon; }
        private set
        {
            if (_meleeWeapon)
            {
                Destroy(_meleeWeapon.gameObject);
                Destroy(_meleeWeapon);
            }
            _meleeWeapon = value;
        }
    }
    public BaseWeaponDecorator PrimaryWeapon
    {
        get { return _primaryWeapon; }
        private set
        {
            if (_primaryWeapon)
            {
                Destroy(_primaryWeapon.gameObject);
                Destroy(_primaryWeapon);
            }
            _primaryWeapon = value;
        }
    }
    public BaseWeaponDecorator SecondaryWeapon
    {
        get { return _secondaryWeapon; }
        private set
        {
            if (_secondaryWeapon)
            {
                Destroy(_secondaryWeapon.gameObject);
                Destroy(_secondaryWeapon);
            }
            _secondaryWeapon = value;
        }
    }
    public BaseWeaponDecorator TertiaryWeapon
    {
        get { return _tertiaryWeapon; }
        private set
        {
            if (_tertiaryWeapon)
            {
                Destroy(_tertiaryWeapon.gameObject);
                Destroy(_tertiaryWeapon);
            }
            _tertiaryWeapon = value;
        }
    }
    public BaseWeaponDecorator PickupWeapon
    {
        get { return _pickupWeapon; }
        private set
        {
            if (_pickupWeapon)
            {
                Destroy(_pickupWeapon.gameObject);
                Destroy(_pickupWeapon);
            }
            _pickupWeapon = value;
        }
    }
    public AvatarHudInformation HudInformation { get; private set; }
    public AvatarAnimationController AnimationController { get; private set; }

    /// <summary>
    /// Set list of avatar gear
    /// </summary>
    public int[] MyGear { get; set; }

    #endregion

    private void Awake()
    {
        MyGear = new int[6];

        _currentWeaponSlot = LoadoutSlotType.WeaponPrimary;

        HudInformation = GetComponentInChildren<AvatarHudInformation>();
        if (HudInformation)
            HudInformation.Target = GetBone(BoneIndex.HeadTop);

        _animation = GetComponent<Animation>();

        _transform = transform;
    }

    private void Start()
    {
        if (_animation)
        {
            AnimationController = new AvatarAnimationController(_animation);
        }
    }

    public void SetSkinColor(Color color)
    {
        Configuration.SkinColor = color;
    }

    public void EnableOutline(bool showOutline)
    {
        if (!OutlineEffectController.Exists)
        {
            return;
        }
        if (showOutline)
        {
            OutlineEffectController.Instance.AddOutlineObject(gameObject,
                Configuration.MaterialGroup, ColorScheme.TeamOutline);
        }
        else
        {
            OutlineEffectController.Instance.RemoveOutlineObject(gameObject);
        }
    }

    /// <summary>
    /// This is for remote player.
    /// </summary>
    /// <param name="feedback"></param>
    public void SetShotFeedback(BodyPart bodyPart)
    {
        if (HudInformation)
        {
            switch (bodyPart)
            {
                case BodyPart.Head:
                    HudInformation.SetInGameFeedback(InGameEventFeedbackType.HeadShot);
                    break;

                case BodyPart.Nuts:
                    HudInformation.SetInGameFeedback(InGameEventFeedbackType.NutShot);
                    break;
            }
        }
    }

    public void DisableRagdoll()
    {
        //disable the ragdoll object
        DestroyCurrentRagdoll();

        //enable the old avatar again
        _transform.gameObject.SetActiveRecursively(true);
        ShowWeapon(_currentWeaponSlot);
    }

    public AvatarDecoratorConfig SpawnDeadRagdoll(DamageInfo shot)
    {
        //just to make sure
        DestroyCurrentRagdoll();

        AvatarDecoratorConfig ragdoll = InstantiateRagdoll();

        // move all projectiles from avatar to on ragdoll
        foreach (ArrowProjectile item in GetComponentsInChildren<ArrowProjectile>(true))
        {
            Vector3 pos = item.transform.localPosition;
            Quaternion rot = item.transform.localRotation;
            item.transform.parent = ragdoll.GetBone(BoneIndex.Hips);
            item.transform.localPosition = pos;
            item.transform.localRotation = rot;
        }

        //turn off current avatar
        _transform.gameObject.SetActiveRecursively(false);

        //apply force, following the impact vector
        Vector3 force = shot != null ? shot.Force.normalized : Vector3.zero;
        foreach (var bone in ragdoll.Bones)
        {
            if (bone.Rigidbody)
            {
                bone.Rigidbody.isKinematic = false;
                if (bone.Bone == BoneIndex.Hips)
                {
                    bone.Rigidbody.AddForce(force * 3);
                }
                else
                {
                    bone.Rigidbody.AddForce(force, ForceMode.VelocityChange);
                }

                if (GameState.IsRagdollShootable)
                {
                    bone.Transform.gameObject.layer = (int)UberstrikeLayer.Props;
                }
            }
        }

        CurrentRagdoll = ragdoll;

        return ragdoll;
    }

    private AvatarDecoratorConfig InstantiateRagdoll()
    {
        var instance = AvatarBuilder.Instance.CreateRagdoll(Configuration.AvatarType, MyGear, Configuration.SkinColor);

        //place avatar on right position
        instance.transform.localPosition = _transform.position;
        instance.transform.localRotation = _transform.rotation;

        //copy current orientation of all bones
        AvatarDecoratorConfig.CopyBones(Configuration, instance);

        SkinnedMeshRenderer smr = instance.GetComponent<SkinnedMeshRenderer>();
        smr.updateWhenOffscreen = true;

        return instance;
    }

    public void AssignWeapon(LoadoutSlotType slot, BaseWeaponDecorator decorator)
    {
        if (decorator != null)
        {
            switch (slot)
            {
                case LoadoutSlotType.WeaponMelee:
                    {
                        if (MeleeWeapon != decorator)
                            MeleeWeapon = decorator;
                        break;
                    }
                case LoadoutSlotType.WeaponPrimary:
                    {
                        if (PrimaryWeapon != decorator)
                            PrimaryWeapon = decorator;
                        break;
                    }
                case LoadoutSlotType.WeaponSecondary:
                    {
                        if (SecondaryWeapon != decorator)
                            SecondaryWeapon = decorator;
                        break;
                    }
                case LoadoutSlotType.WeaponTertiary:
                    {
                        if (TertiaryWeapon != decorator)
                            TertiaryWeapon = decorator;
                        break;
                    }
                case LoadoutSlotType.WeaponPickup:
                    {
                        if (PickupWeapon != decorator)
                            PickupWeapon = decorator;
                        break;
                    }
            }

            decorator.transform.parent = _weaponAttachPoint;
            LayerUtil.SetLayerRecursively(decorator.gameObject.transform, _layer);

            decorator.transform.localPosition = Vector3.zero;
            decorator.transform.localRotation = Quaternion.identity;
        }
        else
        {
            switch (slot)
            {
                case LoadoutSlotType.WeaponMelee:
                    {
                        MeleeWeapon = decorator;
                        break;
                    }
                case LoadoutSlotType.WeaponPrimary:
                    {
                        PrimaryWeapon = decorator;
                        break;
                    }
                case LoadoutSlotType.WeaponSecondary:
                    {
                        SecondaryWeapon = decorator;
                        break;
                    }
                case LoadoutSlotType.WeaponTertiary:
                    {
                        TertiaryWeapon = decorator;
                        break;
                    }
                case LoadoutSlotType.WeaponPickup:
                    {
                        PickupWeapon = decorator;
                        break;
                    }
                default:
                    {
                        CmuneDebug.LogError("Couldn't assign Weapon Slot because Slot is NULL");
                        break;
                    }
            }
        }
    }

    public void SetActiveWeaponSlot(LoadoutSlotType slot)
    {
        _currentWeaponSlot = slot;
    }

    public void ShowWeapon(LoadoutSlotType slot)
    {
        _currentWeaponSlot = slot;

        if (MeleeWeapon != null) MeleeWeapon.IsEnabled = (slot == LoadoutSlotType.WeaponMelee);
        if (PrimaryWeapon != null) PrimaryWeapon.IsEnabled = (slot == LoadoutSlotType.WeaponPrimary);
        if (SecondaryWeapon != null) SecondaryWeapon.IsEnabled = (slot == LoadoutSlotType.WeaponSecondary);
        if (TertiaryWeapon != null) TertiaryWeapon.IsEnabled = (slot == LoadoutSlotType.WeaponTertiary);
        if (PickupWeapon != null) PickupWeapon.IsEnabled = (slot == LoadoutSlotType.WeaponPickup);
    }

    public void HideWeapons()
    {
        if (MeleeWeapon != null) MeleeWeapon.IsEnabled = false;
        if (PrimaryWeapon != null) PrimaryWeapon.IsEnabled = false;
        if (SecondaryWeapon != null) SecondaryWeapon.IsEnabled = false;
        if (TertiaryWeapon != null) TertiaryWeapon.IsEnabled = false;
        if (PickupWeapon != null) PickupWeapon.IsEnabled = false;
    }

    public void SetLayers(UberstrikeLayer layer)
    {
        _layer = layer;
        UpdateLayers();
    }

    public void UpdateLayers()
    {
        LayerUtil.SetLayerRecursively(transform, _layer);
    }

    public Transform GetBone(BoneIndex bone)
    {
        return Configuration.GetBone(bone);
    }

    public void SetPosition(Vector3 position, Quaternion rotation)
    {
        transform.localPosition = position;
        transform.localRotation = rotation;
    }

    public void SetFootStep(FootStepSoundType sound)
    {
        _footStep = sound;
    }

    public bool CanPlayFootSound
    {
        get { return _nextFootStepTime < Time.time; }
    }

    public void PlayFootSound(float length)
    {
        if (CanPlayFootSound)
            PlayFootSound(_footStep, length);
    }

    public void PlayFootSound(FootStepSoundType sound, float length)
    {
        switch (sound)
        {
            case FootStepSoundType.Dive: length *= DiveFootstep; break;
            case FootStepSoundType.Swim: length *= SwimFootstep; break;
            case FootStepSoundType.Water: length *= WaterFootstep; break;
        }

        _nextFootStepTime = Time.time + length;

        SfxManager.PlayFootStepAudioClip(sound, _transform.position);
    }

    public void PlayDieSound()
    {
        int rand = Random.Range(0, 3);
        SoundEffectType sound = SoundEffectType.PcNormalKill1;

        switch (rand)
        {
            case 0:
                sound = SoundEffectType.PcNormalKill1;
                break;
            case 1:
                sound = SoundEffectType.PcNormalKill2;
                break;
            case 3:
                sound = SoundEffectType.PcNormalKill3;
                break;
        }

        SfxManager.Play3dAudioClip(sound, _transform.position);
    }

    public void DestroyCurrentRagdoll()
    {
        if (CurrentRagdoll)
        {
            AvatarBuilder.Destroy(CurrentRagdoll.gameObject);
            CurrentRagdoll = null;
        }
    }
}
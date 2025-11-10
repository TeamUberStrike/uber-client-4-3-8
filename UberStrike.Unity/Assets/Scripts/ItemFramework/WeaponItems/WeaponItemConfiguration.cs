
using UberStrike.Core.Models.Views;
using UnityEngine;

[System.Serializable]
public class WeaponItemConfiguration : UberStrikeItemWeaponView
{
    [CustomProperty("CriticalStrikeBonus")]
    private int _criticalStrikeBonus = 0;
    public int CriticalStrikeBonus { get { return _criticalStrikeBonus; } set { _criticalStrikeBonus = value; } }

    [CustomProperty("SwitchDelay")]
    private int _switchDelay = 500;
    public int SwitchDelayMilliSeconds { get { return _switchDelay; } set { _switchDelay = value; } }

    [SerializeField]
    private DamageEffectType _damageEffectFlag = DamageEffectType.None;
    [SerializeField]
    private float _damageEffectValue;
    [SerializeField]
    private bool _hasAutomaticFire;
    [SerializeField]
    private ZoomInfo _zoomInformation;
    [SerializeField]
    private Vector3 _ironSightPosition;
    [SerializeField]
    private WeaponSecondaryAction _secondaryAction;
    [SerializeField]
    private bool _showReticleForPrimaryAction = false;
    [SerializeField]
    private ReticuleForSecondaryAction _secondaryActionReticule;

    [SerializeField]
    private CombatRangeCategory _combatRange;
    [SerializeField]
    private int _tier;

    [SerializeField]
    private int _minProjectileDistance = 2;
    [SerializeField]
    private int _maxConcurrentProjectiles = 0;

    [SerializeField]
    private Vector3 _position;
    [SerializeField]
    private Vector3 _rotation;

    [SerializeField]
    private ParticleConfigurationType _impactEffect;
    [SerializeField]
    private WeaponInputHandlerType _inputHandler;

    public int MaxConcurrentProjectiles
    {
        get { return _maxConcurrentProjectiles; }
        set { _maxConcurrentProjectiles = value; }
    }

    public int MinProjectileDistance
    {
        get { return _minProjectileDistance; }
        set { _minProjectileDistance = value; }
    }

    public Vector3 Position
    {
        get { return _position; }
        set { _position = value; }
    }

    public Vector3 Rotation
    {
        get { return _rotation; }
        set { _rotation = value; }
    }

    public bool ShowReticleForPrimaryAction
    {
        get { return _showReticleForPrimaryAction; }
        set { _showReticleForPrimaryAction = value; }
    }

    public ReticuleForSecondaryAction SecondaryActionReticle
    {
        get { return _secondaryActionReticule; }
        set { _secondaryActionReticule = value; }
    }

    public WeaponSecondaryAction SecondaryAction
    {
        get { return _secondaryAction; }
        set { _secondaryAction = value; }
    }

    public Vector3 IronSightPosition
    {
        get { return _ironSightPosition; }
        set { _ironSightPosition = value; }
    }

    public ZoomInfo ZoomInformation
    {
        get { return _zoomInformation; }
        set { _zoomInformation = value; }
    }

    public bool HasAutomaticFire
    {
        get { return _hasAutomaticFire; }
        set { _hasAutomaticFire = value; }
    }

    public DamageEffectType DamageEffectFlag
    {
        get { return _damageEffectFlag; }
        set { _damageEffectFlag = value; }
    }

    public float DamageEffectValue
    {
        get { return _damageEffectValue; }
        set { _damageEffectValue = value; }
    }

    public ParticleConfigurationType ParticleEffect
    {
        get { return _impactEffect; }
        set { _impactEffect = value; }
    }

    public WeaponInputHandlerType InputHandlerType
    {
        get { return _inputHandler; }
        set { _inputHandler = value; }
    }

    public CombatRangeCategory CombatRange
    {
        get { return _combatRange; }
        set { _combatRange = value; }
    }

    public int Tier
    {
        get { return _tier; }
        set { _tier = value; }
    }

    public float DPS
    {
        get
        {
            return RateOfFire != 0 ? (DamagePerProjectile * ProjectilesPerShot) / RateOfFire : 0;
        }
    }
}
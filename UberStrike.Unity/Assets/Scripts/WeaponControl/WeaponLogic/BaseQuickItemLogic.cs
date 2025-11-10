
//using Cmune.Realtime.Common.Utils;
//using UberStrike.Core.Types;
//using UnityEngine;

//public abstract class BaseQuickItemLogic : IWeaponLogic
//{
//    protected BaseQuickItemLogic(QuickItemConfiguration item)
//    {
//        _configuration = new WeaponConfiguration();

//        _configuration.Range = 1000;
//        _configuration.ProjectileSpeed = 25;

//        _weaponID = item.ID;
//        _weaponClass = item.ItemClass;
//    }

//    public abstract void Shoot(Ray ray, out CmunePairList<BaseGameProp, ShotPoint> hits);

//    #region PROPERTIES

//    /// <summary>
//    /// The current decorator that is used to visualize weapon activity
//    /// </summary>
//    public BaseWeaponDecorator Decorator
//    {
//        get { return _decorator; }
//        set { _decorator = value; }
//    }

//    /// <summary>
//    /// The current confiuration values of the weapon
//    /// </summary>
//    public WeaponConfiguration Config
//    {
//        get { return _configuration; }
//        set { _configuration = value; }
//    }

//    /// <summary>
//    /// This value is true if the time span between now and the last shot fired
//    /// is greater than the fire rate of the weapon.
//    /// </summary>
//    public bool IsWeaponReady
//    {
//        get { return _isReady; }
//    }

//    /// <summary>
//    /// This value is true if the weapon is currently activated and ready to use,
//    /// and not reloading.
//    /// </summary>
//    public bool IsWeaponActive
//    {
//        get { return _isActive; }
//        set
//        {
//            if (_isActive != value)
//            {
//                _isActive = value;
//                //OnWeaponEnabled(value);
//            }
//        }
//    }

//    public WeaponSecondaryAction SecondaryAction
//    {
//        get;
//        private set;
//    }

//    //protected virtual void OnWeaponEnabled(bool enabled) { }

//    public ushort IncrementProjectileId()
//    {
//        return ++_shotCount;
//    }

//    public ushort CurrentProjectileId
//    {
//        get { return _shotCount; }
//    }

//    public bool IsSimulation
//    {
//        get { return _isSimulation; }
//        set { _isSimulation = value; }
//    }

//    public int WeaponID
//    {
//        get { return _weaponID; }
//    }

//    public virtual float HitDelay
//    {
//        get { return 0; }
//    }

//    public UberstrikeItemClass WeaponClass
//    {
//        get { return _weaponClass; }
//    }

//    #endregion

//    #region FIELDS
//    private bool _isActive = true;
//    private bool _isReady = true;
//    private bool _isSimulation = false;
//    private WeaponConfiguration _configuration;
//    private BaseWeaponDecorator _decorator;
//    private ushort _shotCount = 0;
//    private int _weaponID;
//    private UberstrikeItemClass _weaponClass;
//    #endregion
//}

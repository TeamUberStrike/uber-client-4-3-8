
using System;
using Cmune.Realtime.Common.Utils;
using UnityEngine;

public abstract class BaseWeaponLogic : IWeaponLogic
{
    public event Action<CmunePairList<BaseGameProp, ShotPoint>> OnTargetHit;

    #region Properties

    /// <summary>
    /// 
    /// </summary>
    public IWeaponController Controller { get; private set; }

    /// <summary>
    /// The current confiuration values of the weapon
    /// </summary>
    public WeaponItemConfiguration Config { get; private set; }

    /// <summary>
    /// The current decorator that is used to visualize weapon activity
    /// </summary>
    public abstract BaseWeaponDecorator Decorator { get; }

    public virtual float HitDelay { get { return 0; } }

    /// <summary>
    /// This value is true if the time span between now and the last shot fired
    /// is greater than the fire rate of the weapon.
    /// </summary>
    public bool IsWeaponReady { get; private set; }

    /// <summary>
    /// This value is true if the weapon is currently activated and ready to use,
    /// and not reloading.
    /// </summary>
    public bool IsWeaponActive { get; set; }

    #endregion

    protected BaseWeaponLogic(WeaponItem item, IWeaponController controller)
    {
        Controller = controller;

        Config = item.Configuration;

        //Config = new WeaponConfiguration()
        //{
        //    Damage = (short)item.Configuration.DamagePerProjectile,
        //    SplashRadius = item.Configuration.SplashRadius / 100f,
        //    AccuracySpread = new Vector2(item.Configuration.AccuracySpread, item.Configuration.AccuracySpread) / 10f,
        //    RateOfFire = item.Configuration.RateOfFire / 1000f,
        //    Range = 1000,
        //    Force = item.Configuration.DamageKnockback,
        //    RecoilKickback = item.Configuration.RecoilKickback,
        //    RecoilMovement = item.Configuration.RecoilMovement / 100f,
        //    ProjectileSpeed = item.Configuration.ProjectileSpeed,
        //    DamageEffectFlag = item.Configuration.DamageEffectFlag,
        //    DamageEffectValue = item.Configuration.DamageEffectValue,

        //    WeaponId = item.ItemId,
        //    WeaponClass = item.ItemClass,
        //};
    }

    public abstract void Shoot(Ray ray, out CmunePairList<BaseGameProp, ShotPoint> hits);

    protected void OnHits(CmunePairList<BaseGameProp, ShotPoint> hits)
    {
        if (OnTargetHit != null)
            OnTargetHit(hits);
    }
}
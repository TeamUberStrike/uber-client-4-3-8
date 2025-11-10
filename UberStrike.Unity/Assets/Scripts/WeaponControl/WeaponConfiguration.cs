
using System;
using UnityEngine;
using UberStrike.DataCenter.Common.Entities;
using UberStrike.Core.Types;

[Serializable]
public class WeaponConfiguration
{
    public int WeaponId;
    public UberstrikeItemClass WeaponClass;

    public int Damage;
    public float SplashRadius;
    public int Force;
    public float ReloadTime;
    public int RecoilKickback;
    public float RecoilMovement;
    public Vector2 AccuracySpread;
    public float RateOfFire;
    public int Range;
    public int ProjectileSpeed;
    public Vector2 ZoomLimits;

    public DamageEffectType DamageEffectFlag;
    public float DamageEffectValue;
}
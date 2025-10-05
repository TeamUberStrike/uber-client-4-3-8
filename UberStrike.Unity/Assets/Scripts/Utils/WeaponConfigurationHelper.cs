
using UberStrike.Core.Models.Views;
using UnityEngine;

public static class WeaponConfigurationHelper
{
    static WeaponConfigurationHelper()
    {
        MaxSplashRadius = 1;
        MaxRecoilKickback = 1;
        MaxRateOfFire = 1;
        MaxProjectileSpeed = 1;
        MaxAccuracySpread = 1;
        MaxDamage = 1;
        MaxAmmo = 1;
    }

    public static void UpdateWeaponStatistics(UberStrikeItemShopClientView shopView)
    {
        MaxSplashRadius = 1;
        MaxRecoilKickback = 1;
        MaxRateOfFire = 1;
        MaxProjectileSpeed = 1;
        MaxAccuracySpread = 1;
        MaxDamage = 1;
        MaxAmmo = 1;

        foreach (UberStrikeItemWeaponView weaponView in shopView.WeaponItems)
        {
            MaxAmmo = Mathf.Max(Mathf.RoundToInt(weaponView.MaxAmmo * 1.1f), MaxAmmo);
            MaxSplashRadius = Mathf.Max(Mathf.RoundToInt(weaponView.SplashRadius * 1.1f), MaxSplashRadius);
            MaxRecoilKickback = Mathf.Max(Mathf.RoundToInt(weaponView.RecoilKickback * 1.1f), MaxRecoilKickback);
            MaxRateOfFire = Mathf.Max(Mathf.RoundToInt(weaponView.RateOfFire * 1.1f), MaxRateOfFire);
            MaxProjectileSpeed = Mathf.Max(Mathf.RoundToInt(weaponView.ProjectileSpeed * 1.1f), MaxProjectileSpeed);
            MaxAccuracySpread = Mathf.Max(Mathf.RoundToInt(weaponView.AccuracySpread * 1.1f), MaxAccuracySpread);
            MaxDamage = Mathf.Max(Mathf.RoundToInt(weaponView.DamagePerProjectile * weaponView.ProjectilesPerShot * 1.1f), MaxDamage);
        }

        MaxSplashRadius /= 100f;
        MaxRateOfFire /= 1000f;
        MaxAccuracySpread /= 10;
    }

    public static float MaxAmmo { get; private set; }
    public static float MaxDamage { get; private set; }
    public static float MaxAccuracySpread { get; private set; }
    public static float MaxProjectileSpeed { get; private set; }
    public static float MaxRateOfFire { get; private set; }
    public static float MaxRecoilKickback { get; private set; }
    public static float MaxSplashRadius { get; private set; }

    public static float GetAmmoNormalized(UberStrikeItemWeaponView view) { return view != null ? view.MaxAmmo / WeaponConfigurationHelper.MaxAmmo : 0; }
    public static float GetDamageNormalized(UberStrikeItemWeaponView view) { return view != null ? view.DamagePerProjectile * view.ProjectilesPerShot / WeaponConfigurationHelper.MaxDamage : 0; }
    public static float GetAccuracySpreadNormalized(UberStrikeItemWeaponView view) { return view != null ? view.AccuracySpread / 10f / WeaponConfigurationHelper.MaxAccuracySpread : 0; }
    public static float GetProjectileSpeedNormalized(UberStrikeItemWeaponView view) { return view != null ? view.ProjectileSpeed / WeaponConfigurationHelper.MaxProjectileSpeed : 0; }
    public static float GetRateOfFireNormalized(UberStrikeItemWeaponView view) { return view != null ? view.RateOfFire / 1000f / WeaponConfigurationHelper.MaxRateOfFire : 0; }
    public static float GetRecoilKickbackNormalized(UberStrikeItemWeaponView view) { return view != null ? view.RecoilKickback / WeaponConfigurationHelper.MaxRecoilKickback : 0; }
    public static float GetSplashRadiusNormalized(UberStrikeItemWeaponView view) { return view != null ? view.SplashRadius / 100f / WeaponConfigurationHelper.MaxSplashRadius : 0; }

    public static float GetAmmo(UberStrikeItemWeaponView view) { return view != null ? view.MaxAmmo : 0; }
    public static float GetDamage(UberStrikeItemWeaponView view) { return view != null ? view.DamagePerProjectile * view.ProjectilesPerShot : 0; }
    public static float GetAccuracySpread(UberStrikeItemWeaponView view) { return view != null ? view.AccuracySpread / 10f : 0; }
    public static float GetProjectileSpeed(UberStrikeItemWeaponView view) { return view != null ? view.ProjectileSpeed : 0; }
    public static float GetRateOfFire(UberStrikeItemWeaponView view) { return view != null ? view.RateOfFire / 1000f : 1; }
    public static float GetRecoilKickback(UberStrikeItemWeaponView view) { return view != null ? view.RecoilKickback : 0; }
    public static float GetRecoilMovement(UberStrikeItemWeaponView view) { return view != null ? view.RecoilMovement / 100f : 0; }
    public static float GetSplashRadius(UberStrikeItemWeaponView view) { return view != null ? view.SplashRadius / 100f : 0; }
    public static float GetCriticalStrikeBonus(WeaponItemConfiguration view) { return view != null ? view.CriticalStrikeBonus / 100f : 0; }

}
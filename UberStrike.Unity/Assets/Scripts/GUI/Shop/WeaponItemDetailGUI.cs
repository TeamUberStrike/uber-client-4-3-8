using UberStrike.Core.Types;
using UnityEngine;

public class WeaponItemDetailGUI : IBaseItemDetailGUI
{
    private WeaponItem _item;

    public WeaponItemDetailGUI(WeaponItem item)
    {
        _item = item;
    }

    public void Draw()
    {
        //float weaponMaxDamage = (float)(_item.Configuration.DamagePerProjectile * _item.Configuration.ProjectilesPerShot) / (float)WeaponConfigurationHelper.MaxDamage;
        //float rateOfFire = 1 - ((float)_item.Configuration.RateOfFire / (float)WeaponConfigurationHelper.MaxRateOfFire);
        //float missileVelocity = (float)_item.Configuration.ProjectileSpeed / (float)WeaponConfigurationHelper.MaxProjectileSpeed;
        //float splashDamage = (float)_item.Configuration.SplashRadius / (float)WeaponConfigurationHelper.MaxSplashRadius;
        //float accuracy = 1 - ((float)_item.Configuration.AccuracySpread / (float)WeaponConfigurationHelper.MaxAccuracySpread);
        //float recoil = (float)_item.Configuration.RecoilKickback / (float)WeaponConfigurationHelper.MaxRecoilKickback;

        GUITools.ProgressBar(new Rect(14, 95, 165, 12), LocalizedStrings.Damage, WeaponConfigurationHelper.GetDamageNormalized(_item.Configuration), ColorScheme.ProgressBar, 64, WeaponConfigurationHelper.GetDamage(_item.Configuration).ToString("F0"));
        GUITools.ProgressBar(new Rect(14, 111, 165, 12), LocalizedStrings.RateOfFire, WeaponConfigurationHelper.GetRateOfFireNormalized(_item.Configuration), ColorScheme.ProgressBar, 64, WeaponConfigurationHelper.GetRateOfFire(_item.Configuration).ToString("F0"));

        if (_item.ItemClass == UberstrikeItemClass.WeaponCannon || _item.ItemClass == UberstrikeItemClass.WeaponLauncher || _item.ItemClass == UberstrikeItemClass.WeaponSplattergun)
        {
            GUITools.ProgressBar(new Rect(175, 95, 165, 12), LocalizedStrings.Velocity, WeaponConfigurationHelper.GetProjectileSpeedNormalized(_item.Configuration), ColorScheme.ProgressBar, 64, WeaponConfigurationHelper.GetProjectileSpeed(_item.Configuration).ToString("F0"));
            GUITools.ProgressBar(new Rect(175, 111, 165, 12), LocalizedStrings.Impact, WeaponConfigurationHelper.GetSplashRadiusNormalized(_item.Configuration), ColorScheme.ProgressBar, 64, WeaponConfigurationHelper.GetSplashRadius(_item.Configuration).ToString("F1"));
        }
        else if (_item.ItemClass == UberstrikeItemClass.WeaponMelee)
        {
            bool guiEnabled = GUI.enabled;
            GUI.enabled = false;
            GUITools.ProgressBar(new Rect(175, 95, 165, 12), LocalizedStrings.Accuracy, 0.0f, ColorScheme.ProgressBar, 64, string.Empty);
            GUITools.ProgressBar(new Rect(175, 111, 165, 12), LocalizedStrings.Recoil, 0.0f, ColorScheme.ProgressBar, 64, string.Empty);
            GUI.enabled = guiEnabled;
        }
        else
        {
            GUITools.ProgressBar(new Rect(175, 95, 165, 12), LocalizedStrings.Accuracy, WeaponConfigurationHelper.GetAccuracySpreadNormalized(_item.Configuration), ColorScheme.ProgressBar, 64, (WeaponConfigurationHelper.GetAccuracySpread(_item.Configuration) * 100).ToString("F0") + "%");
            GUITools.ProgressBar(new Rect(175, 111, 165, 12), LocalizedStrings.Recoil, WeaponConfigurationHelper.GetRecoilKickbackNormalized(_item.Configuration), ColorScheme.ProgressBar, 64, WeaponConfigurationHelper.GetRecoilKickback(_item.Configuration).ToString());
        }
    }
}
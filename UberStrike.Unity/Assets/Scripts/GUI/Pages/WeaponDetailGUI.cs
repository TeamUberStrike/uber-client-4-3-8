using Cmune.Util;
using UberStrike.Core.Types;
using UberStrike.Realtime.Common;
using UnityEngine;

class WeaponDetailGUI
{
    public void SetWeaponItem(IUnityItem item, RecommendType type)
    {
        _selectedItem = item;
        _curRecomType = type;
#if !UNITY_ANDROID && !UNITY_IPHONE
        if (_curBadge != null)
        {
            _curBadge.Stop();
        }
        _curBadge = UberstrikeIcons.GetRecommendBadge(type);
        _curBadge.Play();
#endif
    }

    public void Draw(Rect rect)
    {
        Rect detailRect = new Rect(rect.x, rect.y, rect.width, rect.height - 2);

        GUI.BeginGroup(detailRect, GUIContent.none, StormFront.GrayPanelBox);
        {
            DrawWeaponBadge(new Rect((rect.width - 180) / 2, 15, 180, 125));
            DrawWeaponCaption(new Rect(0, 2, rect.width, 20.0f));
            DrawWeaponIcons(new Rect(25, 140, rect.width - 50, 30));
            DrawWeaponPropertyBars(new Rect(-25, 175, rect.width, rect.height - 60));
        }
        GUI.EndGroup();
    }

    #region Private

    private void DrawWeaponCaption(Rect rect)
    {
        GUI.color = ColorConverter.HexToColor("ffc41b");
        GUI.Label(rect, ShopUtils.GetRecommendationString(_curRecomType), BlueStonez.label_interparkbold_16pt);
        GUI.color = Color.white;
    }

    private void DrawWeaponBadge(Rect rect)
    {
#if !UNITY_ANDROID && !UNITY_IPHONE
        if (_curBadge != null)
        {
            GUI.DrawTexture(rect, _curBadge);
        }
#endif
    }

    private void DrawWeaponIcons(Rect rect)
    {
        GUI.BeginGroup(rect);
        if (_selectedItem is WeaponItem)
        {
            WeaponItem item = _selectedItem as WeaponItem;
            DrawWeaponIcon(new Rect(0, 0, 50, rect.height), UberstrikeIcons.GetIconForItemClass(item.ItemClass));
            GUI.Label(new Rect(49, 0, 2, rect.height), GUIContent.none, BlueStonez.vertical_line_grey95);
            GUI.DrawTexture(new Rect(75 - 15, rect.height / 2 - 16, 32, 32), CombatRangeIcon.Instance.GetIconByRange(item.Configuration.CombatRange));
            GUI.Label(new Rect(99, 0, 2, rect.height), GUIContent.none, BlueStonez.vertical_line_grey95);
            DrawWeaponIcon(new Rect(100, 0, 50, rect.height), UberstrikeIcons.LevelLock);
            if (item.ItemView != null)
            {
                GUI.Label(new Rect(98, 12, 50, 16), item.ItemView.LevelLock.ToString(), BlueStonez.label_interparkbold_13pt);
            }
        }
        else if (_selectedItem is GearItem)
        {
            GearItem item = _selectedItem as GearItem;
            DrawWeaponIcon(new Rect(25, 0, 50, rect.height), UberstrikeIcons.GetIconForItemClass(item.ItemClass));
            GUI.Label(new Rect(74, 0, 2, rect.height), GUIContent.none, BlueStonez.vertical_line_grey95);
            DrawWeaponIcon(new Rect(75, 0, 50, rect.height), UberstrikeIcons.LevelLock);
            if (item.ItemView != null)
            {
                GUI.Label(new Rect(73, 12, 50, 16), item.ItemView.LevelLock.ToString(), BlueStonez.label_interparkbold_13pt);
            }
        }
        GUI.EndGroup();
    }

    private void DrawWeaponIcon(Rect rect, Texture iconTexture)
    {
        //GUI.Label(rect, "", BlueStonez.buttondark_medium);
        GUI.Label(new Rect(rect.x + rect.width / 2 - 13, rect.y + rect.height / 2 - 14, 35, rect.height), iconTexture);
    }

    private void DrawWeaponPropertyBars(Rect rect)
    {
        int barWidth = 60;
        float barHeight = 12.0f;
        float barGap = 2.0f;
        GUI.BeginGroup(rect);
        if (_selectedItem is WeaponItem)
        {
            WeaponItem item = _selectedItem as WeaponItem;

            GUITools.ProgressBar(new Rect(0, (barHeight + barGap) * 2, rect.width, barHeight), LocalizedStrings.Damage, WeaponConfigurationHelper.GetDamageNormalized(item.Configuration),
                ColorScheme.ProgressBar, barWidth, WeaponConfigurationHelper.GetDamage(item.Configuration).ToString("F0"));
            GUITools.ProgressBar(new Rect(0, (barHeight + barGap) * 3, rect.width, barHeight), LocalizedStrings.RateOfFire,
                 WeaponConfigurationHelper.GetRateOfFireNormalized(item.Configuration), ColorScheme.ProgressBar, barWidth, WeaponConfigurationHelper.GetRateOfFire(item.Configuration).ToString("F0"));

            if (item.ItemClass == UberstrikeItemClass.WeaponCannon ||
                item.ItemClass == UberstrikeItemClass.WeaponLauncher ||
                item.ItemClass == UberstrikeItemClass.WeaponSplattergun)
            {
                GUITools.ProgressBar(new Rect(0, 0, rect.width, barHeight), LocalizedStrings.Velocity,
                    WeaponConfigurationHelper.GetProjectileSpeedNormalized(item.Configuration), ColorScheme.ProgressBar, barWidth, WeaponConfigurationHelper.GetProjectileSpeed(item.Configuration).ToString("F0"));
                GUITools.ProgressBar(new Rect(0, barHeight + barGap, rect.width, barHeight), LocalizedStrings.Impact,
                    WeaponConfigurationHelper.GetSplashRadiusNormalized(item.Configuration), ColorScheme.ProgressBar, barWidth, WeaponConfigurationHelper.GetSplashRadius(item.Configuration).ToString("F1"));
            }
            else if (item.ItemClass == UberstrikeItemClass.WeaponMelee)
            {
                bool guiEnabled = GUI.enabled;
                GUI.enabled = false;
                GUITools.ProgressBar(new Rect(0, 0, rect.width, barHeight), LocalizedStrings.Accuracy,
                    0, ColorScheme.ProgressBar, barWidth, string.Empty);
                GUITools.ProgressBar(new Rect(0, barHeight + barGap, rect.width, barHeight), LocalizedStrings.Recoil,
                    0, ColorScheme.ProgressBar, barWidth, string.Empty);
                GUI.enabled = guiEnabled;
            }
            else
            {
                GUITools.ProgressBar(new Rect(0, 0, rect.width, barHeight), LocalizedStrings.Accuracy,
                     WeaponConfigurationHelper.GetAccuracySpreadNormalized(item.Configuration), ColorScheme.ProgressBar, barWidth, (WeaponConfigurationHelper.GetAccuracySpread(item.Configuration) * 100).ToString("F0"));
            }
        }
        else if (_selectedItem is GearItem)
        {
            GearItem item = _selectedItem as GearItem;
            float armorAbsorption = (float)item.Configuration.ArmorAbsorptionPercent / 100f;

            GUI.DrawTexture(new Rect(50, 0, 32, 32), UberstrikeIcons.ItemArmorPoints);
            GUI.contentColor = Color.black;
            GUI.Label(new Rect(50, 0, 32, 32), item.Configuration.ArmorPoints.ToString(), BlueStonez.label_interparkbold_16pt);
            GUI.contentColor = Color.white;
            GUI.Label(new Rect(87, 0, 32, 32), "AP", BlueStonez.label_interparkbold_18pt_left);

            GUITools.ProgressBar(new Rect(0, 37, rect.width, 15), LocalizedStrings.Absorption, armorAbsorption, ColorScheme.ProgressBar, barWidth, string.Empty);
            GUI.Label(new Rect(rect.width - 25, 37, 25, 15), CmunePrint.Percent(armorAbsorption), BlueStonez.label_interparkmed_10pt_left);
        }
        GUI.EndGroup();
    }

    private void OnSelectionChange(IUnityItem item)
    {
#if !UNITY_ANDROID && !UNITY_IPHONE

        if (_curBadge != null)
        {
            _curBadge.Stop();
        }
        _curBadge = UberstrikeIcons.GetAchievementBadge(AchievementType.CostEffective);
        _curBadge.Play();
#endif
    }

    private IUnityItem _selectedItem;
#if !UNITY_ANDROID && !UNITY_IPHONE
    private MovieTexture _curBadge;
#endif
    private RecommendType _curRecomType;
    #endregion
}
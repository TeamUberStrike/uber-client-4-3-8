
using System;
using Cmune.Util;
using UberStrike.Core.Models.Views;
using UberStrike.Core.Types;
using UnityEngine;
using Cmune.DataCenter.Common.Entities;

public enum PopupViewSide
{
    Left,
    Right,
    Top,
    Bottom,
}

public class ItemToolTip
{
    private const int TextWidth = 80;

    private FloatPropertyBar _ammo = new FloatPropertyBar("Ammo");
    private FloatPropertyBar _damage = new FloatPropertyBar("Damage");
    private FloatPropertyBar _fireRate = new FloatPropertyBar("Rate of Fire");
    private FloatPropertyBar _accuracy = new FloatPropertyBar("Accuracy");
    private FloatPropertyBar _velocity = new FloatPropertyBar("Velocity");
    private FloatPropertyBar _damageRadius = new FloatPropertyBar("Radius");
    private FloatPropertyBar _defenseBonus = new FloatPropertyBar("Defense Bonus");
    private FloatPropertyBar _armorCarried = new FloatPropertyBar("Armor Carried");

    private Texture2D _icon;
    private string _name;
    private int _level;
    private int _daysLeft;
    private int _criticalHit;
    private string _description;
    private IUnityItem _item;
    private Rect _cacheRect;
    private float _alpha = 0;
    private BuyingDurationType _duration;
    private int _startAmmo;

    private readonly Vector2 Size = new Vector2(260, 240);

    private Action OnDrawItemDetails = () => { };

    private Action OnDrawTip = () => { };

    public bool IsEnabled { get; set; }

    private float Alpha { get { return Mathf.Clamp01(_alpha - Time.time); } }

    Rect _finalRect = new Rect(0, 0, 260, 230);
    Rect _rect = new Rect(0, 0, 260, 230);

    public void OnGui()
    {
        _rect = _rect.Lerp(_finalRect, Time.deltaTime * 5);

        if (IsEnabled)
        {
            GUI.color = new Color(1, 1, 1, Alpha);

            GUI.BeginGroup(_rect, BlueStonez.box_grey_outlined);
            {
                if (_icon)
                {
                    GUI.DrawTexture(new Rect(20, 10, 48, 48), _icon);
                }

                GUI.Label(new Rect(75, 15, 200, 30), _name, BlueStonez.label_interparkbold_13pt_left);
                GUI.Label(new Rect(20, 70, 220, 50), _description, BlueStonez.label_interparkmed_11pt_left);

                if (_duration != BuyingDurationType.None)
                {
                    GUIContent content = new GUIContent(ShopUtils.PrintDuration(_duration), UberstrikeIcons.ItemExpiration);
                    GUI.Label(new Rect(75, 40, 200, 20), content, BlueStonez.label_interparkbold_11pt_left);
                }
                else if (_daysLeft == 0)
                {
                    GUIContent content = new GUIContent(ShopUtils.PrintDuration(BuyingDurationType.Permanent), UberstrikeIcons.ItemExpiration);
                    GUI.Label(new Rect(75, 40, 200, 20), content, BlueStonez.label_interparkmed_11pt_left);
                }
                else if (_daysLeft > 0)
                {
                    GUIContent content = new GUIContent(string.Format(" {0} Days left", _daysLeft), UberstrikeIcons.ItemExpiration);
                    GUI.Label(new Rect(75, 40, 200, 20), content, BlueStonez.label_interparkbold_11pt_left);
                }

                OnDrawItemDetails();

                if (_level > 1)
                {
                    GUI.Label(new Rect(20, 200, 210, 20), "Level Required: " + _level, BlueStonez.label_interparkbold_11pt_left);
                }
                if (_criticalHit > 0)
                {
                    GUI.Label(new Rect(20, 215, 210, 20), "Critical Hit Bonus: " + _criticalHit + "%", BlueStonez.label_interparkmed_11pt_left);
                }
            }
            GUI.EndGroup();

            //draw tip
            OnDrawTip();

            GUI.color = Color.white;

            if (_alpha - Time.time < 0)
            {
                IsEnabled = false;
            }
        }
    }

    public void SetItem(IUnityItem item, Rect bounds, PopupViewSide side, int daysLeft = -1, BuyingDurationType duration = BuyingDurationType.None)
    {
        if (Event.current.type != EventType.Repaint || item == null || ItemManager.Instance.IsDefaultGearItem(item.ItemId)) return;

        //reset position hard if the tooltip window was nearly invisible when the next item is selected
        bool _hardSetPosition = _alpha < Time.time + 0.1f;

        _alpha = Mathf.Lerp(_alpha, Time.time + 1.1f, Time.deltaTime * 12);

        if (_item != item || _cacheRect != bounds || !IsEnabled)
        {
            _cacheRect = bounds;
            bounds = GUITools.ToGlobal(bounds);

            IsEnabled = true;

            _item = item;
            _icon = item.Icon;
            _name = item.Name;

            _level = item.ItemView != null ? item.ItemView.LevelLock : 0;
            _description = item.ItemView != null ? item.ItemView.Description : "";
            _daysLeft = daysLeft;
            _criticalHit = 0;
            _duration = duration;

            switch (side)
            {
                case PopupViewSide.Left:
                    {
                        float tipPosition = bounds.y - 10 + bounds.height * 0.5f;
                        Rect target = new Rect(bounds.x - Size.x - 9, bounds.y - Size.y * 0.5f, Size.x, Size.y);
                        Rect tip = new Rect(target.xMax - 1, tipPosition, 12, 21);

                        if (target.y <= GlobalUIRibbon.HEIGHT)
                        {
                            target.y += (GlobalUIRibbon.HEIGHT - target.y);
                        }

                        if (target.yMax >= Screen.height)
                        {
                            target.y -= (target.yMax - Screen.height);
                        }

                        //try not to move the window, if the tip doesn't exit the bounds
                        if (tip.y < _finalRect.y || tip.yMax > _finalRect.yMax || _finalRect.x != target.x)
                        {
                            _finalRect = target;
                            if (_hardSetPosition)
                                _rect = target;
                        }

                        OnDrawTip = () => GUI.DrawTexture(new Rect(_rect.xMax - 1, tipPosition, 12, 21), ConsumableHudTextures.TooltipRight);
                    } break;
                case PopupViewSide.Top:
                    {
                        float tipPosition = bounds.x - 10 + bounds.width * 0.5f;
                        Rect target = new Rect(bounds.x + (bounds.width - Size.x) * 0.5f, bounds.y - Size.y - 9, Size.x, Size.y);
                        Rect tip = new Rect(tipPosition, target.yMax - 1, 21, 12);

                        if (target.xMin <= 10)
                        {
                            target.x = 10;
                        }

                        if (target.xMax >= (Screen.width - 10))
                        {
                            target.x -= (target.xMax - Screen.width + 10);
                        }

                        //try not to move the window, if the tip doesn't exit the bounds
                        if (tip.x < _finalRect.x || tip.xMax > _finalRect.xMax || _finalRect.y != target.y)
                        {
                            _finalRect = target;
                            if (_hardSetPosition)
                                _rect = target;
                        }
                        OnDrawTip = () => GUI.DrawTexture(new Rect(tipPosition, _rect.yMax - 1, 21, 12), ConsumableHudTextures.TooltipDown);
                    } break;
            }

            //configure the detail draw routine and data
            switch (item.ItemClass)
            {
                case UberstrikeItemClass.GearBoots:
                case UberstrikeItemClass.GearFace:
                case UberstrikeItemClass.GearGloves:
                case UberstrikeItemClass.GearHead:
                case UberstrikeItemClass.GearHolo:
                case UberstrikeItemClass.GearLowerBody:
                case UberstrikeItemClass.GearUpperBody:
                    OnDrawItemDetails = DrawGear;
                    {
                        _defenseBonus.Value = ((UberStrikeItemGearView)item.ItemView).ArmorAbsorptionPercent;
                        _defenseBonus.Max = 25f;
                        _armorCarried.Value = ((UberStrikeItemGearView)item.ItemView).ArmorPoints;
                        _armorCarried.Max = 200f;
                        break;
                    }
                case UberstrikeItemClass.WeaponMelee:
                    {
                        OnDrawItemDetails = DrawMeleeWeapon;
                        var view = item.ItemView as UberStrikeItemWeaponView;
                        if (view != null)
                        {
                            _damage.Value = WeaponConfigurationHelper.GetDamage(view);
                            _damage.Max = WeaponConfigurationHelper.MaxDamage;
                            _fireRate.Value = WeaponConfigurationHelper.GetRateOfFire(view);
                            _fireRate.Max = WeaponConfigurationHelper.MaxRateOfFire;
                        }
                        break;
                    }
                case UberstrikeItemClass.WeaponHandgun:
                case UberstrikeItemClass.WeaponMachinegun:
                case UberstrikeItemClass.WeaponShotgun:
                case UberstrikeItemClass.WeaponSniperRifle:
                    {
                        OnDrawItemDetails = DrawInstantHitWeapon;
                        var view = item.ItemView as WeaponItemConfiguration;
                        if (view != null)
                        {
                            _startAmmo = view.StartAmmo;
                            _ammo.Value = WeaponConfigurationHelper.GetAmmo(view);
                            _ammo.Max = WeaponConfigurationHelper.MaxAmmo;
                            _damage.Value = WeaponConfigurationHelper.GetDamage(view);
                            _damage.Max = WeaponConfigurationHelper.MaxDamage;
                            _fireRate.Value = WeaponConfigurationHelper.GetRateOfFire(view);
                            _fireRate.Max = WeaponConfigurationHelper.MaxRateOfFire;
                            _accuracy.Value = WeaponConfigurationHelper.MaxAccuracySpread - WeaponConfigurationHelper.GetAccuracySpread(view);
                            _accuracy.Max = WeaponConfigurationHelper.MaxAccuracySpread;
                            _criticalHit = view.CriticalStrikeBonus;
                        }

                        //if there is nt specific configuration we set the bonus to 50%
                        if (_criticalHit == 0 && (item.ItemClass == UberstrikeItemClass.WeaponHandgun || item.ItemClass == UberstrikeItemClass.WeaponSniperRifle))
                        {
                            _criticalHit = 50;
                        }
                        break;
                    }
                case UberstrikeItemClass.WeaponLauncher:
                case UberstrikeItemClass.WeaponCannon:
                case UberstrikeItemClass.WeaponSplattergun:
                    {
                        OnDrawItemDetails = DrawProjectileWeapon;
                        var view = item.ItemView as UberStrikeItemWeaponView;
                        if (view != null)
                        {
                            _startAmmo = view.StartAmmo;
                            _ammo.Value = WeaponConfigurationHelper.GetAmmo(view);
                            _ammo.Max = WeaponConfigurationHelper.MaxAmmo;
                            _damage.Value = WeaponConfigurationHelper.GetDamage(view);
                            _damage.Max = WeaponConfigurationHelper.MaxDamage;
                            _fireRate.Value = WeaponConfigurationHelper.GetRateOfFire(view);
                            _fireRate.Max = WeaponConfigurationHelper.MaxRateOfFire;
                            _velocity.Value = WeaponConfigurationHelper.GetProjectileSpeed(view);
                            _velocity.Max = WeaponConfigurationHelper.MaxProjectileSpeed;
                            _damageRadius.Value = WeaponConfigurationHelper.GetSplashRadius(view);
                            _damageRadius.Max = WeaponConfigurationHelper.MaxSplashRadius;
                        }
                        break;
                    }
                case UberstrikeItemClass.QuickUseGeneral:
                case UberstrikeItemClass.QuickUseGrenade:
                case UberstrikeItemClass.QuickUseMine:
                    OnDrawItemDetails = DrawQuickItem;
                    break;
                default:
                    OnDrawItemDetails = () => { };
                    break;
            }
        }
    }

    public void ComparisonOverlay(Rect position, float value, float otherValue)
    {
        float barWidth = position.width - TextWidth - 50;

        float valueWidth = (float)(barWidth - 4) * Mathf.Clamp01(value);
        float difference = (float)(barWidth - 4) * Mathf.Clamp01(Mathf.Abs(value - otherValue));

        GUI.BeginGroup(position);
        {
            if (value < otherValue)
            {
                GUI.color = Color.green.SetAlpha(Alpha * 0.9f);
                GUI.Label(new Rect(TextWidth + 2 + valueWidth, 3, difference, 8), string.Empty, BlueStonez.progressbar_thumb);
            }
            else
            {
                GUI.color = Color.red.SetAlpha(Alpha * 0.9f);
                GUI.Label(new Rect(TextWidth + 2 + valueWidth - difference, 3, difference, 8), string.Empty, BlueStonez.progressbar_thumb);
            }
            GUI.color = new Color(1, 1, 1, Alpha);
        }
        GUI.EndGroup();
    }

    public void ProgressBar(Rect position, string text, float percentage, Color barColor, string value)
    {
        float barWidth = position.width - TextWidth - 50;

        GUI.BeginGroup(position);
        {
            GUI.Label(new Rect(0, 0, TextWidth, 14), text, BlueStonez.label_interparkbold_11pt_left);
            GUI.Label(new Rect(TextWidth, 1, barWidth, 12), GUIContent.none, BlueStonez.progressbar_background);
            GUI.color = barColor.SetAlpha(Alpha);
            GUI.Label(new Rect(TextWidth + 2, 3, (barWidth - 4) * Mathf.Clamp01(percentage), 8), string.Empty, BlueStonez.progressbar_thumb);
            GUI.color = new Color(1, 1, 1, Alpha);
            if (!string.IsNullOrEmpty(value))
            {
                GUI.Label(new Rect(TextWidth + barWidth + 10, 0, 40, 14), value, BlueStonez.label_interparkmed_10pt_left);
            }
        }
        GUI.EndGroup();
    }

    private void DrawGear()
    {
        ProgressBar(new Rect(20, 120, 200, 12), _defenseBonus.Title, _defenseBonus.Percent, ColorScheme.ProgressBar, "+" + CmunePrint.Percent(_defenseBonus.Value / 100f));
        ProgressBar(new Rect(20, 135, 200, 12), _armorCarried.Title, _armorCarried.Percent, ColorScheme.ProgressBar, _armorCarried.Value.ToString("F0") + "AP");
    }

    private void DrawProjectileWeapon()
    {
        bool isComparing = DragAndDrop.Instance.IsDragging &&
            ShopUtils.IsProjectileWeapon(DragAndDrop.Instance.DraggedItem.Item) &&
            DragAndDrop.Instance.DraggedItem.Item.ItemClass == _item.ItemClass;

        ProgressBar(new Rect(20, 120, 200, 12), _damage.Title, _damage.Percent, ColorScheme.ProgressBar, _damage.Value.ToString("F0") + "HP");
        ProgressBar(new Rect(20, 135, 200, 12), _fireRate.Title, 1 - _fireRate.Percent, ColorScheme.ProgressBar, (1f / _fireRate.Value).ToString("F1") + "/s");
        ProgressBar(new Rect(20, 150, 200, 12), _velocity.Title, _velocity.Percent, ColorScheme.ProgressBar, _velocity.Value.ToString("F0") + "m/s");
        ProgressBar(new Rect(20, 165, 200, 12), _damageRadius.Title, _damageRadius.Percent, ColorScheme.ProgressBar, _damageRadius.Value.ToString("F1") + "m");
        ProgressBar(new Rect(20, 180, 200, 12), _ammo.Title, _ammo.Percent, ColorScheme.ProgressBar, _startAmmo + "/" + _ammo.Value.ToString("F0"));

        if (isComparing)
        {
            var weapon = DragAndDrop.Instance.DraggedItem.Item.ItemView as UberStrikeItemWeaponView;

            ComparisonOverlay(new Rect(20, 120, 200, 12), _damage.Percent, WeaponConfigurationHelper.GetDamageNormalized(weapon));
            ComparisonOverlay(new Rect(20, 135, 200, 12), 1 - _fireRate.Percent, 1 - WeaponConfigurationHelper.GetRateOfFireNormalized(weapon));
            ComparisonOverlay(new Rect(20, 150, 200, 12), _velocity.Percent, WeaponConfigurationHelper.GetProjectileSpeedNormalized(weapon));
            ComparisonOverlay(new Rect(20, 165, 200, 12), _damageRadius.Percent, WeaponConfigurationHelper.GetSplashRadiusNormalized(weapon));
        }
    }

    private void DrawInstantHitWeapon()
    {
        bool isComparing = DragAndDrop.Instance.IsDragging &&
            ShopUtils.IsInstantHitWeapon(DragAndDrop.Instance.DraggedItem.Item) &&
            DragAndDrop.Instance.DraggedItem.Item.ItemClass == _item.ItemClass;

        ProgressBar(new Rect(20, 120, 200, 12), _damage.Title, _damage.Percent, ColorScheme.ProgressBar, _damage.Value.ToString("F0") + "HP");
        ProgressBar(new Rect(20, 135, 200, 12), _fireRate.Title, 1 - _fireRate.Percent, ColorScheme.ProgressBar, (1f / _fireRate.Value).ToString("F1") + "/s");
        ProgressBar(new Rect(20, 150, 200, 12), _accuracy.Title, _accuracy.Percent, ColorScheme.ProgressBar, CmunePrint.Percent(_accuracy.Value / _accuracy.Max));
        ProgressBar(new Rect(20, 165, 200, 12), _ammo.Title, _ammo.Percent, ColorScheme.ProgressBar, _startAmmo + "/" + _ammo.Value.ToString("F0"));

        if (isComparing)
        {
            var weapon = DragAndDrop.Instance.DraggedItem.Item.ItemView as UberStrikeItemWeaponView;

            ComparisonOverlay(new Rect(20, 120, 200, 12), _damage.Percent, WeaponConfigurationHelper.GetDamageNormalized(weapon));
            ComparisonOverlay(new Rect(20, 135, 200, 12), 1 - _fireRate.Percent, 1 - WeaponConfigurationHelper.GetRateOfFireNormalized(weapon));
            ComparisonOverlay(new Rect(20, 150, 200, 12), _accuracy.Percent, 1 - WeaponConfigurationHelper.GetAccuracySpreadNormalized(weapon));
        }
    }

    private void DrawMeleeWeapon()
    {
        ProgressBar(new Rect(20, 120, 200, 12), _damage.Title, _damage.Percent, ColorScheme.ProgressBar, _damage.Value.ToString("F0") + "HP");
        ProgressBar(new Rect(20, 135, 200, 12), _fireRate.Title, 1 - _fireRate.Percent, ColorScheme.ProgressBar, (1f / _fireRate.Value).ToString("F1") + "/s");

        if (DragAndDrop.Instance.IsDragging && ShopUtils.IsMeleeWeapon(DragAndDrop.Instance.DraggedItem.Item))
        {
            var weapon = DragAndDrop.Instance.DraggedItem.Item.ItemView as UberStrikeItemWeaponView;
            ComparisonOverlay(new Rect(20, 120, 200, 12), _damage.Percent, WeaponConfigurationHelper.GetDamageNormalized(weapon));
            ComparisonOverlay(new Rect(20, 135, 200, 12), 1 - _fireRate.Percent, 1 - WeaponConfigurationHelper.GetRateOfFireNormalized(weapon));
        }
    }

    private void DrawQuickItem()
    {
        if (_item != null)
        {
            var qi = _item.ItemView as QuickItemConfiguration;

            if (_item.ItemView is HealthBuffConfiguration)
            {
                var hb = _item.ItemView as HealthBuffConfiguration;
                GUI.Label(new Rect(20, 102, 200, 20), "Health: " + hb.GetHealthBonusDescription(), BlueStonez.label_interparkbold_11pt_left);
                GUI.Label(new Rect(20, 117, 200, 20), "Time: " + (hb.IncreaseTimes > 0 ? (hb.IncreaseFrequency * hb.IncreaseTimes / 1000f).ToString("f1") + "s" : "instant"), BlueStonez.label_interparkbold_11pt_left);
            }
            else if (_item.ItemView is AmmoBuffConfiguration)
            {
                var hb = _item.ItemView as AmmoBuffConfiguration;
                GUI.Label(new Rect(20, 102, 200, 20), "Ammo: " + hb.GetAmmoBonusDescription(), BlueStonez.label_interparkbold_11pt_left);
                GUI.Label(new Rect(20, 117, 200, 20), "Time: " + (hb.IncreaseTimes > 0 ? (hb.IncreaseFrequency * hb.IncreaseTimes / 1000f).ToString("f1") + "s" : "instant"), BlueStonez.label_interparkbold_11pt_left);
            }
            else if (_item.ItemView is ArmorBuffConfiguration)
            {
                var hb = _item.ItemView as ArmorBuffConfiguration;
                GUI.Label(new Rect(20, 102, 200, 20), "Armor: " + hb.GetArmorBonusDescription(), BlueStonez.label_interparkbold_11pt_left);
                GUI.Label(new Rect(20, 117, 200, 20), "Time: " + (hb.IncreaseTimes > 0 ? (hb.IncreaseFrequency * hb.IncreaseTimes / 1000f).ToString("f1") + "s" : "instant"), BlueStonez.label_interparkbold_11pt_left);
            }
            else if (_item.ItemView is ExplosiveGrenadeConfiguration)
            {
                var hb = _item.ItemView as ExplosiveGrenadeConfiguration;
                GUI.Label(new Rect(20, 102, 200, 20), "Damage: " + hb.Damage + "HP", BlueStonez.label_interparkbold_11pt_left);
                GUI.Label(new Rect(20, 117, 200, 20), "Radius: " + hb.SplashRadius + "m", BlueStonez.label_interparkbold_11pt_left);
            }
            else if (_item.ItemView is SpringGrenadeConfiguration)
            {
                var hb = _item.ItemView as SpringGrenadeConfiguration;
                GUI.Label(new Rect(20, 102, 200, 20), "Force: " + hb.Force, BlueStonez.label_interparkbold_11pt_left);
                GUI.Label(new Rect(20, 117, 200, 20), "Lifetime: " + hb.LifeTime + "s", BlueStonez.label_interparkbold_11pt_left);
            }

            GUI.Label(new Rect(20, 132, 200, 20), "Warm-up: " + (qi.WarmUpTime > 0 ? (qi.WarmUpTime / 1000f).ToString("f1") + "s" : "instant"), BlueStonez.label_interparkbold_11pt_left);
            GUI.Label(new Rect(20, 147, 200, 20), "Cooldown: " + (qi.CoolDownTime / 1000f).ToString("f1") + "s", BlueStonez.label_interparkbold_11pt_left);

            GUI.Label(new Rect(20, 162, 200, 20), "Uses per Life: " + (qi.UsesPerLife > 0 ? qi.UsesPerLife.ToString() : "unlimited"), BlueStonez.label_interparkbold_11pt_left);
            GUI.Label(new Rect(20, 177, 200, 20), "Uses per Game: " + (qi.UsesPerGame > 0 ? qi.UsesPerGame.ToString() : "unlimited"), BlueStonez.label_interparkbold_11pt_left);
        }
    }

    private class FloatPropertyBar
    {
        private float _value;
        private float _lastValue;
        private float _max = 1;
        private float _time;

        public string Title { get; private set; }

        public float SmoothValue
        {
            get
            {
                return Mathf.Lerp(_lastValue, Value, (Time.time - _time) * 5);
            }
        }

        public float Value
        {
            get { return _value; }
            set
            {
                _lastValue = _value;
                _time = Time.time;
                _value = value;
            }
        }

        public float Percent
        {
            get { return SmoothValue / Max; }
        }

        public float Max
        {
            get { return _max; }
            set { _max = Mathf.Max(value, 1); }
        }

        public FloatPropertyBar(string title)
        {
            Title = title;
        }
    }
}
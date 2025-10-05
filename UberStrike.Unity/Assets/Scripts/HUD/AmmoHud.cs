using Cmune.Util;
using UberStrike.Realtime.Common;
using UnityEngine;

public class AmmoHud : Singleton<AmmoHud>
{
    public bool Enabled
    {
        get { return _entireGroup.IsVisible; }
        set
        {
            if (_entireGroup.IsVisible != value)
            {
                if (value)
                {
                    _entireGroup.Show();
                    CmuneEventHandler.AddListener<ScreenResolutionEvent>(OnScreenResolutionChange);
                }
                else
                {
                    _entireGroup.Hide();
                    CmuneEventHandler.RemoveListener<ScreenResolutionEvent>(OnScreenResolutionChange);
                    TemporaryWeaponHud.Instance.Enabled = false;
                }
            }
        }
    }

    public int Ammo
    {
        get
        {
            return _curAmmo;
        }
        set
        {
            SetRemainingAmmo(value);
        }
    }

    public void Draw()
    {
        if (!WeaponController.Instance.HasAnyWeapon) return;

        _entireGroup.Draw();
    }

    #region Private
    private AmmoHud()
    {
        if (HudAssets.Exists)
        {
            _curAmmo = 0;
            _ammoDigits = new MeshGUIText("", HudAssets.Instance.InterparkBitmapFont, TextAnchor.LowerRight);
            _ammoDigits.NamePrefix = "AM";
            _ammoIcon = new MeshGUIQuad(HudTextures.AmmoBlue);
            _glowBlur = new MeshGUIQuad(HudTextures.WhiteBlur128);
            _glowBlur.Name = "AmmoHudGlow";
            _glowBlur.Depth = 1.0f;
            _ammoGroup = new Animatable2DGroup();
            _entireGroup = new Animatable2DGroup();

            _ammoGroup.Group.Add(_ammoDigits);
            _ammoGroup.Group.Add(_ammoIcon);
            _entireGroup.Group.Add(_ammoGroup);
            _entireGroup.Group.Add(_glowBlur);
            ResetHud();
            Enabled = false;

            CmuneEventHandler.AddListener<OnSetPlayerTeamEvent>(OnTeamChange);
            CmuneEventHandler.AddListener<ScreenResolutionEvent>(OnScreenResolutionChange);
        }
    }

    private void ResetHud()
    {
        ResetStyle();
        ResetTransform();
    }

    private void ResetStyle()
    {
        HudStyleUtility.Instance.SetTeamStyle(_ammoDigits);
        _glowBlur.Color = HudStyleUtility.GLOW_BLUR_BLUE_COLOR;
    }

    private void OnTeamChange(OnSetPlayerTeamEvent ev)
    {
        if (ev.TeamId == TeamID.RED)
        {
            HudStyleUtility.Instance.SetRedStyle(_ammoDigits);
            _ammoIcon.Texture = HudTextures.AmmoRed;
            _glowBlur.Color = HudStyleUtility.GLOW_BLUR_RED_COLOR;
            ResetTransform();
        }
        else
        {
            HudStyleUtility.Instance.SetBlueStyle(_ammoDigits);
            _ammoIcon.Texture = HudTextures.AmmoBlue;
            _glowBlur.Color = HudStyleUtility.GLOW_BLUR_BLUE_COLOR;
            ResetTransform();
        }
    }

    private void OnScreenResolutionChange(ScreenResolutionEvent ev)
    {
        ResetTransform();
    }

    private void ResetTransform()
    {
        _curScaleFactor = 0.65f;
        ResetAmmoTransform();
        ResetBlurTransform();
        _entireGroup.Position = new Vector2(Screen.width * 0.95f, Screen.height * 0.95f);
    }

    private void ResetAmmoTransform()
    {
        float adjustFactor = 0.07f;
        float scaleFactor = Screen.height * 0.03f / _ammoIcon.Texture.height;
        _ammoIcon.Scale = new Vector2(scaleFactor, scaleFactor);
        _ammoIcon.Position = new Vector2(-_ammoIcon.Size.x, -_ammoIcon.Size.y * 0.8f);
        _ammoDigits.Text = _curAmmo.ToString();
        _ammoDigits.Scale = new Vector2(HudStyleUtility.SMALLER_DIGITS_SCALE * _curScaleFactor,
            HudStyleUtility.SMALLER_DIGITS_SCALE * _curScaleFactor);
        _ammoDigits.Position = new Vector2(-_ammoIcon.Size.x - HudStyleUtility.GAP_BETWEEN_TEXT,
            adjustFactor * _ammoDigits.Size.y);
    }

    private void ResetBlurTransform()
    {
        float blurWidth = _ammoGroup.Rect.width * HudStyleUtility.BLUR_WIDTH_SCALE_FACTOR;
        float blurHeight = _ammoGroup.Rect.height * HudStyleUtility.BLUR_HEIGHT_SCALE_FACTOR;
        _glowBlur.Scale = new Vector2(blurWidth / HudTextures.WhiteBlur128.width,
            blurHeight / HudTextures.WhiteBlur128.height);
        _glowBlur.Position = new Vector2((-blurWidth - _ammoGroup.Rect.width) / 2,
            (-blurHeight - _ammoGroup.Rect.height) / 2);
    }

    private void SetRemainingAmmo(int ammo)
    {
        bool needFlicker = ammo > _curAmmo;
        _curAmmo = ammo;
        ResetTransform();
        if (needFlicker)
        {
            _ammoDigits.Flicker(0.1f);
        }
    }

    private float _curScaleFactor;
    private MeshGUIText _ammoDigits;
    private MeshGUIQuad _ammoIcon;
    private MeshGUIQuad _glowBlur;
    private Animatable2DGroup _ammoGroup;
    private Animatable2DGroup _entireGroup;
    private int _curAmmo;
    #endregion
}

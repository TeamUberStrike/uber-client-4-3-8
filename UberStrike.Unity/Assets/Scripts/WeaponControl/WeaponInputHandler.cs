using UnityEngine;
using System.Collections;
using Cmune.Util;

/// <summary>
/// Types of WeaponInputHandlers.
/// </summary>
public enum WeaponInputHandlerType
{
    SemiAutomatic,
    FullAutomatic,

    Ironsight,
    SniperRifle,
    Minigun
}

public abstract class WeaponInputHandler
{
    protected bool _isLocal;
    protected IWeaponLogic _weaponLogic;
    protected bool _isTriggerPulled;
    protected ZoomInfo _zoomInfo;

    protected static void ZoomIn(ZoomInfo zoomInfo, BaseWeaponDecorator weapon, float zoom)
    {
        if (weapon)
        {
            if (!LevelCamera.Instance.IsZoomedIn)
            {
                SfxManager.Play3dAudioClip(SoundEffectType.WeaponSniperScopeIn, weapon.transform.position);
            }
            else if (zoom < 0 && zoomInfo.CurrentMultiplier != zoomInfo.MinMultiplier)
            {
                SfxManager.Play3dAudioClip(SoundEffectType.WeaponSniperZoomIn, weapon.transform.position);
            }
            else if (zoom > 0 && zoomInfo.CurrentMultiplier != zoomInfo.MaxMultiplier)
            {
                SfxManager.Play3dAudioClip(SoundEffectType.WeaponSniperZoomOut, weapon.transform.position);
            }

            zoomInfo.CurrentMultiplier = Mathf.Clamp(zoomInfo.CurrentMultiplier + zoom, zoomInfo.MinMultiplier, zoomInfo.MaxMultiplier);

            LevelCamera.Instance.DoZoomIn(60 / zoomInfo.CurrentMultiplier, 20);
            UserInput.ZoomSpeed = 0.5f;
        }
    }

    protected static void ZoomOut(ZoomInfo zoomInfo, BaseWeaponDecorator weapon)
    {
        LevelCamera.Instance.DoZoomOut(60, 10);
        UserInput.ZoomSpeed = 1;

        if (zoomInfo != null)
            zoomInfo.CurrentMultiplier = zoomInfo.DefaultMultiplier;

        SfxManager.Play3dAudioClip(SoundEffectType.WeaponSniperScopeOut, weapon.transform.position);
    }

    protected WeaponInputHandler(IWeaponLogic logic, bool isLocal)
    {
        _isLocal = isLocal;
        _weaponLogic = logic;
        _isTriggerPulled = false;
    }

    public abstract void OnPrimaryFire(bool pressed);
    public abstract void OnSecondaryFire(bool pressed);
    public abstract void OnPrevWeapon();
    public abstract void OnNextWeapon();
    public abstract void Update();
    public abstract bool CanChangeWeapon();

    public virtual void Stop() { }
}

/// <summary>
/// Semi-automatic firearms fire one shot per single pull of the trigger.
/// </summary>
public class SemiAutoWeaponInputHandler : WeaponInputHandler
{
    public SemiAutoWeaponInputHandler(IWeaponLogic logic, bool isLocal)
        : base(logic, isLocal) { }

    public override void Update() { }

    public override void OnPrimaryFire(bool pressed)
    {
        /* Play tail sound when trigger pulled */
        if (pressed && !_isTriggerPulled)
        {
            if (WeaponController.Instance.Shoot())
                _weaponLogic.Decorator.PostShoot();
        }

        _isTriggerPulled = pressed;
    }

    public override void OnSecondaryFire(bool pressed) { }
    public override void OnPrevWeapon() { }
    public override void OnNextWeapon() { }
    public override bool CanChangeWeapon() { return true; }
}

/// <summary>
/// Fully automatic firearms continue to load and fire ammunition
/// until the trigger is released, the ammunition is exhausted.
/// </summary>
public class FullAutoWeaponInputHandler : WeaponInputHandler
{
    bool _isShooting;

    public FullAutoWeaponInputHandler(IWeaponLogic logic, bool isLocal)
        : base(logic, isLocal) { }

    public override void Update()
    {
        if (_isTriggerPulled)
        {
            if (WeaponController.Instance.Shoot())
                _isShooting = true;
        }

        if (_isShooting && !_isTriggerPulled)
        {
            _isShooting = false;
            _weaponLogic.Decorator.PostShoot();
        }
    }

    public override void OnPrimaryFire(bool pressed)
    {
        _isTriggerPulled = pressed;
    }

    public override void OnSecondaryFire(bool pressed) { }
    public override void OnPrevWeapon() { }
    public override void OnNextWeapon() { }
    public override bool CanChangeWeapon() { return true; }

    public override void Stop()
    {
        _isTriggerPulled = false;
    }
}

public class IronsightInputHandler : FullAutoWeaponInputHandler
{
    protected bool _isIronsight = false;
    protected float _ironSightDelay = 0;

    public IronsightInputHandler(IWeaponLogic logic, bool isLocal, ZoomInfo zoomInfo)
        : base(logic, isLocal)
    {
        _zoomInfo = zoomInfo;
    }

    public override void OnSecondaryFire(bool pressed)
    {
        _isIronsight = pressed;
    }

    public override void Update()
    {
        base.Update();

        UpdateIronsight();

        if (_isIronsight)
        {
            if (!LevelCamera.Instance.IsZoomedIn)
            {
                ZoomIn(_zoomInfo, _weaponLogic.Decorator, 0);
            }
        }
        else if (LevelCamera.Instance.IsZoomedIn)
        {
            ZoomOut(_zoomInfo, _weaponLogic.Decorator);
        }

        if (!_isIronsight && _ironSightDelay > 0)
        {
            _ironSightDelay -= Time.deltaTime;
        }
    }

    public override void Stop()
    {
        base.Stop();

        if (_isIronsight)
        {
            _isIronsight = false;

            if (_isLocal)
                LevelCamera.Instance.ResetZoom();

            if (WeaponFeedbackManager.Instance.IsIronSighted)
                WeaponFeedbackManager.Instance.ResetIronSight();
        }
    }

    public override bool CanChangeWeapon()
    {
        return !_isIronsight && _ironSightDelay <= 0;
    }

    private void UpdateIronsight()
    {
        if (_isIronsight)
        {
            if (!WeaponFeedbackManager.Instance.IsIronSighted)
            {
                //_ironSightDelay = 0.3f;
                WeaponFeedbackManager.Instance.BeginIronSight();
            }
        }
        else
        {
            if (WeaponFeedbackManager.Instance.IsIronSighted)
            {
                WeaponFeedbackManager.Instance.EndIronSight();
            }
        }
    }
}

public class SniperRifleInputHandler : SemiAutoWeaponInputHandler
{
    protected const float ZOOM = 4;

    protected bool _scopeOpen = false;

    protected float _zoom = 0;

    public SniperRifleInputHandler(IWeaponLogic logic, bool isLocal, ZoomInfo zoomInfo)
        : base(logic, isLocal)
    {
        _zoomInfo = zoomInfo;
    }

    public override void OnSecondaryFire(bool pressed)
    {
        _scopeOpen = pressed;
        Update();
    }

    public override void OnPrevWeapon()
    {
        _zoom = -ZOOM;
    }

    public override void OnNextWeapon()
    {
        _zoom = ZOOM;
    }

    public override void Update()
    {
        if (_scopeOpen)
        {
            if (!LevelCamera.Instance.IsZoomedIn || _zoom != 0)
            {
                ZoomIn(_zoomInfo, _weaponLogic.Decorator, _zoom);

                _zoom = 0;

                CmuneEventHandler.Route(new OnCameraZoomInEvent());

                GameState.LocalPlayer.WeaponCamera.SetCameraEnabled(false);
            }
        }
        else if (LevelCamera.Instance.IsZoomedIn)
        {
            GameState.LocalPlayer.WeaponCamera.SetCameraEnabled(true);

            ZoomOut(_zoomInfo, _weaponLogic.Decorator);
        }
    }

    public override bool CanChangeWeapon()
    {
        return !_scopeOpen;
    }

    public override void Stop()
    {
        if (_scopeOpen)
        {
            _scopeOpen = false;

            if (_isLocal)
            {
                LevelCamera.Instance.ResetZoom();

                if (GameState.LocalCharacter.IsAlive)
                    GameState.LocalPlayer.WeaponCamera.SetCameraEnabled(true);
            }
        }
    }
}

public class MinigunInputHandler : FullAutoWeaponInputHandler
{
    protected bool _isGunWarm = false;
    protected bool _isWarmupPlayed = false;

    protected float _warmTime = 0;

    private MinigunWeaponDecorator _decorator;

    public MinigunInputHandler(IWeaponLogic logic, bool isLocal, MinigunWeaponDecorator decorator)
        : base(logic, isLocal)
    {
        _decorator = decorator;
    }

    public override void Update()
    {
        if (!_decorator) return;

        if (_warmTime < _decorator.MaxWarmUpTime)
        {
            if (_isGunWarm || _isTriggerPulled)
            {
                if (!_isWarmupPlayed)
                {
                    _isWarmupPlayed = true;

                    _decorator.PlayWindUpSound(_warmTime);
                }

                _warmTime += Time.deltaTime;

                if (_warmTime >= _decorator.MaxWarmUpTime)
                {
                    _decorator.PlayDuringSound();
                }

                _decorator.SpinWeaponHead();
            }
        }
        else if (_isTriggerPulled)
        {
            WeaponController.Instance.Shoot();
        }
        else if (_isGunWarm)
        {
            _decorator.SpinWeaponHead();
        }

        if (!_isGunWarm && !_isTriggerPulled)
        {
            if (_warmTime > 0)
            {
                _warmTime -= Time.deltaTime;

                if (_warmTime < 0) _warmTime = 0;

                if (_isWarmupPlayed)
                    _decorator.PlayWindDownSound((1 - (_warmTime / _decorator.MaxWarmUpTime)) * _decorator.MaxWarmDownTime);
            }

            _isWarmupPlayed = false;
        }
    }

    public override void OnSecondaryFire(bool pressed)
    {
        _isGunWarm = pressed;
    }

    public override bool CanChangeWeapon()
    {
        return !_isGunWarm;
    }

    public override void Stop()
    {
        base.Stop();

        _warmTime = 0;
        _isGunWarm = false;
        _isWarmupPlayed = false;
        _isTriggerPulled = false;

        if (_decorator)
            _decorator.StopSound();
    }
}

public class ExplosionInputHandler : SemiAutoWeaponInputHandler
{
    public ExplosionInputHandler(IWeaponLogic logic, bool isLocal)
        : base(logic, isLocal) { }

    public override void OnSecondaryFire(bool pressed)
    {
        if (pressed)
        {
            if (_weaponLogic is ProjectileWeapon)
            {
                ProjectileManager.Instance.RemoveAllLimitedProjectiles();
            }
        }
    }
}

using System.Collections.Generic;
using UberStrike.DataCenter.Common.Entities;
using UnityEngine;
using UberStrike.Core.Types;

public class AvatarAnimationManager : AutoMonoBehaviour<AvatarAnimationManager>
{
    private enum AnimationState
    {
        None = 0,
        Idle,
        Melee,
        SmallGun,
        MediumGun,
        HeavyGun,
    }

    private Dictionary<AnimationState, AnimationIndex[]> _homeAnimations = new Dictionary<AnimationState, AnimationIndex[]>();
    private Dictionary<AnimationState, AnimationIndex[]> _shopAnimations = new Dictionary<AnimationState, AnimationIndex[]>();

    private float _nextAnimationTime;
    private Dictionary<AnimationState, AnimationIndex[]> _currentSet;
    private AnimationState _currentState = 0;

    private void Awake()
    {
        AnimationIndex[] idleAnimations = new AnimationIndex[]
        {
            AnimationIndex.HomeNoWeaponIdle,
            AnimationIndex.HomeNoWeaponnLookAround,
            AnimationIndex.HomeNoWeaponRelaxNeck,
        };
        AnimationIndex[] meleeAnimations = new AnimationIndex[]
        {
            AnimationIndex.HomeMeleeIdle,
            AnimationIndex.HomeMeleeCheckWeapon,
            AnimationIndex.HomeMeleeLookAround,
            AnimationIndex.HomeMeleeRelaxNeck,
        };
        AnimationIndex[] smallGunAnimations = new AnimationIndex[]
        {
            AnimationIndex.HomeSmallGunIdle,
            AnimationIndex.HomeSmallGunCheckWeapon,
            AnimationIndex.HomeSmallGunLookAround,
            AnimationIndex.HomeSmallGunRelaxNeck,
        };
        AnimationIndex[] mediumGunAnimations = new AnimationIndex[]
        {
            AnimationIndex.HomeMediumGunIdle,
            AnimationIndex.HomeMediumGunCheckWeapon,
            AnimationIndex.HomeMediumGunLookAround,
            AnimationIndex.HomeMediumGunRelaxNeck,
        };
        AnimationIndex[] largeGunAnimations = new AnimationIndex[]
        {
            AnimationIndex.HomeLargeGunIdle,
            AnimationIndex.HomeLargeGunCheckWeapon,
            AnimationIndex.HomeLargeGunLookAround,
            AnimationIndex.HomeLargeGunRelaxNeck,
            AnimationIndex.HomeLargeGunShakeWeapon,
        };

        _homeAnimations.Add(AnimationState.Idle, idleAnimations);
        _homeAnimations.Add(AnimationState.Melee, meleeAnimations);
        _homeAnimations.Add(AnimationState.SmallGun, smallGunAnimations);
        _homeAnimations.Add(AnimationState.MediumGun, mediumGunAnimations);
        _homeAnimations.Add(AnimationState.HeavyGun, largeGunAnimations);

        _shopAnimations.Add(AnimationState.Idle, idleAnimations);
        _shopAnimations.Add(AnimationState.Melee, new AnimationIndex[] { AnimationIndex.ShopMeleeAimIdle });
        _shopAnimations.Add(AnimationState.SmallGun, new AnimationIndex[] { AnimationIndex.ShopSmallGunAimIdle, AnimationIndex.ShopSmallGunShoot });
        _shopAnimations.Add(AnimationState.MediumGun, new AnimationIndex[] { AnimationIndex.ShopLargeGunAimIdle, AnimationIndex.ShopLargeGunShoot });
        _shopAnimations.Add(AnimationState.HeavyGun, new AnimationIndex[] { AnimationIndex.ShopLargeGunAimIdle, AnimationIndex.ShopLargeGunShoot });
    }

    private void Update()
    {
        if (_currentState != 0 && (!GameState.HasCurrentGame))// || !GameState.CurrentGame.IsRoundRunning || GameState.LocalPlayer.IsDead))
        {
            //time for the next animation
            if (_nextAnimationTime < Time.time)
            {
                PlayAnimation(GetNextAnimation(), _currentState);
            }

            if (GameState.LocalDecorator != null && GameState.LocalDecorator.AnimationController != null)
                GameState.LocalDecorator.AnimationController.UpdateAnimation();
        }
    }

    private AnimationIndex GetNextAnimation()
    {
        AnimationIndex[] anims = _currentSet[_currentState];
        return anims[Random.Range(0, anims.Length)];
    }

    private void PlayAnimation(AnimationIndex nextAnimation, AnimationState state, bool resetAnimations = false)
    {
        if (GameState.LocalDecorator != null && GameState.LocalDecorator.AnimationController != null)
        {
            AnimationInfo info;
            if (GameState.LocalDecorator.AnimationController.TryGetAnimationInfo(nextAnimation, out info))
            {
                GameState.LocalDecorator.AnimationController.TriggerAnimation(info.Index, resetAnimations || _currentState != state);
                _nextAnimationTime = Time.time + info.State.length - 0.1f;
            }
            else
            {
                _nextAnimationTime = Time.time + 1;
            }
        }
        else
        {
            //no avatar loaded yet
            _nextAnimationTime = Time.time + 0.01f;
        }

        _currentState = state;
    }

    public void ResetAnimationState(PageType page)
    {
        SetAnimationState(page, 0, true);
    }

    public void SetAnimationState(PageType page, UberstrikeItemClass type, bool resetAnimations = false)
    {
        if (page == PageType.Shop)
        {
            _currentSet = _shopAnimations;

            switch (type)
            {
                //WEAPONS
                case UberstrikeItemClass.WeaponMelee:
                    PlayAnimation(AnimationIndex.ShopMeleeTakeOut, AnimationState.Melee, resetAnimations);
                    break;
                case UberstrikeItemClass.WeaponHandgun:
                    PlayAnimation(AnimationIndex.ShopSmallGunTakeOut, AnimationState.SmallGun, resetAnimations);
                    break;
                case UberstrikeItemClass.WeaponCannon:
                case UberstrikeItemClass.WeaponLauncher:
                case UberstrikeItemClass.WeaponMachinegun:
                case UberstrikeItemClass.WeaponShotgun:
                case UberstrikeItemClass.WeaponSniperRifle:
                case UberstrikeItemClass.WeaponSplattergun:
                    PlayAnimation(AnimationIndex.ShopLargeGunTakeOut, AnimationState.HeavyGun, resetAnimations);
                    break;

                //GEAR
                case UberstrikeItemClass.GearBoots:
                    PlayAnimation(AnimationIndex.ShopNewBoots, AnimationState.Idle, resetAnimations);
                    break;
                case UberstrikeItemClass.GearFace:
                case UberstrikeItemClass.GearHead:
                    PlayAnimation(AnimationIndex.ShopNewHead, AnimationState.Idle, resetAnimations);
                    break;
                case UberstrikeItemClass.GearGloves:
                    PlayAnimation(AnimationIndex.ShopNewGloves, AnimationState.Idle, resetAnimations);
                    break;
                case UberstrikeItemClass.GearLowerBody:
                    PlayAnimation(AnimationIndex.ShopNewLowerBody, AnimationState.Idle, resetAnimations);
                    break;
                case UberstrikeItemClass.GearUpperBody:
                    PlayAnimation(AnimationIndex.ShopNewUpperBody, AnimationState.Idle, resetAnimations);
                    break;
                case UberstrikeItemClass.GearHolo:
                    PlayAnimation(AnimationIndex.ShopNewUpperBody, AnimationState.Idle, resetAnimations);
                    break;

                default:
                    if (_currentState == AnimationState.Melee)
                        PlayAnimation(AnimationIndex.ShopHideMelee, AnimationState.Idle, resetAnimations);
                    else if (_currentState == AnimationState.SmallGun || _currentState == AnimationState.MediumGun || _currentState == AnimationState.HeavyGun)
                        PlayAnimation(AnimationIndex.ShopHideGun, AnimationState.Idle, resetAnimations);
                    else
                        _currentState = AnimationState.Idle;
                    break;
            }
        }
        else
        {
            _currentSet = _homeAnimations;
            _nextAnimationTime = 0;

            switch (type)
            {
                //WEAPONS
                case UberstrikeItemClass.WeaponMelee:
                    _currentState = AnimationState.Melee;
                    break;
                case UberstrikeItemClass.WeaponHandgun:
                    _currentState = AnimationState.SmallGun;
                    break;
                case UberstrikeItemClass.WeaponCannon:
                case UberstrikeItemClass.WeaponLauncher:
                case UberstrikeItemClass.WeaponMachinegun:
                case UberstrikeItemClass.WeaponShotgun:
                case UberstrikeItemClass.WeaponSniperRifle:
                case UberstrikeItemClass.WeaponSplattergun:
                    _currentState = AnimationState.HeavyGun;
                    break;
                default:
                    _currentState = AnimationState.Idle;
                    PlayAnimation(AnimationIndex.HomeNoWeaponIdle, AnimationState.Idle, resetAnimations);
                    break;
            }
        }
    }
}
using UberStrike.DataCenter.Common.Entities;
using UberStrike.Realtime.Common;
using UberStrike.Core.Types;

public class CharacterStateAnimationController
{
    public void Update(CharacterInfo state, AvatarAnimationController animation)
    {
        if (animation != null && state != null)
        {
            RunPlayerConditions(state, animation);

            animation.UpdateAnimation();
        }
    }

    private void RunPlayerConditions(CharacterInfo state, AvatarAnimationController animation)
    {
        if (IsCinematic || animation == null) return;

        if (state.Is(PlayerStates.PAUSED))
        {
            if (state.Is(PlayerStates.DUCKED))
            {
                animation.PlayAnimation(AnimationIndex.squat);
            }
            else
            {
                if (state.CurrentWeaponID == 0)
                    animation.PlayAnimation(AnimationIndex.idle);
                else if (state.CurrentWeaponSlot == 0)
                    animation.PlayAnimation(AnimationIndex.ShopSmallGunAimIdle);
                else
                    animation.PlayAnimation(AnimationIndex.heavyGunBreathe);
            }
        }
        else
        {
            float animationSpeed = LevelEnviroment.MovementSpeed * 1;// state.Velocity;

            if (state.Is(PlayerStates.DIVING))
            {
                animation.PlayAnimation(AnimationIndex.swimLoop, animationSpeed * 0.5f);
            }
            else if (state.Is(PlayerStates.SWIMMING))
            {
                if (state.Is(PlayerStates.GROUNDED))
                {
                    if (state.Keys == KeyState.Still)
                    {
                        if (state.CurrentWeaponSlot == 0)
                            animation.PlayAnimation(AnimationIndex.ShopSmallGunAimIdle);
                        else
                            animation.PlayAnimation(AnimationIndex.heavyGunBreathe);
                    }
                    else
                    {
                        animation.PlayAnimation(AnimationIndex.walk, animationSpeed);
                    }
                }
                else
                {
                    animation.PlayAnimation(AnimationIndex.swimLoop, animationSpeed);
                }

                _isJumping = false;
            }
            else
            {
                //is walking
                if (state.Distance > 0.05 && state.Is(PlayerStates.GROUNDED))// && (state.Keys & KeyState.Walking) != 0 && (state.Keys ^ KeyState.Horizontal) != 0 && (state.Keys ^ KeyState.Vertical) != 0)
                {
                    if (state.Is(PlayerStates.DUCKED))
                    {
                        animation.PlayAnimation(AnimationIndex.crouch, animationSpeed);
                    }
                    else
                    {
                        animation.PlayAnimation(AnimationIndex.run, animationSpeed);
                    }
                }
                else if (state.Is(PlayerStates.JUMPING))
                {
                    animation.PlayAnimation(AnimationIndex.jumpUp);
                    _isJumping = true;
                }
                else if (state.Is(PlayerStates.DUCKED))
                {
                    animation.PlayAnimation(AnimationIndex.squat);
                    _isJumping = false;
                }
                else
                {

                    if (_isJumping)
                        animation.TriggerAnimation(AnimationIndex.jumpLand);

                    if (state.CurrentWeaponSlot == 0)
                        animation.PlayAnimation(AnimationIndex.ShopSmallGunAimIdle);
                    else
                        animation.PlayAnimation(AnimationIndex.heavyGunBreathe);

                    _isJumping = false;
                }


                if (state.IsFiring)
                {
                    if (state.CurrentWeaponCategory == UberstrikeItemClass.WeaponMelee && !IsPlayingMelee(animation))
                    {
                        if (UnityEngine.Random.Range(0, 2) == 0)
                        {
                            animation.TriggerAnimation(AnimationIndex.meleeSwingRightToLeft, 1.5f, false);
                        }
                        else
                        {
                            animation.TriggerAnimation(AnimationIndex.meleSwingLeftToRight, 1.5f, false);
                        }
                    }
                }
            }

            UpdateAimingMode(state, animation);
        }
    }

    private bool IsPlayingMelee(AvatarAnimationController animation)
    {
        return animation.IsPlaying(AnimationIndex.meleeSwingRightToLeft) || animation.IsPlaying(AnimationIndex.meleSwingLeftToRight);
    }

    private void UpdateAimingMode(CharacterInfo state, AvatarAnimationController animation)
    {
        if (state.CurrentFiringMode == FireMode.Secondary)
        {
            _aimingMode = AnimationIndex.snipeUpDown;
        }
        else if (state.CurrentFiringMode == FireMode.Primary)
        {
            _aimingMode = AnimationIndex.heavyGunUpDown;
        }

        animation.SetAnimationTimeNormalized(_aimingMode, state.VerticalRotation);
    }

    #region Properties
    public bool IsCinematic { get; set; }
    #endregion

    #region Fields
    private AnimationIndex _aimingMode = AnimationIndex.heavyGunUpDown;

    private bool _isJumping = false;
    #endregion
}

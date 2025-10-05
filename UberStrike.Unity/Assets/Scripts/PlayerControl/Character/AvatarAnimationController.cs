using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class AvatarAnimationController
{
    public AvatarAnimationController(Animation animation)
    {
        _bones = new Dictionary<int, Transform>();

        _animations = new Dictionary<int, AnimationInfo>();

        _animation = animation;

        InitBones(animation.transform);

        InitAnimations();
    }

    public void UpdateAnimation()
    {
        foreach (AnimationInfo info in Animations)
        {
            if (info.EndTime >= Time.time)
            {
                if (info.State.weight == 0)
                {
                    info.State.time = 0;
                }

                info.State.speed = info.Speed;
                info.CurrentTimePlayed = info.State.normalizedTime;

                Animation.Blend(info.Name, 1, 0);
            }
            else
            {
                info.State.speed = 0;
                info.CurrentTimePlayed = info.State.normalizedTime;
                Animation.Blend(info.Name, 0, 0.3f);
            }
        }
    }

    public void PlayAnimation(AnimationIndex id)
    {
        PlayAnimation(id, 1);
    }

    public bool PlayAnimation(AnimationIndex id, float speed)
    {
        return PlayAnimation(id, speed, Time.deltaTime);
    }

    public bool PlayAnimation(AnimationIndex id, float speed, float runtime)
    {
        AnimationInfo info;
        if (_animations.TryGetValue((int)id, out info))
        {
            info.Speed = speed;
            info.EndTime = Time.time + runtime;
        }

        return info != null;
    }

    public void TriggerAnimation(AnimationIndex id)
    {
        TriggerAnimation(id, 1, false);
    }

    public void TriggerAnimation(AnimationIndex id, bool stopAll)
    {
        TriggerAnimation(id, 1, stopAll);
    }

    public void TriggerAnimation(AnimationIndex id, float speed, bool stopAll)
    {
        AnimationInfo info;
        if (_animations.TryGetValue((int)id, out info))
        {
            if (stopAll) ResetAllAnimations();

            info.State.time = 0;

            PlayAnimation(id, speed, info.State.length / speed);
        }
    }

    public void RewindAnimation(AnimationIndex id)
    {
        AnimationInfo info;
        if (_animations.TryGetValue((int)id, out info))
        {
            info.State.speed = 0;
            info.State.time = 0;
            info.EndTime = 0;
        }
    }

    public void ResetAllAnimations()
    {
        foreach (AnimationIndex i in _animations.Keys)
            RewindAnimation(i);
    }

    public string GetDebugInfo()
    {
        StringBuilder b = new StringBuilder();
        foreach (AnimationInfo i in _animations.Values)
        {
            AnimationState s = i.State;
            b.Append(s.name);
            b.Append(", weight: ");
            b.Append(s.weight.ToString("N2"));
            b.Append(", time/runtime: ");
            b.Append(s.time.ToString("N2"));
            b.Append("/");
            b.Append(i.EndTime.ToString("N2"));
            b.Append(", length ");
            b.Append(s.length.ToString("N2"));
            b.Append("\n");
        }
        return b.ToString();
    }

    public bool IsPlaying(AnimationIndex idx)
    {
        AnimationInfo info;

        if (_animations.TryGetValue((int)idx, out info))
            return info.State.weight > 0;
        else
            return false;
    }

    public void SetAnimationTimeNormalized(AnimationIndex idx, float time)
    {
        AnimationInfo info;
        if (_animations.TryGetValue((int)idx, out info))
        {
            info.State.normalizedTime = Mathf.Lerp(info.State.normalizedTime, Mathf.Clamp01(time), Time.deltaTime * 10);

            PlayAnimation(idx, 0);
        }
    }

    public Transform GetBoneTransform(BoneIndex i)
    {
        Transform t = null;
        _bones.TryGetValue((int)i, out t);
        return t;
    }

    public bool TryGetAnimationInfo(AnimationIndex id, out AnimationInfo info)
    {
        _animations.TryGetValue((int)id, out info);
        return info != null;
    }

    private void InitBones(Transform rigging)
    {
        Transform[] transforms = rigging.GetComponentsInChildren<Transform>(true);
        Dictionary<string, Transform> bones = new Dictionary<string, Transform>(transforms.Length);

        foreach (Transform t in transforms)
        {
            bones[t.name] = t;
        }

        foreach (BoneIndex b in Enum.GetValues(typeof(BoneIndex)))
        {
            string name = Enum.GetName(typeof(BoneIndex), b);
            Transform t;
            if (bones.TryGetValue(name, out t))
                _bones.Add((int)b, t);
        }
    }

    private void InitAnimations()
    {
        foreach (AnimationIndex index in Enum.GetValues(typeof(AnimationIndex)))
        {
            AnimationState state = _animation[Enum.GetName(typeof(AnimationIndex), index)];
            if (state != null)
            {
                _animations.Add((int)index, new AnimationInfo(index, state));

                Transform t;
                if (_bones.TryGetValue(GetMixingTransformBoneIndex(index), out t))
                    state.AddMixingTransform(t);

                state.wrapMode = GetAnimationWrapMode(index);
                state.blendMode = GetAnimationBlendMode(index);
                state.layer = GetAnimationLayer(index);
            }
        }
    }

    private WrapMode GetAnimationWrapMode(AnimationIndex id)
    {
        switch (id)
        {
            case AnimationIndex.swimLoop:
            case AnimationIndex.run:
            case AnimationIndex.walk:
            case AnimationIndex.crouch:
            case AnimationIndex.HomeLargeGunCheckWeapon:
            case AnimationIndex.HomeLargeGunLookAround:
            case AnimationIndex.HomeLargeGunIdle:
            case AnimationIndex.HomeLargeGunRelaxNeck:
            case AnimationIndex.HomeLargeGunShakeWeapon:
            case AnimationIndex.HomeSmallGunCheckWeapon:
            case AnimationIndex.HomeSmallGunLookAround:
            case AnimationIndex.HomeSmallGunIdle:
            case AnimationIndex.HomeSmallGunRelaxNeck:
            case AnimationIndex.HomeMeleeCheckWeapon:
            case AnimationIndex.HomeMeleeLookAround:
            case AnimationIndex.HomeMeleeIdle:
            case AnimationIndex.HomeMeleeRelaxNeck:
            case AnimationIndex.HomeMediumGunCheckWeapon:
            case AnimationIndex.HomeMediumGunLookAround:
            case AnimationIndex.HomeMediumGunIdle:
            case AnimationIndex.HomeMediumGunRelaxNeck:
            case AnimationIndex.ShopSmallGunAimIdle:
            case AnimationIndex.ShopSmallGunShoot:
            case AnimationIndex.ShopLargeGunAimIdle:
            case AnimationIndex.ShopLargeGunShoot:
            case AnimationIndex.ShopMeleeAimIdle:
            case AnimationIndex.HomeNoWeaponIdle:
            case AnimationIndex.HomeNoWeaponnLookAround:
            case AnimationIndex.HomeNoWeaponRelaxNeck:
            case AnimationIndex.ShopNewGloves:
            case AnimationIndex.ShopNewUpperBody:
            case AnimationIndex.ShopNewBoots:
            case AnimationIndex.ShopNewLowerBody:
            case AnimationIndex.ShopNewHead:
            case AnimationIndex.lightGunBreathe:
            case AnimationIndex.heavyGunBreathe:
            case AnimationIndex.idle:
            case AnimationIndex.idleWalk:
            case AnimationIndex.TutorialGuideWalk:
            case AnimationIndex.TutorialGuideIdle:
                return WrapMode.Loop;

            case AnimationIndex.shootLightGun:
            case AnimationIndex.shootHeavyGun:
            case AnimationIndex.gotHit:
            case AnimationIndex.swimStart:
            case AnimationIndex.meleeSwingRightToLeft:
            case AnimationIndex.meleSwingLeftToRight:
                return WrapMode.Once;

            case AnimationIndex.ShopLargeGunTakeOut:
            case AnimationIndex.ShopSmallGunTakeOut:
            case AnimationIndex.ShopMeleeTakeOut:
            case AnimationIndex.die1:
            case AnimationIndex.squat:
            case AnimationIndex.lightGunUpDown:
            case AnimationIndex.heavyGunUpDown:
            case AnimationIndex.snipeUpDown:
            case AnimationIndex.jumpUp:
            case AnimationIndex.ShopHideGun:
            case AnimationIndex.ShopHideMelee:
                return WrapMode.ClampForever;

            default:
                return WrapMode.Default;
        }
    }

    private AnimationBlendMode GetAnimationBlendMode(AnimationIndex id)
    {
        switch (id)
        {
            case AnimationIndex.shootHeavyGun:
            case AnimationIndex.shootLightGun:
                return AnimationBlendMode.Additive;

            default:
                return AnimationBlendMode.Blend;
        }
    }

    private int GetAnimationLayer(AnimationIndex id)
    {
        switch (id)
        {
            case AnimationIndex.lightGunUpDown:
            case AnimationIndex.heavyGunUpDown:
            case AnimationIndex.snipeUpDown:
            case AnimationIndex.swimLoop:
            case AnimationIndex.ShopSmallGunAimIdle:
                return 1;

            case AnimationIndex.meleeSwingRightToLeft:
            case AnimationIndex.meleSwingLeftToRight:
                return 2;

            case AnimationIndex.die1:
                return 5;

            default:
                return 0;
        }
    }

    private int GetMixingTransformBoneIndex(AnimationIndex id)
    {
        switch (id)
        {
            case AnimationIndex.lightGunUpDown:
            case AnimationIndex.heavyGunUpDown:
            case AnimationIndex.shootLightGun:
            case AnimationIndex.shootHeavyGun:
            case AnimationIndex.snipeUpDown:
            case AnimationIndex.meleeSwingRightToLeft:
            case AnimationIndex.meleSwingLeftToRight:
                return (int)BoneIndex.Spine;

            default:
                return (int)BoneIndex.NONE;
        }
    }

    #region Properties

    public ICollection<AnimationInfo> Animations
    {
        get { return _animations.Values; }
    }

    public Animation Animation
    {
        get { return _animation; }
    }

    #endregion

    #region Fields

    private Animation _animation;

    private Dictionary<int, AnimationInfo> _animations;

    private Dictionary<int, Transform> _bones;

    #endregion
}

[Serializable]
public class AnimationInfo
{
    public AnimationInfo(AnimationIndex idx, AnimationState state)
    {
        State = state;
        Name = Enum.GetName(typeof(AnimationIndex), idx);
        Index = idx;
    }

    public AnimationState State;
    public String Name;
    public AnimationIndex Index;
    public float EndTime = 0;
    public float CurrentTimePlayed = 0;
    public float Speed = 1;
}
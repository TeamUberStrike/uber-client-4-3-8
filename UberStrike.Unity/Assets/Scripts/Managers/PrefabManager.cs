using System;
using Cmune.Util;
using UberStrike.Core.Types;
using UnityEngine;

public class PrefabManager : MonoSingleton<PrefabManager>
{
    [SerializeField]
    private AvatarDecorator[] _avatarPrefabs;

    [SerializeField]
    private AvatarDecoratorConfig[] _ragdollPrefabs;

    [SerializeField]
    private CharacterConfig _remoteCharacter;

    [SerializeField]
    private CharacterConfig _localCharacter;

    [SerializeField]
    private GameObject _lotteryEffect;

    [SerializeField]
    private Animation _lotteryUIAnimation;

    public CharacterConfig InstantiateLocalCharacter()
    {
        return GameObject.Instantiate(_localCharacter) as CharacterConfig;
    }

    public CharacterConfig InstantiateRemoteCharacter()
    {
        return GameObject.Instantiate(_remoteCharacter) as CharacterConfig;
    }

    public AvatarDecorator GetAvatarPrefab(AvatarType avatarType)
    {
        var prefab = Array.Find(_avatarPrefabs, p => p.Configuration.AvatarType == avatarType);
        
        return prefab;
    }

    public AvatarDecoratorConfig GetRagdollPrefab(AvatarType avatarType)
    {
        var prefab = Array.Find(_ragdollPrefabs, p => p.AvatarType == avatarType);
        
        return prefab;
    }

    public void InstantiateLotteryEffect()
    {
        if (_lotteryEffect)
        {
            GameObject.Instantiate(_lotteryEffect);
        }
    }

    public Animation GetLotteryUIAnimation()
    {
        Animation anim = null;

        if (_lotteryUIAnimation)
        {
            anim = GameObject.Instantiate(_lotteryUIAnimation) as Animation;
        }
        else
        {
            Debug.LogError("The LotteryUIAnimation is not signed in PrefabManger!");
        }

        return anim;
    }
}
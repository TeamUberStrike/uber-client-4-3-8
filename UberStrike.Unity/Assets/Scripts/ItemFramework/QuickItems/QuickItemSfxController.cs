using System.Collections.Generic;
using UberStrike.DataCenter.Common.Entities;
using UnityEngine;
using UberStrike.Core.Types;

public class QuickItemSfxController : Singleton<QuickItemSfxController>
{
    private Dictionary<QuickItemLogic, QuickItemSfx> _effects;
    private Dictionary<int, QuickItemSfx> _curShownEffects;
    private int _sfxId;

    private int NextSfxId
    {
        get { return ++_sfxId; }
    }

    private QuickItemSfxController()
    {
        _effects = new Dictionary<QuickItemLogic, QuickItemSfx>();
        _curShownEffects = new Dictionary<int, QuickItemSfx>();
    }

    public void RegisterQuickItemEffect(QuickItemLogic behaviour, QuickItemSfx effect)
    {
        if (effect)
        {
            _effects.Add(behaviour, effect);
        }
        else
        {
            Debug.LogError("QuickItemSfx is null: " + behaviour);
        }
    }

    public void ShowThirdPersonEffect(CharacterConfig player, QuickItemLogic effectType, 
        int robotLifeTime, int scrapsLifeTime, bool isInstant = false)
    {
        QuickItemSfx effect;
        robotLifeTime = robotLifeTime > 0 ? robotLifeTime : 5000;
        scrapsLifeTime = scrapsLifeTime > 0 ? scrapsLifeTime : 3000;

        if (_effects.TryGetValue(effectType, out effect))
        {
            var sfx = GameObject.Instantiate(effect) as QuickItemSfx;
            sfx.ID = CreateGlobalSfxID(player.State.PlayerNumber, NextSfxId);
            if (sfx)
            {
                _curShownEffects.Add(sfx.ID, sfx);
                sfx.transform.parent = player.transform;
                sfx.transform.localRotation = Quaternion.AngleAxis(-45, Vector3.up);
                sfx.transform.localPosition = new Vector3(0, 0.2f, 0);
                sfx.Play(robotLifeTime, scrapsLifeTime, isInstant);
                LayerUtil.SetLayerRecursively(sfx.transform, UberstrikeLayer.IgnoreRaycast);
            }
        }
        else
        {
            Debug.LogError("Failed to get effect: " + effectType);
        }
    }

    public void RemoveEffect(int id)
    {
        QuickItemSfx effect;
        if (_curShownEffects.TryGetValue(id, out effect))
        {
            _curShownEffects.Remove(id);
        }
    }

    public void DestroytSfxFromPlayer(byte playerNumber)
    {
        foreach (var effectPair in _curShownEffects)
        {
            if ((effectPair.Key & 0xFF) == playerNumber)
            {
                effectPair.Value.Destroy();
                _curShownEffects.Remove(effectPair.Key);
                break;
            }
        }
    }

    private static int CreateGlobalSfxID(byte playerNumber, int sfxId)
    {
        return (sfxId << 8) + playerNumber;
    }
}
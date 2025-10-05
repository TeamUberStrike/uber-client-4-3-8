using System.Collections.Generic;
using UberStrike.DataCenter.Common.Entities;
using UnityEngine;
using UberStrike.Core.Types;

public class QuickItemSfxConfigurator : MonoBehaviour
{
    [SerializeField]
    private List<QuickItemEffectHolder> _effects;

    void Awake()
    {
        foreach (var e in _effects)
        {
            QuickItemSfxController.Instance.RegisterQuickItemEffect(e.Behaviour, e.Effect);
        }

        //clean up after initialization
        //GameObject.Destroy(gameObject);
    }

    [System.Serializable]
    private class QuickItemEffectHolder
    {
#pragma warning disable 0649
        public QuickItemSfx Effect;
        public QuickItemLogic Behaviour;
#pragma warning restore 0649
    }
}
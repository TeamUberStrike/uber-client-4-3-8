using UnityEngine;
using System.Collections.Generic;
using UberStrike.DataCenter.Common.Entities;
using UberStrike.Core.Types;

public class UnityItemConfiguration : MonoBehaviour
{
    public List<HoloGearItem> UnityItemsHolo;
    public List<GearItem> UnityItemsGear;
    public List<WeaponItem> UnityItemsWeapons;
    public List<FunctionalItemHolder> UnityItemsFunctional;

    [SerializeField]
    private List<QuickItemHolder> _unityItemsQuick;

    void Awake()
    {
        ItemManager.Instance.AddUnityItems(UnityItemsHolo.ToArray());
        ItemManager.Instance.AddUnityItems(UnityItemsGear.ToArray());
        ItemManager.Instance.AddUnityItems(UnityItemsWeapons.ToArray());

        ItemManager.Instance.AddUnityItems(UnityItemsFunctional.ConvertAll<FunctionalItem>(new System.Converter<FunctionalItemHolder, FunctionalItem>
            (a => new FunctionalItem()
            {
                Icon = a.Icon,
                Configuration = new FunctionalItemConfiguration()
                {
                    ID = a.ItemId,
                    Name = a.Name,
                }
            })).ToArray());

        foreach (var i in _unityItemsQuick)
        {
            QuickItem.AddPrefab(new KeyValuePair<QuickItemLogic, BaseQuickItem>(i.Logic, i.Prefab));
        }

        ItemManager.Instance.ConfigureDefaultGearAndWeapons();

        //clean up after yourself
        //GameObject.Destroy(this);
    }

    [System.Serializable]
    public class FunctionalItemHolder
    {
        public string Name;
        public Texture2D Icon;
        public int ItemId;
    }

    [System.Serializable]
    private class QuickItemHolder
    {
#pragma warning disable 0649
        public QuickItemLogic Logic;
        public BaseQuickItem Prefab;
#pragma warning restore 0649
    }
}

using System.Collections.Generic;
using UberStrike.Core.Models.Views;
using UberStrike.Core.Types;
using UnityEngine;

public class QuickItem : IUnityItem
{
    private BaseQuickItem _prefab;

    public Texture2D Icon { get; set; }
    public int ItemId { get { return ItemView.ID; } set { ; } }
    public string Name { get { return ItemView.Name; } }
    public UberstrikeItemType ItemType { get { return ItemView.ItemType; } }
    public UberstrikeItemClass ItemClass { get { return ItemView.ItemClass; } }
    public BaseUberStrikeItemView ItemView { get { return Configuration; } }
    public MonoBehaviour Prefab { get { return _prefab; } }

    public QuickItemConfiguration Configuration { get; private set; }

    public QuickItem(UberStrikeItemQuickView itemView)
    {
        if (AllPrefabs.TryGetValue(itemView.BehaviourType, out _prefab))
        {
            //first clone the existing configuration
            Configuration = CloneUtil.Clone(_prefab.Configuration);
            //secondly copy over properties from the DB
            ItemConfigurationUtil.CopyProperties(Configuration, itemView);

            Icon = _prefab.GetCustomIcon(Configuration);
        }
        else
        {
            throw new System.Exception("QuickItem " + itemView.ID + " " + itemView.Name + " not supported");
        }
    }

    public BaseQuickItem Instantiate()
    {
        var instance = GameObject.Instantiate(_prefab) as BaseQuickItem;

        instance.Configuration = Configuration;
        instance.gameObject.SetActiveRecursively(false);
        instance.gameObject.active = true;
        instance.gameObject.name = "QI - " + Configuration.Name;
        
        return instance;
    }

    private static IDictionary<QuickItemLogic, BaseQuickItem> AllPrefabs = new Dictionary<QuickItemLogic, BaseQuickItem>();
    internal static void AddPrefab(KeyValuePair<QuickItemLogic, BaseQuickItem> prefab)
    {
        AllPrefabs.Add(prefab);
    }
}
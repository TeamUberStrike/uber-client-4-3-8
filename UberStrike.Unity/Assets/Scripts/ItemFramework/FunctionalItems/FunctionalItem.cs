using UberStrike.Core.Models.Views;
using UberStrike.Core.Types;
using UnityEngine;

public class FunctionalItem : IUnityItem
{
    private Texture2D _icon;

    public Texture2D Icon { get { return _icon; } set { _icon = value; } }
    public int ItemId { get { return ItemView.ID; } set { ItemView.ID = value; } }
    public string Name { get { return ItemView.Name; } set { ItemView.Name = value; } }
    public UberstrikeItemType ItemType { get { return ItemView.ItemType; } }
    public UberstrikeItemClass ItemClass { get { return ItemView.ItemClass; } }
    public MonoBehaviour Prefab { get { return null; } }

    public FunctionalItemConfiguration Configuration { get; set; }

    public BaseUberStrikeItemView ItemView
    {
        get { return Configuration; }
    }
}

public class FunctionalItemConfiguration : UberStrikeItemFunctionalView
{
    //nothing to configure yet
}
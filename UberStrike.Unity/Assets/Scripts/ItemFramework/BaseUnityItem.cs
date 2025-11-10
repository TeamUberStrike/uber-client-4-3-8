using UberStrike.Core.Types;
using UberStrike.DataCenter.Common.Entities;
using UnityEngine;
using UberStrike.Core.Models.Views;

public abstract class BaseUnityItem : MonoBehaviour, IUnityItem
{
    [SerializeField]
    private Texture2D _icon;

    [SerializeField]
    private int _itemId;

    public abstract BaseUberStrikeItemView ItemView { get; }

    public Texture2D Icon { get { return _icon; } set { _icon = value; } }
    public int ItemId { get { return _itemId; } set { _itemId = value; } }
    public string Name { get { return ItemView.Name; } }
    public UberstrikeItemType ItemType { get { return ItemView.ItemType; } }
    public UberstrikeItemClass ItemClass { get { return ItemView.ItemClass; } }
    public MonoBehaviour Prefab { get { return this; } }
}
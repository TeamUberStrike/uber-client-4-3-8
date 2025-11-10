using UberStrike.Core.Models.Views;
using UberStrike.Core.Types;
using UnityEngine;

public interface IUnityItem
{
    Texture2D Icon { get; set; }
    int ItemId { get; set; }
    string Name { get; }
    UberstrikeItemType ItemType { get; }
    UberstrikeItemClass ItemClass { get; }
    BaseUberStrikeItemView ItemView { get; }

    MonoBehaviour Prefab { get; }
}
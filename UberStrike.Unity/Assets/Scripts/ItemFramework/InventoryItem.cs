
using UnityEngine;

public class InventoryItem
{
    private IUnityItem _item;

    public InventoryItem(IUnityItem item)
    {
        _item = item;
    }

    public IUnityItem Item
    {
        get { return _item; }
    }

    public int DaysRemaining
    {
        get
        {
            return (!IsPermanent && ExpirationDate.HasValue) ? Mathf.CeilToInt((float)ExpirationDate.Value.Subtract(ApplicationDataManager.ServerDateTime).TotalHours / 24f) : 0;
        }
    }

    public int AmountRemaining { get; set; }
    public bool IsPermanent { get; set; }
    public System.DateTime? ExpirationDate { get; set; }
    public bool IsHighlighted { get; set; }

    public bool IsValid
    {
        get { return IsPermanent || DaysRemaining > 0; }
    }
}
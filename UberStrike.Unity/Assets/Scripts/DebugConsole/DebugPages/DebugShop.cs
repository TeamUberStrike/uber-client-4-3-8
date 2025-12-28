using UnityEngine;

public class DebugShop : IDebugPage
{
    Vector2 scroll1;
    Vector2 scroll2;

    public string Title
    {
        get { return "Shop"; }
    }

    // Use this for initialization
    public void Draw()
    {
        GUILayout.BeginHorizontal();
        scroll1 = GUILayout.BeginScrollView(scroll1);
        foreach (var item in ItemManager.Instance.ShopItems)
        {
            GUILayout.Label(item.ItemId + ": " + item.Name);
        }
        GUILayout.EndScrollView();

        scroll2 = GUILayout.BeginScrollView(scroll2);
        foreach (var item in InventoryManager.Instance.InventoryItems)
        {
            GUILayout.Label(item.Item.ItemId + ": " + item.Item.Name + ", Amount: " + item.AmountRemaining + ", Days: " + item.DaysRemaining);
        }
        GUILayout.EndScrollView();
        GUILayout.EndHorizontal();
    }
}

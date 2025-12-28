using UberStrike.Core.Models.Views;
using UberStrike.Core.Types;
using UnityEngine;

public class CreditsUnityItem : IUnityItem
{
    public CreditsUnityItem(int credits)
    {
        Name = credits.ToString("N0") + " Credits";
        ItemView = new DummyItemView()
        {
            Description = string.Format("An extra {0:N0} Credits to fatten up your UberWallet!", credits)
        };
    }

    public Texture2D Icon { get { return UberstrikeTextures.CreditsIcon48x48; } set { } }
    public int ItemId { get; set; }
    public string Name { get; private set; }

    public UberstrikeItemType ItemType { get; private set; }
    public UberstrikeItemClass ItemClass { get; private set; }
    public BaseUberStrikeItemView ItemView { get; private set; }
    public MonoBehaviour Prefab { get; private set; }

    private class DummyItemView : BaseUberStrikeItemView
    {
        public override UberstrikeItemType ItemType
        {
            get { return UberstrikeItemType.Special; }
        }
    }
}
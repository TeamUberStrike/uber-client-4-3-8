using Cmune.DataCenter.Common.Entities;

public class MasBundleUnityView
{
    public BundleView BundleView { get; private set; }
    public string CurrencySymbol { get; set; }
    public string Price { get; set; }
    public DynamicTexture Icon { get; private set; }
    public DynamicTexture Image { get; private set; }
    public bool IsOwned { get; set; }
    public bool IsValid { get { return !string.IsNullOrEmpty(Price); } }

    public BundleCategoryType Category { get { return BundleView.Category; } }

    public MasBundleUnityView(BundleView bundleView)
    {
        BundleView = bundleView;
        Icon = new DynamicTexture(BundleView.IconUrl);
        Image = new DynamicTexture(BundleView.ImageUrl);
    }
}
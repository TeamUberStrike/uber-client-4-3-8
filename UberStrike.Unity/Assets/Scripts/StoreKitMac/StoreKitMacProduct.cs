using System;

public class StoreKitMacProductModel
{
    public string ProductIdentifier { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Price { get; set; }
    public string CurrencySymbol { get; set; }

    public static StoreKitMacProductModel productFromString(string productString)
    {
        var product = new StoreKitMacProductModel();

        string[] productParts = productString.Split(new string[] { "|||" }, StringSplitOptions.None);
        if (productParts.Length == 5)
        {
            product.ProductIdentifier = productParts[0];
            product.Title = productParts[1];
            product.Description = productParts[2];
            product.Price = productParts[3];
            product.CurrencySymbol = productParts[4];
        }

        return product;
    }


    public override string ToString()
    {
        return String.Format("<StoreKitProduct>\nID: {0}\nTitle: {1}\nDescription: {2}\nPrice: {3}\nCurrency Symbol: {4}",
            ProductIdentifier, Title, Description, Price, CurrencySymbol);
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Cmune.DataCenter.Common.Entities;
using Cmune.Util;
using UberStrike.DataCenter.Common.Entities;
using UberStrike.WebService.Unity;
using UnityEngine;

public class MasBundleManager : Singleton<MasBundleManager>
{
#if UNITY_STANDALONE_OSX
    [System.Runtime.InteropServices.DllImport("StoreKitReceiptValidator")]
    private static extern string GetReceipt(string receiptPath, string verNumber, bool debugMode);
#endif

    private BasePopupDialog _appStorePopup;

    private Dictionary<BundleCategoryType, List<MasBundleUnityView>> _bundlesPerCategory = new Dictionary<BundleCategoryType, List<MasBundleUnityView>>();

    public int Count { get; private set; }

    public bool CanMakeMasPayments { get; private set; }

    public List<MasBundleUnityView> GetBundlesInCategory(BundleCategoryType category)
    {
        List<MasBundleUnityView> boxes;
        if (_bundlesPerCategory.TryGetValue(category, out boxes))
            return boxes;
        else
            return new List<MasBundleUnityView>(0);
    }

    public IEnumerable<MasBundleUnityView> AllItemBundles
    {
        get
        {
            foreach (var category in _bundlesPerCategory)
                if (category.Key != BundleCategoryType.None)
                {
                    foreach (var box in category.Value)
                        yield return box;
                }
        }
    }

    public IEnumerable<MasBundleUnityView> AllBundles
    {
        get
        {
            foreach (var category in _bundlesPerCategory.Values)
                foreach (var box in category)
                    yield return box;
        }
    }

    private MasBundleManager()
    {
#if UNITY_EDITOR
        CanMakeMasPayments = true;
#elif UNITY_STANDALONE_OSX
        StoreKitMacManager.productListReceived += OnMasProductListReceived;
        StoreKitMacManager.productListRequestFailed += OnMasProductListRequestFailed;
        StoreKitMacManager.purchaseFailed += OnMasPurchaseFailed;
        StoreKitMacManager.purchaseCancelled += OnMasPurchaseCancelled;
        StoreKitMacManager.purchaseSuccessful += OnMasPurchaseSuccessful;
        CanMakeMasPayments = StoreKitMacBinding.canMakePayments();
#endif
    }

    public void Initialize()
    {
        // Get the Mac App Store packs 
        if (CanMakeMasPayments)
        {
            ShopWebServiceClient.GetBundles(ChannelType.MacAppStore,
                (bundles) => SetBundles(bundles),
                (exception) => CmuneDebug.LogError("Error getting bundles from the server."));
        }
    }

    private void SetBundles(List<BundleView> bundleViews)
    {
        if (bundleViews != null && bundleViews.Count > 0)
        {
            foreach (var view in bundleViews)
            {
                if (!string.IsNullOrEmpty(view.MacAppStoreUniqueId))
                {
                    List<MasBundleUnityView> bundles;
                    if (!_bundlesPerCategory.TryGetValue(view.Category, out bundles))
                    {
                        bundles = new List<MasBundleUnityView>();
                        _bundlesPerCategory[view.Category] = bundles;
                    }

                    bundles.Add(new MasBundleUnityView(view));
                }
            }

            GetStoreKitProductData();
        }
        else
        {
            CmuneDebug.LogError("SetServerBundles: Bundles received from the server were null or empty!");
        }
    }

    private float dialogTimer = 0.0f;

    public IEnumerator StartCancelDialogTimer()
    {
        if (dialogTimer < 5.0f) dialogTimer = 5.0f;

        while (_appStorePopup != null && dialogTimer > 0.0f)
        {
            dialogTimer -= Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }

        if (_appStorePopup != null)
            _appStorePopup.SetAlertType(PopupSystem.AlertType.Cancel);
    }

    public void BuyStoreKitItem(string uniqueId, int bundleId)
    {
        _appStorePopup = PopupSystem.ShowMessage("In App Purchase", "Opening the Mac App Store, please wait...", PopupSystem.AlertType.None) as BasePopupDialog;
        MonoRoutine.Start(StartCancelDialogTimer());
#if UNITY_STANDALONE_OSX
        StoreKitMacBinding.purchaseProduct(uniqueId, 1);
#endif
    }

    private void GetStoreKitProductData()
    {
#if UNITY_EDITOR
        Count = 0;
        foreach (var bundle in AllBundles)
        {
            bundle.CurrencySymbol = "$";
            bundle.Price = bundle.BundleView.USDPrice.ToString("N2");
            bundle.IsOwned = IsItemPackOwned(bundle.BundleView.BundleItemViews);
            Count++;
        }
#elif UNITY_STANDALONE_OSX
        // Get the Mas Bundles
        // Comma delimited list of product ID's from iTunesConnect. MUST match exactly what you have there!
        string productIdentifiers = string.Empty;
        foreach (var bundle in AllBundles)
        {
            productIdentifiers += bundle.BundleView.MacAppStoreUniqueId + ",";
        }
        StoreKitMacBinding.requestProductData(productIdentifiers);
#endif
    }

    private void BuyMasBundle(MasBundleUnityView bundle, string transactionIdentifier)
    {
        MonoRoutine.Start(StartBuyMasBundle(transactionIdentifier, bundle.BundleView.Id));
    }

    private IEnumerator StartBuyMasBundle(string transactionIdentifier, int bundleId)
    {
        yield return new WaitForSeconds(1.0f);

        var popupDialog = PopupSystem.ShowMessage("Updating", "Completing your In App Purchase, please wait...", PopupSystem.AlertType.None);

        // Attribute the item and record the receipt
        yield return UberStrike.WebService.Unity.ShopWebServiceClient.BuyMasBundle(PlayerDataManager.CmidSecure,
            bundleId,
            transactionIdentifier,
            UberStrikeCommonConfig.ApplicationId,
            (success) =>
            {
                PopupSystem.HideMessage(popupDialog);
                if (success)
                {
                    OnBundlePurchased(bundleId);
                }
                else
                {
                    PopupSystem.HideMessage(popupDialog);
                    PopupSystem.ShowMessage(LocalizedStrings.Error, "There was an error completing your purchase.\nPlease contact support@cmune.com");
                }
            },
            (exception) =>
            {
                PopupSystem.HideMessage(popupDialog);
                CmuneDebug.LogError("Error - ShopWebServiceClient.BuyMasBundle(): " + exception.Message);
            });
    }

    private void OnBundlePurchased(int bundleId)
    {
        var bundle = AllBundles.FirstOrDefault(p => p.BundleView.Id == bundleId);

        if (bundle != null)
        {
            // We bought a credits or points pack
            if (bundle.BundleView.Credits > 0 || bundle.BundleView.Points > 0)
            {
                // Show the animation in the global UI ribbon if points or credits are attributed
                if (bundle.BundleView.Credits > 0)
                    GlobalUIRibbon.Instance.AddCreditsEvent(bundle.BundleView.Credits);
                if (bundle.BundleView.Points > 0)
                    GlobalUIRibbon.Instance.AddPointsEvent(bundle.BundleView.Points);

                // Refresh the players points / credits balance
                ApplicationDataManager.Instance.RefreshWallet();
            }
            else // We bought a bundle of items
            {
                // Show items attributed in a popup dialog
                List<IUnityItem> items = new List<IUnityItem>(8);
                for (int i = 0; i < bundle.BundleView.BundleItemViews.Count && i < 8; i++)
                {
                    items.Add(ItemManager.Instance.GetItemInShop(bundle.BundleView.BundleItemViews[i].ItemId));
                }
                PopupSystem.ShowItems("Purchase Successful", "New Items have been added to your inventory!", items);

                // Update the players inventory
                UberStrike.WebService.Unity.UserWebServiceClient.GetInventory(
                     PlayerDataManager.CmidSecure,
                     (inventory) =>
                     {
                         InventoryManager.Instance.UpdateInventoryItems(inventory);

                         // Update the IsOwned status of all bundles
                         foreach (MasBundleUnityView masBundle in AllBundles)
                         {
                             masBundle.IsOwned = IsItemPackOwned(masBundle.BundleView.BundleItemViews);
                         }
                     },
                     (exception) => CmuneDebug.LogError("Exception getting inventory: " + exception.Message)
                     );
            }
        }
        else
        {
            Debug.LogError("No MasBundle found with ID: " + bundleId);
        }
    }

    private bool IsItemPackOwned(List<BundleItemView> items)
    {
        // If there are no items in the list, let's assume the player doesn't "own" the bundle - used for credits and points bundles
        if (items.Count > 0)
        {
            foreach (var item in items)
            {
                if (!InventoryManager.Instance.IsItemInInventory(item.ItemId))
                {
                    return false;
                }
            }
            return true;
        }
        else
        {
            return false;
        }
    }

    private List<InAppPurchase> GetInAppPurchases(List<KeyValuePair<string, string>> receiptList)
    {
        // Iterate through all In App Purchases and put into a list
        List<InAppPurchase> inAppPurchases = new List<InAppPurchase>();
        if (receiptList.Exists(p => p.Key == "InApp"))
        {
            foreach (KeyValuePair<string, string> kvp in receiptList.FindAll(p => p.Key == "InApp"))
            {
                inAppPurchases.Add(ParseInAppPurchase(kvp.Value));
            }
        }
        return inAppPurchases;
    }

    private List<KeyValuePair<string, string>> ParseXmlReceipt(string receiptXml)
    {
        List<KeyValuePair<string, string>> receiptList = new List<KeyValuePair<string, string>>();

        if (string.IsNullOrEmpty(receiptXml) || receiptXml.Contains("<MASRECEIPT>Invalid</MASRECEIPT>"))
        {
            CmuneDebug.LogError("Receipt XML is invalid.");
        }
        else
        {
            try
            {
                XmlReader xmlReader = XmlReader.Create(new StringReader(receiptXml));
                while (xmlReader.Read())
                {
                    if (xmlReader.NodeType == XmlNodeType.Element && xmlReader.Name != "MASRECEIPT")
                    {
                        receiptList.Add(new KeyValuePair<string, string>(xmlReader.Name, xmlReader.ReadString()));
                    }
                }
            }
            catch (Exception ex)
            {
                CmuneDebug.LogError("Receipt XML was malformed.\n" + ex.Message);
            }
        }

        return receiptList;
    }

    private InAppPurchase ParseInAppPurchase(string inAppPurchaseText)
    {
        InAppPurchase inAppPurchase = new InAppPurchase();

        try
        {
            // Remove unwanted chars
            string cleanedData = inAppPurchaseText.Replace("(", "").Replace(")", "").Replace("{", "").Replace("}", "");
            cleanedData = cleanedData.Trim();

            // Split the data into an array and trim
            string[] fields = cleanedData.Split(';');
            fields = TrimStringArray(fields);

            foreach (string str in fields)
            {
                string[] row = str.Split('=');
                row = TrimStringArray(row);
                if (!string.IsNullOrEmpty(row[0]))
                {
                    switch (row[0])
                    {
                        case "PurchaseDate":
                            inAppPurchase.PurchaseDate = row[1].Replace("\"", "");
                            break;
                        case "ProductIdentifier":
                            inAppPurchase.ProductIdentifier = row[1].Replace("\"", "");
                            break;
                        case "TransactionIdentifier":
                            inAppPurchase.TransactionIdentifier = row[1];
                            break;
                        case "Quantity":
                            inAppPurchase.Quantity = row[1];
                            break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            CmuneDebug.LogError("Unable to parse In App Purchase.\n" + ex.Message);
        }

        return inAppPurchase;
    }

    private string[] TrimStringArray(string[] stringArray)
    {
        string[] returnArray = stringArray;

        for (int i = 0; i < returnArray.Length; i++)
        {
            returnArray[i] = returnArray[i].Trim();
        }
        return returnArray;
    }

    public MasBundleUnityView GetNextItem(MasBundleUnityView currentItem)
    {
        var boxes = new List<MasBundleUnityView>(AllItemBundles);
        if (boxes.Count > 0)
        {
            int index = boxes.FindIndex(i => i == currentItem);

            //take a random item if the current item doesn't exist (e.g. daily login)
            if (index < 0)
            {
                return boxes[UnityEngine.Random.Range(0, boxes.Count)];
            }
            //otherwise just take the next one in the list
            else
            {
                int next = (index + 1) % boxes.Count;
                return boxes[next];
            }
        }
        else
        {
            return currentItem;
        }
    }

    public MasBundleUnityView GetPreviousItem(MasBundleUnityView currentItem)
    {
        var boxes = new List<MasBundleUnityView>(AllItemBundles);
        if (boxes.Count > 0)
        {
            int index = boxes.FindIndex(i => i == currentItem);

            //take a random item if the current item doesn't exist (e.g. daily login)
            if (index < 0)
            {
                return boxes[UnityEngine.Random.Range(0, boxes.Count)];
            }
            //otherwise just take the next one in the list
            else
            {
                int next = (index - 1 + boxes.Count) % boxes.Count;
                return boxes[next];
            }
        }
        else
        {
            return currentItem;
        }
    }

    #region MAS callback functions

    private void OnMasPurchaseFailed(string error)
    {
        if (_appStorePopup != null)
            PopupSystem.HideMessage(_appStorePopup);

        PopupSystem.ShowMessage("Purchase Failed", "Sorry, it seems your purchase failed.\n Please contact support@cmune.com");
    }

    private void OnMasPurchaseCancelled(string error)
    {
        if (_appStorePopup != null)
            PopupSystem.HideMessage(_appStorePopup);

        PopupSystem.ShowMessage("Purchase Cancelled", "Your purchase was cancelled.");
    }

    private void OnMasPurchaseSuccessful(string productIdentifier, string unusedReceipt, int unusedQuantity)
    {
        if (_appStorePopup != null)
            PopupSystem.HideMessage(_appStorePopup);

        //CmuneDebug.Log(string.Format("OnMasPurchaseSuccessful: ProductIdenitifier={0} Receipt={1} Quantity={2}", productIdentifier, unusedReceipt, unusedQuantity));

        // Read the receipt from disk and load
        List<KeyValuePair<string, string>> receiptList = new List<KeyValuePair<string, string>>();

#if UNITY_STANDALONE_OSX
        receiptList = ParseXmlReceipt(GetReceipt(Application.dataPath + "/_MASReceipt/receipt", ApplicationDataManager.VersionShort, false));
#endif

        // Get all the In App Purchases we can find in the receipt
        List<InAppPurchase> inAppPurchases = GetInAppPurchases(receiptList);

        // Match the productIdentifier sent by the StoreKit callback with the one in our receipt
        InAppPurchase inAppPurchase = new InAppPurchase();
        if (inAppPurchases.Exists(p => p.ProductIdentifier == productIdentifier))
            inAppPurchase = inAppPurchases.Find(p => p.ProductIdentifier == productIdentifier);
        else
            CmuneDebug.LogError("Unable to find any In App Purchases in the receipt.");

        // Ensure that we have valid data (i.e. ProductId, Transaction
        if (!string.IsNullOrEmpty(inAppPurchase.TransactionIdentifier))
        {
            // Find the bundle we just purchased from the storeKitBundles, we'll use it's ID later
            var bundle = AllBundles.FirstOrDefault(p => p.BundleView.MacAppStoreUniqueId == productIdentifier);
            if (bundle != null)
            {
                // Attribute the pack to the player
                BuyMasBundle(bundle, inAppPurchase.TransactionIdentifier);
            }
            else
            {
                Debug.LogError("No MasBundle found with ProductIdentifier: " + productIdentifier);
            }
        }
        else
        {
            PopupSystem.ShowMessage(LocalizedStrings.Error, "There was an error completing your purchase.\nPlease contact support@cmune.com");
        }
    }

    private void OnMasProductListRequestFailed(string error)
    {
        CmuneDebug.LogError("Error Getting MAS Product List (" + error + ")");
    }

    private void OnMasProductListReceived(List<StoreKitMacProductModel> productList)
    {
        //productList.Sort(delegate(StoreKitMacProductModel s1, StoreKitMacProductModel s2) { return s1.Price.CompareTo(s2.Price); });

        foreach (var bundle in AllBundles)
        {
            Count = 0;
            var product = productList.Find(b => b.ProductIdentifier == bundle.BundleView.MacAppStoreUniqueId);
            if (product != null)
            {
                bundle.CurrencySymbol = product.CurrencySymbol;
                bundle.Price = product.Price;
                bundle.IsOwned = IsItemPackOwned(bundle.BundleView.BundleItemViews);
                Count++;
            }
            else
            {
                bundle.Price = string.Empty;
            }
        }
    }

    #endregion

    public class InAppPurchase
    {
        public string PurchaseDate = string.Empty;
        public string TransactionIdentifier = string.Empty;
        public string ProductIdentifier = string.Empty;
        public string Quantity = string.Empty;

        public InAppPurchase()
        {
            PurchaseDate = string.Empty;
            TransactionIdentifier = string.Empty;
            ProductIdentifier = string.Empty;
            Quantity = string.Empty;
        }

        public InAppPurchase(string purchaseDate, string transactionIdentifier, string productIdentifier, string quantity)
        {
            PurchaseDate = purchaseDate;
            TransactionIdentifier = transactionIdentifier;
            ProductIdentifier = productIdentifier;
            Quantity = quantity;
        }

        public override string ToString()
        {
            return string.Format("PurchaseDate={0} TransactionIdentifier={1} ProductIdentifier={2} Quantity={3}", PurchaseDate, TransactionIdentifier, ProductIdentifier, Quantity);
        }
    }
}
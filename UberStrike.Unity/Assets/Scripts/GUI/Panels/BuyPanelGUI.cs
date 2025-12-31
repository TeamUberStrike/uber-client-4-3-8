using Cmune.DataCenter.Common.Entities;
using Cmune.Util;
using UberStrike.Core.Models.Views;
using UberStrike.Core.Types;
using UnityEngine;

public class BuyPanelGUI : PanelGuiBase
{
    private const int WIDTH = 300;
    private const int BORDER = 10;
    private const int TITLE_HEIGHT = 100;

    private int Height;

    private IUnityItem _item;
    private ItemPriceGUI _price;
    private bool _autoEquip = false;
    private static bool _isBuyingItem = false;

    private Texture _priceIcon;
    private string _priceTag;

    private BuyingLocationType _buyingLocation;
    private BuyingRecommendationType _buyingRecommendation;


    private void OnGUI()
    {
        GUI.skin = BlueStonez.Skin;
        GUI.depth = (int)GuiDepth.Panel;

        Height = TITLE_HEIGHT + _price.Height + 100;

        DrawUnityItem(new Rect((Screen.width - WIDTH) / 2, (Screen.height - Height) / 2, WIDTH, Height));

        GuiManager.DrawTooltip();
    }

    private void DrawUnityItem(Rect rect)
    {
        GUI.BeginGroup(rect, GUIContent.none, BlueStonez.window_standard_grey38);
        {
            int exitButtonWidth = 20;
            if (ApplicationDataManager.Channel == ChannelType.Android || ApplicationDataManager.Channel == ChannelType.IPad || ApplicationDataManager.Channel == ChannelType.IPhone)
            {
                exitButtonWidth = 45;
            }

            if (GUI.Button(new Rect(rect.width - exitButtonWidth, 0, exitButtonWidth, exitButtonWidth), "X", BlueStonez.friends_hidden_button))
                Hide();

            DrawTitle(new Rect(BORDER, BORDER, rect.width - BORDER * 2, TITLE_HEIGHT));

            Rect priceRect = new Rect(BORDER * 3, BORDER + TITLE_HEIGHT, rect.width - BORDER * 6, rect.height - TITLE_HEIGHT);

            DrawPrice(priceRect);
            DrawBuyButton(new Rect(0, rect.height - 90, rect.width, 90));
        }
        GUI.EndGroup();

        //if clicked anything that is not a button, we exit
        if (Event.current.type == EventType.MouseDown && !rect.Contains(Event.current.mousePosition))
        {
            Hide();
            Event.current.Use();
        }
    }

    private void DrawTitle(Rect rect)
    {
        GUI.BeginGroup(rect);
        {
            GUI.Label(new Rect(0, 0, 48, 48), _item.Icon, BlueStonez.item_slot_large);

            float nameWidth = rect.width - 48 - BORDER * 2 - 32;

            GUI.Label(new Rect(48 + BORDER, 0, nameWidth, 48), _item.Name, BlueStonez.label_interparkmed_18pt_left_wrap);

            if (_item.ItemView.LevelLock > PlayerDataManager.PlayerLevel)
            {
                GUI.color = new Color(1, 1, 1, 0.5f);

                // offset for mobile to compensate larger exit button
                int offset = 0;
                if (ApplicationDataManager.Channel == ChannelType.Android || ApplicationDataManager.Channel == ChannelType.IPad || ApplicationDataManager.Channel == ChannelType.IPhone)
                    offset = 25;

                GUI.Label(new Rect(rect.width - BORDER - 32 - offset, 8, 32, 32), UberstrikeIcons.LevelLock);
                GUI.Label(new Rect(rect.width - BORDER - 31 - offset, 16, 24, 24), _item.ItemView.LevelLock.ToString(), BlueStonez.label_interparkmed_11pt);
                GUI.color = Color.white;
            }

            GUI.Label(new Rect(0, 48 + BORDER, rect.width, rect.height - 48 - BORDER), _item.ItemView.Description, BlueStonez.label_itemdescription);
        }
        GUI.EndGroup();
    }

    private void DrawPrice(Rect rect)
    {
        _price.Draw(rect);
    }

    private void DrawBuyButton(Rect rect)
    {
        GUI.BeginGroup(rect);
        {
            Rect buttonRect = new Rect((rect.width - 120) / 2, (rect.height - 30) / 2, 120, 30);

            GUITools.PushGUIState();
            GUI.enabled = !_isBuyingItem && _price.SelectedPriceOption != null;
            if (GUI.Button(buttonRect, GUIContent.none, BlueStonez.buttongold_large) && !_isBuyingItem)
            {
                _isBuyingItem = true;
                BuyItem(_item, _price.SelectedPriceOption, _buyingLocation, _buyingRecommendation, _autoEquip);
            }
            GUITools.PopGUIState();

            Rect textRect = new Rect((rect.width - 120) / 2, (rect.height - 20) / 2, 120, 20);
            GUI.Label(textRect, new GUIContent(_priceTag, _priceIcon), BlueStonez.label_interparkbold_13pt_black);
        }
        GUI.EndGroup();
    }

    private void OnPriceOptionSelected(ItemPrice price)
    {
        _priceTag = (price.Price == 0) ? "FREE" : string.Format("{0:N0}", price.Price);
        _priceIcon = price.Currency == UberStrikeCurrencyType.Points ? UberstrikeTextures.IconPoints20x20 : UberstrikeTextures.IconCredits20x20;
    }

    public static void BuyItem(IUnityItem item, ItemPrice price,
        BuyingLocationType buyingLocation = BuyingLocationType.Shop,
        BuyingRecommendationType recommendation = BuyingRecommendationType.Manual,
        bool autoEquip = false)
    {
        if (item.ItemView.IsConsumable)
        {
            int itemId = item.ItemId;
            UberStrike.WebService.Unity.ShopWebServiceClient.BuyPack(itemId, PlayerDataManager.CmidSecure,
                price.PackType, price.Currency, item.ItemType, buyingLocation, recommendation,
                (result) =>
                {
                    HandleBuyItem(item, result, autoEquip);
                },
                (ex) =>
                {
                    _isBuyingItem = false;
                    PanelManager.Instance.ClosePanel(PanelType.BuyItem);
                    DebugConsoleManager.SendExceptionReport(ex.Message, ex.StackTrace, "There was a problem. Please try again later.");
                });
        }
        else
        {
            //GoogleAnalytics.Instance.LogEvent("ui-buypanelgui-click", "Shop/" + recommendation + str);

            int itemId = item.ItemId;
            UberStrike.WebService.Unity.ShopWebServiceClient.BuyItem(itemId, PlayerDataManager.CmidSecure, price.Currency,
                price.Duration, item.ItemType, buyingLocation, recommendation,
                (result) =>
                {
                    HandleBuyItem(item, result, autoEquip);
                },
                (ex) =>
                {
                    _isBuyingItem = false;
                    PanelManager.Instance.ClosePanel(PanelType.BuyItem);
                    DebugConsoleManager.SendExceptionReport(ex.Message, ex.StackTrace, "There was a problem. Please try again later.");
                });
        }
    }

    private static void HandleBuyItem(IUnityItem item, int result, bool autoEquip)
    {
        _isBuyingItem = false;

        //called to close promotion event panels automatically after purchase
        CmuneEventHandler.Route(new BuyItemEvent() { Result = result });

        switch (result)
        {
            case UberstrikeBuyItemResult.Ok:
                MonoRoutine.Start(InventoryManager.Instance.StartUpdateInventoryAndEquipNewItem(item.ItemId, autoEquip));
                break;
            case UberstrikeBuyItemResult.AlreadyInInventory:// = 11;
                PopupSystem.ShowMessage(LocalizedStrings.ProblemBuyingItem, LocalizedStrings.YouAlreadyOwnThisItem, PopupSystem.AlertType.OK, HandleWebServiceError);
                break;
            case UberstrikeBuyItemResult.DisableForPermanent:// = 4;
                PopupSystem.ShowMessage(LocalizedStrings.ProblemBuyingItem, LocalizedStrings.ThisItemCannotBePurchasedPermanently, PopupSystem.AlertType.OK, HandleWebServiceError);
                break;
            case UberstrikeBuyItemResult.DisableForRent:// = 3;
                PopupSystem.ShowMessage(LocalizedStrings.ProblemBuyingItem, LocalizedStrings.ThisItemCannotBeRented, PopupSystem.AlertType.OK, HandleWebServiceError);
                break;
            case UberstrikeBuyItemResult.DisableInShop:// = 1;
                PopupSystem.ShowMessage(LocalizedStrings.ProblemBuyingItem, LocalizedStrings.ThisItemCannotBeRented, PopupSystem.AlertType.OK, HandleWebServiceError);
                break;
            case UberstrikeBuyItemResult.DurationDisabled:// = 5;
                PopupSystem.ShowMessage(LocalizedStrings.ProblemBuyingItem, LocalizedStrings.ThisItemCannotBePurchasedForDuration, PopupSystem.AlertType.OK, HandleWebServiceError);
                break;
            case UberstrikeBuyItemResult.InvalidAmount:// = 12;
                int maxAmount = (item.ItemView as UberStrikeItemQuickView).MaxOwnableAmount;
                PopupSystem.ShowMessage(LocalizedStrings.ProblemBuyingItem,
                    string.Format(LocalizedStrings.TheAmountYouTriedToPurchaseIsInvalid, maxAmount),
                    PopupSystem.AlertType.OK, HandleWebServiceError);
                break;
            case UberstrikeBuyItemResult.InvalidData:// = 14;
                PopupSystem.ShowMessage(LocalizedStrings.ProblemBuyingItem, LocalizedStrings.InvalidData, PopupSystem.AlertType.OK, HandleWebServiceError);
                break;
            case UberstrikeBuyItemResult.InvalidExpirationDate:// = 10;
                PopupSystem.ShowMessage(LocalizedStrings.ProblemBuyingItem, string.Format(LocalizedStrings.YouCannotPurchaseThisItemForMoreThanNDays, (CommonConfig.ItemMaximumDurationInDays.ToString())), PopupSystem.AlertType.OK, HandleWebServiceError);
                break;
            case UberstrikeBuyItemResult.InvalidMember:// = 9;
                PopupSystem.ShowMessage(LocalizedStrings.ProblemBuyingItem, LocalizedStrings.AccountIsInvalid, PopupSystem.AlertType.OK, HandleWebServiceError);
                break;
            case UberstrikeBuyItemResult.IsNotForSale:// = 7;
                PopupSystem.ShowMessage(LocalizedStrings.ProblemBuyingItem, LocalizedStrings.ThisItemIsNotForSale, PopupSystem.AlertType.OK, HandleWebServiceError);
                break;
            case UberstrikeBuyItemResult.NoStockRemaining:// = 13;
                PopupSystem.ShowMessage(LocalizedStrings.ProblemBuyingItem, LocalizedStrings.ThisItemIsOutOfStock, PopupSystem.AlertType.OK, HandleWebServiceError);
                break;
            case UberstrikeBuyItemResult.NotEnoughCurrency:// = 8;
                PopupSystem.ShowMessage(LocalizedStrings.ProblemBuyingItem, LocalizedStrings.YouDontHaveEnoughPointsOrCreditsToPurchaseThisItem, PopupSystem.AlertType.OKCancel, HandleWebServiceError, LocalizedStrings.OkCaps, ApplicationDataManager.Instance.OpenBuyCredits, "GET CREDITS");
                break;
            case UberstrikeBuyItemResult.PackDisabled:// = 6;
                PopupSystem.ShowMessage(LocalizedStrings.ProblemBuyingItem, LocalizedStrings.ThisPackIsDisabled, PopupSystem.AlertType.OK, HandleWebServiceError);
                break;
            case UberstrikeBuyItemResult.InvalidLevel:// = 100;
                PopupSystem.ShowMessage(LocalizedStrings.ProblemBuyingItem, LocalizedStrings.YourLevelIsTooLowToBuyThisItem, PopupSystem.AlertType.OK, HandleWebServiceError);
                break;
            default:
                PopupSystem.ShowMessage(LocalizedStrings.ProblemBuyingItem, LocalizedStrings.DataError, PopupSystem.AlertType.OK, HandleWebServiceError);
                break;
        }

        PanelManager.Instance.ClosePanel(PanelType.BuyItem);
    }

    private static void HandleWebServiceError() { }

    public void SetItem(IUnityItem item, BuyingLocationType location, BuyingRecommendationType recommendation, bool autoEquip = false)
    {
        _autoEquip = autoEquip;
        _item = item;
        _buyingLocation = location;
        _buyingRecommendation = recommendation;
        _isBuyingItem = false;

        if (item != null && item.ItemView.Prices.Count > 0)
        {
            if (item.ItemView.IsConsumable)
            {
                _price = new PackItemPriceGUI(item, OnPriceOptionSelected);
            }
            else
            {
                _price = new RentItemPriceGUI(item, OnPriceOptionSelected);
            }
        }
        else
        {
            Debug.LogError("Item is null or not for sale");
        }
    }
}
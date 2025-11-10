using System;
using Cmune.DataCenter.Common.Entities;
using UberStrike.Core.Models.Views;
using UnityEngine;

public abstract class ItemPriceGUI
{
    protected bool _levelLocked;
    protected string _tooltip = string.Empty;
    protected ItemPrice _selectedPrice;
    protected Action<ItemPrice> _onPriceSelected;

    public ItemPriceGUI(int levelLock, Action<ItemPrice> onPriceSelected)
    {
        if (levelLock > PlayerDataManager.PlayerLevel)
        {
            _levelLocked = true;
            _tooltip = string.Format("Not so fast, squirt!\n\nYou need to be Level {0} to buy this item using points.\n\nGet fragging!", levelLock);
        }

        _onPriceSelected = (price) => { _selectedPrice = price; };
        _onPriceSelected += onPriceSelected;
    }

    public int Height { get; protected set; }

    public ItemPrice SelectedPriceOption { get { return _selectedPrice; } }

    public abstract void Draw(Rect rect);

    protected int DrawPrice(ItemPrice price, float width, int y)
    {
        string priceTag = price.Price > 0 ? string.Format(" {0:N0}", price.Price) : " FREE";
        Texture icon = price.Currency == UberStrikeCurrencyType.Points ? UberstrikeTextures.IconPoints20x20 : UberstrikeTextures.IconCredits20x20;

        GUIContent text = new GUIContent(priceTag, icon);
        GUI.Label(new Rect(width, y, width, 20), text, BlueStonez.label_itemdescription);

        if (price.Price > 0 && price.Discount > 0)
        {
            string discount = string.Format(LocalizedStrings.DiscountPercentOff, price.Discount);
            GUI.color = ColorScheme.UberStrikeYellow;
            GUI.Label(new Rect(width + 80, y + 5, width, 20), discount, BlueStonez.label_itemdescription);
            GUI.color = Color.white;
        }

        return y += 24;
    }
}
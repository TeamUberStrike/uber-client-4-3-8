using System;
using System.Collections.Generic;
using Cmune.DataCenter.Common.Entities;
using UberStrike.Core.Models.Views;
using UnityEngine;

public class RentItemPriceGUI : ItemPriceGUI
{
    private List<ItemPrice> _prices;

    public RentItemPriceGUI(IUnityItem item, Action<ItemPrice> onPriceSelected)
        : base(item.ItemView.LevelLock, onPriceSelected)
    {
        _prices = new List<ItemPrice>(item.ItemView.Prices);

        if (_prices.Count > 0)
            _onPriceSelected(_prices[_prices.Count - 1]);
    }

    public override void Draw(Rect rect)
    {
        GUI.BeginGroup(rect);
        {
            int y = 30;

            if (_prices.Exists(p => p.Duration != BuyingDurationType.Permanent))
            {
                GUI.Label(new Rect(0, 0, rect.width, 16), "Limited Use", BlueStonez.label_interparkbold_16pt_left);

                foreach (var price in _prices)
                {
                    if (price.Duration == BuyingDurationType.Permanent) continue;

                    GUIContent content = new GUIContent(ShopUtils.PrintDuration(price.Duration));

                    if (price.Currency == UberStrikeCurrencyType.Points && _levelLocked)
                    {
                        GUI.enabled = false;
                        content.tooltip = _tooltip;
                    }

                    if (GUI.Toggle(new Rect(0, y, rect.width, 20), (_selectedPrice == price), content, BlueStonez.toggle))
                    {
                        if (price != _selectedPrice)
                        {
                            _onPriceSelected(price);
                        }
                    }

                    y = DrawPrice(price, rect.width * 0.5f, y);

                    GUI.enabled = true;
                }

                y += 20;
            }

            if (_prices.Exists(p => p.Duration == BuyingDurationType.Permanent))
            {
                GUI.Label(new Rect(0, y, rect.width, 16), "Unlimited Use", BlueStonez.label_interparkbold_16pt_left);

                y += 30;

                foreach (var price in _prices)
                {
                    if (price.Duration != BuyingDurationType.Permanent) continue;

                    string tooltip = string.Empty;

                    if (GUI.Toggle(new Rect(0, y, rect.width, 20), (_selectedPrice == price), new GUIContent(LocalizedStrings.Permanent, tooltip), BlueStonez.toggle))
                    {
                        if (price != _selectedPrice)
                        {
                            _onPriceSelected(price);
                        }
                    }

                    y = DrawPrice(price, rect.width * 0.5f, y);
                }
            }

            Height = y;
        }
        GUI.EndGroup();
    }

    private string GetRentDuration(BuyingDurationType duration)
    {
        string str = string.Empty;

        switch (duration)
        {
            case BuyingDurationType.OneDay:
                str = LocalizedStrings.OneDay;
                break;

            case BuyingDurationType.SevenDays:
                str = LocalizedStrings.SevenDays;
                break;
        }

        return str;
    }
}
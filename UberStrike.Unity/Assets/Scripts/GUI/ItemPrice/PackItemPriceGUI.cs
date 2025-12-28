using System;
using System.Collections.Generic;
using Cmune.DataCenter.Common.Entities;
using UberStrike.Core.Models.Views;
using UnityEngine;

public class PackItemPriceGUI : ItemPriceGUI
{
    private List<ItemPrice> _prices;

    public PackItemPriceGUI(IUnityItem item, Action<ItemPrice> onPriceSelected)
        : base(item.ItemView.LevelLock, onPriceSelected)
    {
        _prices = new List<ItemPrice>(item.ItemView.Prices);

        if (_prices.Count > 1)
            _onPriceSelected(_prices[1]);
        else
            _onPriceSelected(_prices[0]);
    }

    public override void Draw(UnityEngine.Rect rect)
    {
        GUI.BeginGroup(rect);
        {
            int y = 30;

            GUI.Label(new Rect(0, 0, rect.width, 16), "Purchase Options", BlueStonez.label_interparkbold_16pt_left);

            foreach (var price in _prices)
            {
                GUIContent content = new GUIContent(price.Amount + " Uses");

                if (_levelLocked && price.Currency == UberStrikeCurrencyType.Points)
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

            Height = y;
        }
        GUI.EndGroup();
    }
}
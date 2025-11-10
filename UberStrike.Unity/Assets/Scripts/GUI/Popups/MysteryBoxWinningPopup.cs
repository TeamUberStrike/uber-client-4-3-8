using UnityEngine;
using Cmune.DataCenter.Common.Entities;
using System.Collections.Generic;

public class MysteryBoxWinningPopup : LotteryWinningPopup
{
    private LotteryItemGrid _grid;

    public MysteryBoxWinningPopup(DynamicTexture image, MysteryBoxShopItem item, List<bool> highlight)
        : base(image, item)
    {
        _grid = new LotteryItemGrid(item.View.MysteryBoxItems, item.View.CreditsAttributed, item.View.PointsAttributed);
        _grid.HighlightState = highlight;
        _grid.SetTooltip(_tooltip);

        Text = "You find your winnings in the inventory!";
    }

    protected override void DrawItemGrid(Rect rect, bool showItems)
    {
        _grid.Show = showItems;
        _grid.Draw(rect);
    }
}
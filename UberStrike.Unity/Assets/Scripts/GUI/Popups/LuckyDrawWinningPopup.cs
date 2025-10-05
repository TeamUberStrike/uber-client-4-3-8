using UnityEngine;
using Cmune.DataCenter.Common.Entities;

public class LuckyDrawWinningPopup : LotteryWinningPopup
{
    private LotteryItemGrid _grid;

    public LuckyDrawWinningPopup(string text, DynamicTexture image, LotteryShopItem item, LuckyDrawSetUnityView luckyDrawSet)
        : base(image, item)
    {
        _grid = new LotteryItemGrid(luckyDrawSet.LuckyDrawSetItems, luckyDrawSet.CreditsAttributed, luckyDrawSet.PointsAttributed);
        _grid.SetTooltip(_tooltip);

        Text = "You find your winnings in the inventory!";
    }

    protected override void DrawItemGrid(Rect rect, bool showItems)
    {
        _grid.Show = showItems;
        _grid.Draw(rect);
    }
}
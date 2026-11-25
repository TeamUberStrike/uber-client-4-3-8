using System.Collections.Generic;
using Cmune.DataCenter.Common.Entities;
using UnityEngine;

public class LuckyDrawPopup : LotteryPopupDialog
{
    private LuckyDrawShopItem luckyDraw;
    private List<LotteryItemGrid> _itemGrids;
    private ItemToolTip itemTooltip = new ItemToolTip();
    private Vector2Anim scroll = new Vector2Anim();
    private const int spacing = 20;
    private const int offset = 30;

    private int _luckyDrawResult = -1;

    public bool ShowNavigationArrows { get; set; }
    public string HelpText { get; set; }

    public LuckyDrawPopup(LuckyDrawShopItem luckyDraw)
    {
        this.luckyDraw = luckyDraw;
        this.Title = luckyDraw.Name;
        this.Text = luckyDraw.View.Description;

        Width = offset + offset + luckyDraw.Sets.Count * (ItemPackGuiUtil.Columns * 48 + spacing) - spacing;
        Height = ApplicationDataManager.MinimalHeight - GlobalUIRibbon.HEIGHT - 10;


        ShowNavigationArrows = true;
        HelpText = "Try your luck at winning one of the prizes above!\nBe careful not to play for items you already own permanently!";

        _itemGrids = new List<LotteryItemGrid>(luckyDraw.Sets.Count);
        foreach (var luckyDrawSet in luckyDraw.View.LuckyDrawSets)
        {
            LotteryItemGrid grid = new LotteryItemGrid(luckyDrawSet.LuckyDrawSetItems, luckyDrawSet.CreditsAttributed, luckyDrawSet.PointsAttributed);
            grid.SetTooltip(itemTooltip);
            _itemGrids.Add(grid);
        }

        // don;t show button for login lucky draws
        _showExitButton = luckyDraw.View.Category != BundleCategoryType.Login && luckyDraw.View.Category != BundleCategoryType.Signup;

        IsVisible = true;

        if (_showExitButton)
        {
            LotteryAudioPlayer.Instance.Play();
            BackgroundMusicPlayer.Instance.Stop();
        }
    }

    protected override void DrawPlayGUI(Rect rect)
    {
        Width = offset + offset + luckyDraw.Sets.Count * (ItemPackGuiUtil.Columns * 48 + spacing) - spacing;

        //title
        GUI.color = ColorScheme.HudTeamBlue;
        float titleWidth = BlueStonez.label_interparkbold_18pt.CalcSize(new GUIContent(Title)).x * 2.5f;
        GUI.DrawTexture(new Rect((rect.width - titleWidth) * 0.5f, -29, titleWidth, 100), HudTextures.WhiteBlur128);
        GUI.color = Color.white;
        GUITools.OutlineLabel(new Rect(0, 10, rect.width, 30), Title, BlueStonez.label_interparkbold_18pt, 1, Color.white, ColorScheme.GuiTeamBlue.SetAlpha(0.5f));

        //text
        GUI.Label(new Rect(30, 35, rect.width - 60, 40), Text, BlueStonez.label_interparkbold_16pt);

        int i = 0;
        int width = ItemPackGuiUtil.Columns * 48;
        int xOffset = offset;
        int height = 323;
        foreach (var set in luckyDraw.Sets)
        {
            GUI.BeginGroup(new Rect(xOffset, 75, width, height), BlueStonez.item_slot_large);
            {
                Rect imgRect = new Rect((width - LotteryManager.IMG_WIDTH) / 2, (height - LotteryManager.IMG_HEIGHT) / 2, LotteryManager.IMG_WIDTH, LotteryManager.IMG_HEIGHT);

                set.Image.Draw(imgRect);

                _itemGrids[i].Show = (imgRect.Contains(Event.current.mousePosition) || ApplicationDataManager.IsMobile) && !IsUIDisabled;

                if (set.View.ExposeItemsToPlayers)
                {
                    _itemGrids[i++].Draw(new Rect(0, 0, width, height));
                }
                xOffset += width + spacing;
            }
            GUI.EndGroup();
        }

        if (luckyDraw.Price.Price > 0)
        {
            if (GUI.Button(new Rect(rect.width * 0.5f - 70, rect.height - 47, 140, 30), luckyDraw.Price.PriceTag(false), BlueStonez.buttongold_large_price))
            {
                Play();
            }
        }
        else
        {
            if (GUI.Button(new Rect(rect.width * 0.5f - 70, rect.height - 47, 140, 30), "PLAY", BlueStonez.buttongold_large))
            {
                Play();
            }
        }

        if (ShowNavigationArrows)
        {
            DrawNaviArrows(rect, luckyDraw);
        }
    }

    public override void OnAfterGUI()
    {
        itemTooltip.OnGui();
        scroll.Update();
    }

    public override LotteryWinningPopup ShowReward()
    {
        var result = luckyDraw.Sets.Find(s => s.Id == _luckyDrawResult);
        LuckyDrawWinningPopup popup = new LuckyDrawWinningPopup(Text, result.Image, luckyDraw, result.View);
        return popup;
    }

    private void Play()
    {
        if (luckyDraw.Price.Currency == UberStrikeCurrencyType.Credits && luckyDraw.Price.Price > PlayerDataManager.CreditsSecure)
        {
            PopupSystem.ShowMessage(LocalizedStrings.ProblemBuyingItem, LocalizedStrings.YouNeedMoreCreditsToBuyThisItem,
                PopupSystem.AlertType.OKCancel, ApplicationDataManager.Instance.OpenBuyCredits, "GET CREDITS", null,
                LocalizedStrings.CancelCaps, PopupSystem.ActionType.Positive);
        }
        else if (luckyDraw.Price.Currency == UberStrikeCurrencyType.Points && luckyDraw.Price.Price > PlayerDataManager.PointsSecure)
        {
            PopupSystem.ShowMessage(LocalizedStrings.ProblemBuyingItem, LocalizedStrings.YouNeedToEarnMorePointsToBuyThisItem,
                PopupSystem.AlertType.OK, LocalizedStrings.OkCaps, null);
        }
        else
        {
            RollLuckyDraw();
        }
    }

    private void RollLuckyDraw()
    {
        if (_onLotteryRolled != null)
        {
            _onLotteryRolled();
        }

        UberStrike.WebService.Unity.ShopWebServiceClient.RollLuckyDraw(PlayerDataManager.CmidSecure, luckyDraw.View.Id, ApplicationDataManager.Channel,
            OnLuckyDrawReturn,
            (ex) =>
            {
                Debug.LogError("ERROR IN StartPlaying: " + ex.Message);
            });
    }

    private void OnLuckyDrawReturn(int result)
    {
        _luckyDrawResult = result;
        IsLotteryReturned = true;
    }
}
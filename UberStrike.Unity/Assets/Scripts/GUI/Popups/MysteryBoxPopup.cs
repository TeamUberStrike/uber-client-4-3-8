using Cmune.Util;
using UnityEngine;
using System.Collections.Generic;
using Cmune.DataCenter.Common.Entities;
using System.Collections;

public class MysteryBoxPopup : LotteryPopupDialog
{
    private MysteryBoxShopItem mysteryBox;
    private ItemToolTip itemTooltip = new ItemToolTip();
    private Vector2Anim scroll = new Vector2Anim();
    private LotteryItemGrid _lotteryItemGrid;

    private List<bool> _rewardHighlight;

    public MysteryBoxPopup(MysteryBoxShopItem mysteryBox)
    {
        this.mysteryBox = mysteryBox;
        this.Title = mysteryBox.Name;
        this.Text = mysteryBox.View.Description;

        Width = 100 + ItemPackGuiUtil.Columns * 48;
        Height = ApplicationDataManager.MinimalHeight - GlobalUIRibbon.HEIGHT - 10;

        _lotteryItemGrid = new LotteryItemGrid(mysteryBox.View.MysteryBoxItems, mysteryBox.View.CreditsAttributed, mysteryBox.View.PointsAttributed);
        _lotteryItemGrid.SetTooltip(itemTooltip);

        IsVisible = true;

        if (mysteryBox.Category != BundleCategoryType.Login && mysteryBox.Category != BundleCategoryType.Signup)
        {
            LotteryAudioPlayer.Instance.Play();
            BackgroundMusicPlayer.Instance.Stop();
        }
    }

    protected override void DrawPlayGUI(Rect rect)
    {
        //title
        GUI.color = ColorScheme.HudTeamBlue;
        float titleWidth = BlueStonez.label_interparkbold_18pt.CalcSize(new GUIContent(Title)).x * 2.5f;
        GUI.DrawTexture(new Rect((rect.width - titleWidth) * 0.5f, -29, titleWidth, 100), HudTextures.WhiteBlur128);
        GUI.color = Color.white;
        GUITools.OutlineLabel(new Rect(0, 10, rect.width, 30), Title, BlueStonez.label_interparkbold_18pt, 1, Color.white, ColorScheme.GuiTeamBlue.SetAlpha(0.5f));

        //text
        GUI.Label(new Rect(30, 35, rect.width - 60, 40), Text, BlueStonez.label_interparkbold_16pt);

        int width = ItemPackGuiUtil.Columns * 48;
        int offset = (Width - width - 6) / 2;
        int height = 323;
        GUI.BeginGroup(new Rect(offset, 75, width, height), BlueStonez.item_slot_large);
        {
            Rect imgRect = new Rect((width - LotteryManager.IMG_WIDTH) / 2, (height - LotteryManager.IMG_HEIGHT) / 2, LotteryManager.IMG_WIDTH, LotteryManager.IMG_HEIGHT);
            mysteryBox.Image.Draw(imgRect);

            _lotteryItemGrid.Show = imgRect.Contains(Event.current.mousePosition) && !IsUIDisabled;

            if (mysteryBox.View.ExposeItemsToPlayers)
            {
                _lotteryItemGrid.Draw(new Rect(0, 0, width, height));
            }
        }
        GUI.EndGroup();

        if (GUI.Button(new Rect(rect.width * 0.5f - 70, rect.height - 47, 140, 30), mysteryBox.Price.PriceTag(false), BlueStonez.buttongold_large_price))
        {
            Play();
        }

        DrawNaviArrows(rect, mysteryBox);
    }

    public override void OnAfterGUI()
    {
        itemTooltip.OnGui();
        scroll.Update();
    }

    public override LotteryWinningPopup ShowReward()
    {
        return new MysteryBoxWinningPopup(mysteryBox.Image, mysteryBox, _rewardHighlight);
    }

    private void Play()
    {
        if (mysteryBox.Price.Currency == UberStrikeCurrencyType.Credits && mysteryBox.Price.Price > PlayerDataManager.CreditsSecure)
        {
            PopupSystem.ShowMessage(LocalizedStrings.ProblemBuyingItem, LocalizedStrings.YouNeedMoreCreditsToBuyThisItem,
                PopupSystem.AlertType.OKCancel, ApplicationDataManager.Instance.OpenBuyCredits, "GET CREDITS", null,
                LocalizedStrings.CancelCaps, PopupSystem.ActionType.Positive);
        }
        else if (mysteryBox.Price.Currency == UberStrikeCurrencyType.Points && mysteryBox.Price.Price > PlayerDataManager.PointsSecure)
        {
            PopupSystem.ShowMessage(LocalizedStrings.ProblemBuyingItem, LocalizedStrings.YouNeedToEarnMorePointsToBuyThisItem,
                PopupSystem.AlertType.OK, LocalizedStrings.OkCaps, null);
        }
        else
        {
            RollMysteryBox();
        }
    }

    private void RollMysteryBox()
    {
        if (_onLotteryRolled != null)
        {
            _onLotteryRolled();
        }

        UberStrike.WebService.Unity.ShopWebServiceClient.RollMysteryBox(PlayerDataManager.CmidSecure, mysteryBox.View.Id, ApplicationDataManager.Channel,
            OnMysteryBoxReturned,
            (ex) =>
            {
                Debug.LogError("ERROR IN StartPlaying: " + ex.Message);
            });
    }

    private void OnMysteryBoxReturned(List<MysteryBoxWonItemUnityView> items)
    {
        IsLotteryReturned = true;

        _rewardHighlight = new List<bool>(_lotteryItemGrid.Items.Count);
        for (int i = 0; i < _lotteryItemGrid.Items.Count; i++)
        {
            _rewardHighlight.Add(false);
        }

        foreach (var view in items)
        {
            int index = _lotteryItemGrid.Items.FindIndex((t) => (t.ItemId > 0 && t.ItemId == view.ItemIdWon) || (t.ItemId == 0 && (t.Credits == view.CreditWon || t.Points == view.PointWon)));
            if (index >= 0)
            {
                _rewardHighlight[index] = true;
            }
        }
    }
}
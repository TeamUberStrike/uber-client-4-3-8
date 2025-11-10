using System.Collections.Generic;
using UnityEngine;
using Cmune.DataCenter.Common.Entities;

public class MasBundlePopup : LotteryPopupDialog
{
    private MasBundleUnityView masBundle;
    private ItemToolTip itemTooltip = new ItemToolTip();
    private LotteryItemGrid _lotteryItemGrid;

    public MasBundlePopup(MasBundleUnityView masBundle)
    {
        this.masBundle = masBundle;
        this.Title = masBundle.BundleView.Name;
        this.Text = masBundle.BundleView.Description;

        Width = 100 + ItemPackGuiUtil.Columns * 48;
        Height = ApplicationDataManager.MinimalHeight - GlobalUIRibbon.HEIGHT - 10;

        _lotteryItemGrid = new LotteryItemGrid(masBundle.BundleView.BundleItemViews, 0, 0);
        _lotteryItemGrid.SetTooltip(itemTooltip);
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
        GUI.Label(new Rect(30, 35, rect.width - 60, 40), Text, BlueStonez.label_interparkbold_13pt);

        int width = ItemPackGuiUtil.Columns * 48;
        int offset = (Width - width - 6) / 2;
        int height = 323;
        GUI.BeginGroup(new Rect(offset, 75, width, height), BlueStonez.item_slot_large);
        {
            //image
            Rect imgRect = new Rect((width - LotteryManager.IMG_WIDTH) / 2, (height - LotteryManager.IMG_HEIGHT) / 2, LotteryManager.IMG_WIDTH, LotteryManager.IMG_HEIGHT);
            masBundle.Image.Draw(imgRect);

            _lotteryItemGrid.Show = imgRect.Contains(Event.current.mousePosition);

            _lotteryItemGrid.Draw(new Rect(0, 0, width, height));
        }
        GUI.EndGroup();

        if (GUI.Button(new Rect(rect.width * 0.5f - 95, rect.height - 42, 20, 20), GUIContent.none, BlueStonez.button_left))
        {
            SfxManager.Play2dAudioClip(SoundEffectType.UIButtonClick);
            PopupSystem.HideMessage(this);
            var next = MasBundleManager.Instance.GetPreviousItem(masBundle);
            if (next != null) PopupSystem.Show(new MasBundlePopup(next));
        }

        if (GUI.Button(new Rect(rect.width * 0.5f + 75, rect.height - 42, 20, 20), GUIContent.none, BlueStonez.button_right))
        {
            SfxManager.Play2dAudioClip(SoundEffectType.UIButtonClick);
            PopupSystem.HideMessage(this);
            var next = MasBundleManager.Instance.GetNextItem(masBundle);
            if (next != null) PopupSystem.Show(new MasBundlePopup(next));
        }

        GUI.enabled = !masBundle.IsOwned && masBundle.IsValid && GUITools.SaveClickIn(1);
        if (GUI.Button(new Rect(rect.width * 0.5f - 70, rect.height - 47, 140, 30), (masBundle.IsOwned) ? new GUIContent("Purchased") : new GUIContent(masBundle.CurrencySymbol + masBundle.Price, masBundle.BundleView.Description), BlueStonez.buttongold_large_price))
        {
            BuyBundle();
        }
        GUI.enabled = true;
    }

    public override void OnAfterGUI()
    {
        itemTooltip.OnGui();
    }

    private void BuyBundle()
    {
        PopupSystem.HideMessage(this);
        if (MasBundleManager.Instance.CanMakeMasPayments)
        {
            GUITools.Clicked();
            if (ScreenResolutionManager.IsFullScreen) ScreenResolutionManager.IsFullScreen = false;
            MasBundleManager.Instance.BuyStoreKitItem(masBundle.BundleView.MacAppStoreUniqueId, masBundle.BundleView.Id);
        }
        else
        {
            PopupSystem.ShowMessage(LocalizedStrings.Error, "Sorry, it appears you are unable to make Mac App Store payments at this time.", PopupSystem.AlertType.OK);
        }
    }

    public override LotteryWinningPopup ShowReward()
    {
        return null;
    }
}
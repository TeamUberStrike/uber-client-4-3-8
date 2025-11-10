using System.Collections;
using Cmune.DataCenter.Common.Entities;
using Cmune.Util;
using UberStrike.Core.Models.Views;
using UberStrike.DataCenter.Common.Entities;
using UnityEngine;

public class PromotionPopupDialog : BaseEventPopup
{
    private PromotionItemGUI _itemGui;
    private Texture2D _texture;

    public PromotionPopupDialog(WeeklySpecialView weeklySpecial)
        : this(weeklySpecial.Title, weeklySpecial.Text, weeklySpecial.ItemId, weeklySpecial.ImageUrl)
    { }

    public PromotionPopupDialog(string title, string text, int itemId, string imageUrl)
    {
        this._itemGui = new PromotionItemGUI(ItemManager.Instance.GetItemInShop(itemId), BuyingLocationType.HomeScreen);

        this._texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
        this.Title = title;
        this.Text = text;

        float aspect = 320 / 430f;
        this.Width = 700;
        this.Height = Mathf.RoundToInt(Width * aspect * 0.5f);

        MonoRoutine.Start(DownloadTexture(imageUrl));

        CmuneEventHandler.AddListener<BuyItemEvent>(OnBuyItemEvent);
    }

    private void OnBuyItemEvent(BuyItemEvent ev)
    {
        PopupSystem.HideMessage(this);
    }

    public override void OnHide()
    {
        CmuneEventHandler.RemoveListener<BuyItemEvent>(OnBuyItemEvent);
    }

    protected override void DrawGUI(UnityEngine.Rect rect)
    {
        //promotion image
        GUI.DrawTexture(new Rect(0, 0, rect.width / 2, rect.height), _texture);

        //title
        GUI.Label(new Rect(rect.width / 2, 20, rect.width / 2, 20), Title, BlueStonez.label_interparkbold_16pt);

        //description
        GUI.Label(new Rect(rect.width / 2 + 15, 60, rect.width / 2 - 30, 100), Text, BlueStonez.label_interparkbold_13pt_left);

        //item
        _itemGui.Draw(new Rect(rect.width / 2 + 10, rect.height - 64, rect.width / 2 - 20, 54), true);
    }

    private IEnumerator DownloadTexture(string url)
    {
        WWW www = new WWW(url);

        yield return www;

        if (string.IsNullOrEmpty(www.error))
        {
            www.LoadImageIntoTexture(_texture);
        }
        else
        {
            Debug.LogError("Texture download failed!: " + www.error);
        }
    }
}
using UnityEngine;
using Cmune.Util;
using Cmune.DataCenter.Common.Entities;

public abstract class LotteryWinningPopup : IPopupDialog
{
    private const int Width = 100 + ItemPackGuiUtil.Columns * 48;
    private const int Height = ApplicationDataManager.MinimalHeight - GlobalUIRibbon.HEIGHT - 10;

    private float _deltaY;
    private DynamicTexture _bkImage;
    protected ItemToolTip _tooltip;
    private LotteryShopItem _shopItem;

    public string Text { get; set; }

    public string Title { get; set; }

    public GuiDepth Depth
    {
        get { return GuiDepth.Event; }
    }

    public LotteryWinningPopup(DynamicTexture image, LotteryShopItem shopItem)
    {
        Title = LocalizedStrings.Congratulations.ToUpper();

        _bkImage = image;
        _shopItem = shopItem;
        _tooltip = new ItemToolTip();
    }

    public void OnGUI()
    {
        Rect rect = GetPosition();

        GUI.Box(rect, GUIContent.none, BlueStonez.window);
        GUI.BeginGroup(rect);
        {
            if (GUI.Button(new Rect(rect.width - 20, 0, 20, 20), "X", BlueStonez.friends_hidden_button))
            {
                PopupSystem.HideMessage(this);
                LotteryAudioPlayer.Instance.Stop();
                BackgroundMusicPlayer.Instance.Play();
            }

            //title
            GUI.color = ColorScheme.HudTeamBlue;
            float titleWidth = BlueStonez.label_interparkbold_18pt.CalcSize(new GUIContent(Title)).x * 2.5f;
            GUI.DrawTexture(new Rect((rect.width - titleWidth) * 0.5f, -29, titleWidth, 100), HudTextures.WhiteBlur128);
            GUI.color = Color.white;
            GUITools.OutlineLabel(new Rect(0, 15, rect.width, 30), Title, BlueStonez.label_interparkbold_32pt, 1, Color.white, ColorScheme.GuiTeamBlue);

            //text
            GUI.Label(new Rect(30, 40, rect.width - 60, 40), Text, BlueStonez.label_interparkbold_16pt);

            int width = ItemPackGuiUtil.Columns * 48;
            int offset = (Width - width - 6) / 2;
            int height = 323;
            GUI.BeginGroup(new Rect(offset, 75, width, height), BlueStonez.item_slot_large);
            {
                _bkImage.Draw(new Rect((width - LotteryManager.IMG_WIDTH) / 2, (height - LotteryManager.IMG_HEIGHT) / 2, LotteryManager.IMG_WIDTH, LotteryManager.IMG_HEIGHT));

                var gridRect = new Rect(0, 0, width, height);
                DrawItemGrid(gridRect, true);
            }
            GUI.EndGroup();

            if (GUI.Button(new Rect(offset, rect.height - 55, 120, 32), "PLAY AGAIN", BlueStonez.button_green))
            {
                PopupSystem.HideMessage(this);
                if (_shopItem != null)
                {
                    if (_shopItem.Category == BundleCategoryType.Login || _shopItem.Category == BundleCategoryType.Signup)
                        LotteryManager.Instance.ShowNextItem(_shopItem);
                    else
                        _shopItem.Use();
                }
            }

            if (GUI.Button(new Rect(rect.width - 126 - offset, rect.height - 55, 120, 32), "DONE", BlueStonez.button))
            {
                PopupSystem.HideMessage(this);
                LotteryAudioPlayer.Instance.Stop();
                BackgroundMusicPlayer.Instance.Play();

                CmuneEventHandler.Route(new SelectShopAreaEvent()
                {
                    ShopArea = ShopArea.Inventory,
                });
            }
        }
        GUI.EndGroup();

        _tooltip.OnGui();
    }

    public void OnHide()
    {
    }

    public void SetYOffset(float offset)
    {
        _deltaY = offset;
    }

    protected abstract void DrawItemGrid(Rect rect, bool showItems);

    private Rect GetPosition()
    {
        float offsetX = (Screen.width - Width) * 0.5f;
        float offsetY = GlobalUIRibbon.HEIGHT + (Screen.height - GlobalUIRibbon.HEIGHT - Height) * 0.5f;

        return new Rect(offsetX, offsetY - _deltaY, Width, Height);
    }
}
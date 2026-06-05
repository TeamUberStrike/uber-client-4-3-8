using System.Collections;
using UnityEngine;

public class HomePageGUI : MonoBehaviour
{
    #region Fields

    private const int PromotionWidth = 320;
    private const float PromoTextureAspect = 323 / 430f;

    private bool _mainMenuEnabled;
    private float mainMenuX = -250;
    private bool _hasCheckedPerformance = false;

    private ItemToolTip _itemTooltip = new ItemToolTip();

    #endregion Fields

    private void OnEnable()
    {
        _mainMenuEnabled = true;
        StartCoroutine(AnimateMainMenu(0.25f));
        if (_hasCheckedPerformance == false)
        {
            PerformanceTest.Instance.enabled = true;
            _hasCheckedPerformance = true;
        }

        BackgroundMusicPlayer.Instance.Play();
        AvatarBuilder.Instance.UpdateLocalAvatar();
    }

    private IEnumerator AnimateMainMenu(float time)
    {
        float t = 0;
        while (t < time)
        {
            t += Time.deltaTime;
            mainMenuX = Mathf.Lerp(-250, 0, (t / time) * (t / time));
            yield return new WaitForEndOfFrame();
        }
    }

    private void OnGUI()
    {
        GUI.depth = (int)GuiDepth.Page;
        Matrix4x4 scaleMatrix = MobileMenuScale.Begin();

        // Draw the Main Menu
        if (_mainMenuEnabled)
        {
            GUI.enabled = !PanelManager.IsAnyPanelOpen;

            DrawWeeklySpecial();

            if (ApplicationDataManager.ApplicationOptions.UseClassicLobby)
            {
                DrawClassicMainMenu();
            }
            else
            {
            int buttonCount = (Application.platform != RuntimePlatform.WebGLPlayer) ? 4 : 3;
            int buttonSpacing = 59 + 8;
            int topOffset = 14;
            int height = topOffset + (buttonSpacing * buttonCount);

            // Anchor against the virtual (scaled) screen height so the menu stays vertically centred
            // below the (equally scaled) ribbon under the mobile menu scale.
            int top = Mathf.RoundToInt((((MobileMenuScale.VirtualHeight - GlobalUIRibbon.HEIGHT) * 0.5f) + GlobalUIRibbon.HEIGHT) - (height * 0.5f));

            GUI.BeginGroup(new Rect(mainMenuX, top, 310, height));
            {
                GUI.color = new Color(1, 1, 1, 1);

                // Draw Play button
                if (MainMenuButton(new Vector2(0, topOffset), new GUIContent(LocalizedStrings.PlayCaps, LocalizedStrings.MainMenuPlayTooltip), UberstrikeIcons.MainMenuPlay64x64, new Vector2(6, -14)))
                {
                    //GoogleAnalytics.Instance.LogEvent("ui-mainmenu-click", "Play", true);

                    if (PlayerDataManager.IsPlayerLoggedIn)
                        GameServerController.Instance.JoinFastestServer();
                    else
                        MenuPageManager.Instance.LoadPage(PageType.Training);
                }

                // Draw Guns N Stuff button
                if (MainMenuButton(new Vector2(0, topOffset + buttonSpacing), new GUIContent(LocalizedStrings.GunsNStuffCaps, LocalizedStrings.MainMenuShopTooltip), UberstrikeIcons.MainMenuShop64x64, new Vector2(6, -5)))
                {
                    //GoogleAnalytics.Instance.LogEvent("ui-mainmenu-Shop", "Play", true);

                    //Open the shop page, and show the shop list of items
                    MenuPageManager.Instance.LoadPage(PageType.Shop);
                }

                // Draw Training button
                if (MainMenuButton(new Vector2(0, topOffset + (buttonSpacing * 2)), new GUIContent(LocalizedStrings.TrainingCaps, LocalizedStrings.MainMenuTrainTooltip), UberstrikeIcons.MainMenuTrain64x64, new Vector2(6, -5)))
                {
                    //GoogleAnalytics.Instance.LogEvent("ui-mainmenu-click", "Training", true);

                    MenuPageManager.Instance.LoadPage(PageType.Training);
                }

                // Draw Quit button
                if (buttonCount == 4)
                {
                    if (MainMenuButton(new Vector2(0, topOffset + (buttonSpacing * 3)), new GUIContent(LocalizedStrings.QuitCaps, LocalizedStrings.MainMenuQuitTooltip), UberstrikeIcons.MainMenuQuit64x64, new Vector2(6, -4)))
                    {
                        PopupSystem.ShowMessage(LocalizedStrings.QuitCaps, LocalizedStrings.AreYouSureQuitMsg, PopupSystem.AlertType.OKCancel, Application.Quit);
                    }
                }

                GUI.color = Color.white;
            }
            GUI.EndGroup();
            }
            GUI.enabled = true;
            GuiManager.DrawTooltip();
        }

        _itemTooltip.OnGui();

        MobileMenuScale.End(scaleMatrix);
    }

    private void DrawWeeklySpecial()
    {
        var promo = ItemPromotionManager.Instance.WeeklySpecial;
        if (promo == null)
            return;

        // Mobile (and Editor preview): hold the panel hidden until its prewarmed promo image has
        // finished downloading, so it appears complete in one go instead of flashing an empty box +
        // spinner next to the avatar. The image URL only arrives ~0.02s before the lobby (its data is
        // the last step of login), too late for prewarm to finish in time — so we wait for it here.
        // Desktop keeps its original immediate spinner-box behaviour.
        if (MobileMenuScale.Active && !promo.Texture.IsLoaded)
            return;

        float textureHeight = PromoTextureAspect * PromotionWidth;
        float height = 28 + textureHeight + 58;
        // Desktop: right-anchored. Mobile (and Editor "Preview Menu Scale"): moved into the open area to the
        // RIGHT of the lobby avatar (left edge ~54% across) so it no longer covers the avatar, but isn't
        // jammed in the far corner. Gated on Active so it previews in the Editor.
        float x = MobileMenuScale.Active
            ? MobileMenuScale.VirtualWidth * 0.60f
            : MobileMenuScale.VirtualWidth - PromotionWidth;
        Rect rect = new Rect(x, GlobalUIRibbon.Instance.GetHeight() + (MobileMenuScale.VirtualHeight - GlobalUIRibbon.Instance.GetHeight() - height) * 0.5f, PromotionWidth, height);

        GUI.BeginGroup(rect, GUIContent.none, BlueStonez.window_standard_grey38);
        {
            GUI.Label(new Rect(0, 0, rect.width, 28), ItemPromotionManager.Instance.WeeklySpecial.Title, BlueStonez.tab_strip_small);

            //promotion image
            ItemPromotionManager.Instance.WeeklySpecial.Texture.Draw(new Rect(0, 28, rect.width, textureHeight));

            //item
            Rect r = new Rect(0, textureHeight + 28, rect.width, 58);
            ItemPromotionManager.Instance.WeeklySpecial.ItemGui.Draw(r, true);

            Rect tooltipRect = new Rect(r.x, r.y, r.width - 100, r.height);

            if (tooltipRect.Contains(Event.current.mousePosition))
            {
                _itemTooltip.SetItem(ItemPromotionManager.Instance.WeeklySpecial.ItemGui.Item, r, PopupViewSide.Left);
            }
        }
        GUI.EndGroup();
    }

    private bool MainMenuButton(Vector2 position, GUIContent content, Texture2D icon, Vector2 iconPosition)
    {
        bool b = GUITools.Button(new Rect(position.x, position.y, 310, 59), content, BlueStonez.button_mainmenu, SoundEffectType.UIRibbonClick);
        GUI.DrawTexture(new Rect(position.x + iconPosition.x, position.y + iconPosition.y, 64, 64), icon);
        return b;
    }

    // ── Classic 4.3.10.1-style lobby (opt-in: Options ▸ General ▸ "Classic lobby HUD") ───────────────
    // First-pass layout matching the reference screenshot: Play + Shop big (side by side), then a
    // Profile / Inbox / Clans / Options ring + Chat. Uses 4.3.8's existing Play/Shop icons; the round
    // ring icons + glowing frame are placeholders (text buttons) until the 4.3.10.1 icon set is
    // extracted (Phase 2). Weekly Special is drawn by the shared DrawWeeklySpecial above. Spacing/colors
    // and the Lvl bar are Phase 3 fidelity work — this is the structural skeleton to preview + iterate.
    private void DrawClassicMainMenu()
    {
        float vw = MobileMenuScale.VirtualWidth;
        float top = GlobalUIRibbon.HEIGHT + 40;

        const float bigW = 250f, bigH = 90f, gap = 24f;
        float blockW = bigW * 2f + gap;
        // Bias the cluster left of the lobby avatar (which sits on the right), clamped on-screen.
        float left = mainMenuX + Mathf.Max(40f, vw * 0.5f - blockW * 0.5f - 120f);

        // Two big buttons: Play (left), Shop (right).
        if (ClassicBigButton(new Rect(left, top, bigW, bigH), LocalizedStrings.PlayCaps, UberstrikeIcons.MainMenuPlay64x64))
        {
            if (PlayerDataManager.IsPlayerLoggedIn)
                GameServerController.Instance.JoinFastestServer();
            else
                MenuPageManager.Instance.LoadPage(PageType.Training);
        }
        if (ClassicBigButton(new Rect(left + bigW + gap, top, bigW, bigH), LocalizedStrings.GunsNStuffCaps, UberstrikeIcons.MainMenuShop64x64))
            MenuPageManager.Instance.LoadPage(PageType.Shop);

        // Ring: Profile / Inbox under Play; Clans / Options under Shop; Chat centred below the left pair.
        float ringY = top + bigH + 18f;
        const float rW = 118f, rH = 40f, rGap = 12f;
        if (ClassicRingButton(new Rect(left, ringY, rW, rH), "Profile"))
            MenuPageManager.Instance.LoadPage(PageType.Home); // TODO(Phase 1): real Profile destination
        if (ClassicRingButton(new Rect(left + rW + rGap, ringY, rW, rH), "Inbox"))
            MenuPageManager.Instance.LoadPage(PageType.Inbox);
        if (ClassicRingButton(new Rect(left + bigW + gap, ringY, rW, rH), "Clans"))
            MenuPageManager.Instance.LoadPage(PageType.Clans);
        if (ClassicRingButton(new Rect(left + bigW + gap + rW + rGap, ringY, rW, rH), "Options"))
            PanelManager.Instance.OpenPanel(PanelType.Options);
        if (ClassicRingButton(new Rect(left + (bigW - rW) * 0.5f, ringY + rH + rGap, rW, rH), "Chat"))
            MenuPageManager.Instance.LoadPage(PageType.Chat);
    }

    private bool ClassicBigButton(Rect rect, string label, Texture2D icon)
    {
        bool b = GUITools.Button(rect, new GUIContent(label), BlueStonez.button_mainmenu, SoundEffectType.UIRibbonClick);
        if (icon != null)
            GUI.DrawTexture(new Rect(rect.x + 12f, rect.y + (rect.height - 64f) * 0.5f, 64f, 64f), icon);
        return b;
    }

    private bool ClassicRingButton(Rect rect, string label)
    {
        // Placeholder round-button until the 4.3.10.1 circular icon set is imported (Phase 2).
        return GUITools.Button(rect, new GUIContent(label), BlueStonez.button, SoundEffectType.UIRibbonClick);
    }
}

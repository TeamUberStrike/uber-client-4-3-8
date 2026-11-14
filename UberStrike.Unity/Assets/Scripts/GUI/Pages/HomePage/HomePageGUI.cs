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

    // Mobile variant of the main menu button art: the original art's left end is cut square to sit
    // against the screen edge; when the menu is inset from the edge (mobile safe area) the buttons use
    // art extended on the left by an angled tip (top-left corner cut, mirroring the right end).
    // The original 310px body is unchanged; the tip adds MobileButtonExtension px on the left.
    // (Artwork/GUI/Resources/mainmenubutton_*_mobile.png, 338x59)
    private const string MobileButtonNormalResource = "mainmenubutton_normal_mobile";
    private const string MobileButtonHoverResource = "mainmenubutton_over_mobile";
    private const float MainMenuButtonWidth = 310f;
    private const float MobileButtonExtension = 28f;
    // Icon + text move right by half the tip: balanced between the tip and the right end
    // (a full-tip shift left too much empty button before the icon, no shift crowded the tip).
    private const float MobileButtonContentShift = MobileButtonExtension * 0.5f;

    private GUIStyle _mobileMainMenuButton;
    private GUIStyle _mobileMainMenuButtonSource;

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

            int buttonCount = (Application.platform != RuntimePlatform.WebGLPlayer) ? 4 : 3;
            int buttonSpacing = 59 + 8;
            int topOffset = 14;
            int height = topOffset + (buttonSpacing * buttonCount);

            // Anchor against the virtual (scaled) screen height so the menu stays vertically centred
            // below the (equally scaled) ribbon under the mobile menu scale.
            int top = Mathf.RoundToInt((((MobileMenuScale.VirtualHeight - GlobalUIRibbon.HEIGHT) * 0.5f) + GlobalUIRibbon.HEIGHT) - (height * 0.5f));

            GUI.BeginGroup(new Rect(mainMenuX, top, MainMenuButtonWidth + MainMenuButtonExtension, height));
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
#if !UNITY_ANDROID && !UNITY_IPHONE
                    if (MainMenuButton(new Vector2(0, topOffset + (buttonSpacing * 3)), new GUIContent(LocalizedStrings.QuitCaps, LocalizedStrings.MainMenuQuitTooltip), UberstrikeIcons.MainMenuQuit64x64, new Vector2(6, -4)))
                    {
                        PopupSystem.ShowMessage(LocalizedStrings.QuitCaps, LocalizedStrings.AreYouSureQuitMsg, PopupSystem.AlertType.OKCancel, Application.Quit);
                    }
#else
                    if (MainMenuButton(new Vector2(0, topOffset + (buttonSpacing * 3)), new GUIContent(LocalizedStrings.LogoutCaps), UberstrikeIcons.MainMenuQuit64x64, new Vector2(6, -4)))
                    {
                        PopupSystem.ShowMessage(LocalizedStrings.LogoutCaps, LocalizedStrings.AreYouSureLogoutMsg, PopupSystem.AlertType.OKCancel, ApplicationDataManager.Instance.Logout);
                    }
#endif
                }

                GUI.color = Color.white;
            }
            GUI.EndGroup();
            GUI.enabled = true;
            GuiManager.DrawTooltip();
        }

        _itemTooltip.OnGui();

        MobileMenuScale.End(scaleMatrix);
    }

    private void DrawWeeklySpecial()
    {
        float textureHeight = PromoTextureAspect * PromotionWidth;
        float height = 28 + textureHeight + 58;
        // Right/centre-anchored against the virtual (scaled) menu area so it stays placed under the scale.
        Rect rect = new Rect(MobileMenuScale.VirtualWidth - PromotionWidth, GlobalUIRibbon.Instance.GetHeight() + (MobileMenuScale.VirtualHeight - GlobalUIRibbon.Instance.GetHeight() - height) * 0.5f, PromotionWidth, height);

        // Stays inside the safe menu area; the scene shows in the strip beside it (like beside the main
        // menu buttons on the left), so no background is extended to the screen edge here.
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
        // PC (no inset): original edge-attached art. Mobile (inset from the edge): art with the angled
        // tip added on the left; text (via style padding) and icon shift right by MobileButtonContentShift.
        float extension = MainMenuButtonExtension;
        float contentShift = extension > 0f ? MobileButtonContentShift : 0f;
        GUIStyle style = extension > 0f ? MobileMainMenuButton : BlueStonez.button_mainmenu;

        bool b = GUITools.Button(new Rect(position.x, position.y, MainMenuButtonWidth + extension, 59), content, style, SoundEffectType.UIRibbonClick);
        GUI.DrawTexture(new Rect(position.x + contentShift + iconPosition.x, position.y + iconPosition.y, 64, 64), icon);
        return b;
    }

    /// <summary>Width of the mobile angled tip, or 0 when the original art is used.</summary>
    private float MainMenuButtonExtension
    {
        get
        {
            return (MobileMenuScale.BleedLeft > 0f && !ReferenceEquals(MobileMainMenuButton, BlueStonez.button_mainmenu))
                ? MobileButtonExtension : 0f;
        }
    }

    /// <summary>
    /// button_mainmenu with the mobile (left-tip extended) backgrounds and padding. Falls back to button_mainmenu if the
    /// textures are missing. Rebuilt if BlueStonez re-creates its styles (e.g. localisation change).
    /// </summary>
    private GUIStyle MobileMainMenuButton
    {
        get
        {
            GUIStyle source = BlueStonez.button_mainmenu;
            if (_mobileMainMenuButton == null || !ReferenceEquals(_mobileMainMenuButtonSource, source))
            {
                _mobileMainMenuButtonSource = source;

                Texture2D normal = Resources.Load(MobileButtonNormalResource, typeof(Texture2D)) as Texture2D;
                Texture2D hover = Resources.Load(MobileButtonHoverResource, typeof(Texture2D)) as Texture2D;

                if (normal != null && hover != null)
                {
                    _mobileMainMenuButton = new GUIStyle(source);
                    _mobileMainMenuButton.normal.background = normal;
                    _mobileMainMenuButton.hover.background = hover;
                    _mobileMainMenuButton.padding.left = source.padding.left + (int)MobileButtonContentShift;
                }
                else
                {
                    Debug.LogWarning("HomePageGUI: mobile main menu button textures missing, using default art.");
                    _mobileMainMenuButton = source;
                }
            }
            return _mobileMainMenuButton;
        }
    }
}

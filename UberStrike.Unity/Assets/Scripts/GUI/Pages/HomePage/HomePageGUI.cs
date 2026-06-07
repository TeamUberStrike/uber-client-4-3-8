using System;
using System.Collections;
using System.Collections.Generic;
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
        if (_hasCheckedPerformance == false)
        {
            PerformanceTest.Instance.enabled = true;
            _hasCheckedPerformance = true;
        }

        BackgroundMusicPlayer.Instance.Play();
        AvatarBuilder.Instance.UpdateLocalAvatar();

        // ALWAYS drive the column menu's slide-in (mainMenuX -250→0). It's harmless for the classic
        // ring (which ignores mainMenuX) and is required so the column lobby renders correctly if the
        // Classic toggle is switched OFF at runtime — otherwise mainMenuX stays at -250 and the column
        // menu draws off the left edge.
        StartCoroutine(AnimateMainMenu(0.25f));

        // Classic ring: build the tiles and show them immediately (no startup fade).
        if (ApplicationDataManager.ApplicationOptions.UseClassicLobby)
        {
            ClassicInit();
            _classicReady = true;
        }
    }

    private void OnDisable()
    {
        // Reset the classic ring so re-entering the lobby re-runs the fade-in (authentic behaviour).
        if (_classicInited)
            ClassicResetMenu();
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

            if (ApplicationDataManager.ApplicationOptions.UseClassicLobby)
            {
                // Authentic 4.3.10.1 ring draws its own Weekly Special inside the AdLarge tile.
                DrawClassicRing();
            }
            else
            {
            DrawWeeklySpecial();

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

    // ══ Classic 4.3.10.1 authentic ring lobby (opt-in: Options ▸ General ▸ "Classic lobby HUD") ══════
    // Full port of the authentic 4.3.10.1 HomePageGUI "MenuTile ring" (decompiled from the original
    // Assembly-CSharp.dll, restored in the 4.3.10.1 reconstruction). Layout, the timed fade-in, the
    // Play → JoinGame/NewGame/ExploreMaps/Back sub-menu toggle, and the in-ring Weekly Special all match
    // retail. The *Tile PNGs (Resources/ClassicLobby/) are COMPLETE button graphics — icon + label + glow
    // baked in — so a tile is drawn as its texture with an invisible hit-rect on top (no skin "MenuTile"
    // style dependency). Adapted to the 4.3.8 fork API vs the 2012 decomp:
    //   • DynamicTexture.Draw(rect) / IsLoaded (no Draw(rect,bool)/IsDone);
    //   • NewGame → PageType.Play (fork has no GameServerController.CreateOnFastestServer);
    //   • Profile → PageType.Stats;
    //   • chat-unread pulse dropped (no ChatManager unread flag); inbox pulse kept;
    //   • the small dead-CDN ad slot dropped; the lobby XP widget dropped (the top ribbon shows Lvl/XP).
    // Runs inside the OnGUI MobileMenuScale matrix and lays out against VirtualWidth/Height, so it scales
    // on phones exactly like the rest of the mobile menu. Desktop (Active==false) uses raw screen dims.

    private class MenuTile
    {
        public Rect Rect;
        public Color Color;
        private readonly float _relativeWidth;
        private readonly float _relativeHeight;

        public MenuTile(float relativeWidth, float relativeHeight)
        {
            _relativeWidth = relativeWidth;
            _relativeHeight = relativeHeight;
            Color = Color.white;
        }

        public void SetRect(float x, float y, Rect canvasRect)
        {
            Rect = new Rect(Mathf.RoundToInt(x), Mathf.RoundToInt(y),
                Mathf.RoundToInt(canvasRect.width * _relativeWidth), Mathf.RoundToInt(canvasRect.height * _relativeHeight));
        }
    }

    private const float ClassicAspect = 1.4f;
    private const int ClassicMinWidth = 480;
    private const int ClassicMaxWidth = 840; // capped so the left-aligned ring (incl. Chat/AdSmall) stays left of the centre avatar
    private const float ClassicAnimSpeed = 13f;

    private Dictionary<string, MenuTile> _tiles;
    private Dictionary<string, MenuTile> _playTiles;
    private bool _classicInited;
    private bool _classicReady;
    private bool _classicPlayActive;
    private bool _classicTransitioning;
    private Rect _classicScreenRect, _classicCanvasRect, _classicMenuRect;

    private static readonly Dictionary<string, Texture2D> _classicTiles = new Dictionary<string, Texture2D>();

    private static Texture2D ClassicTile(string name)
    {
        Texture2D t;
        if (!_classicTiles.TryGetValue(name, out t))
        {
            t = Resources.Load<Texture2D>("ClassicLobby/" + name);
            _classicTiles[name] = t;
        }
        return t;
    }

    private void ClassicInit()
    {
        _tiles = new Dictionary<string, MenuTile>
        {
            // Authentic 4.3.10.1 relative tile sizes (match retail), with a little inter-row breathing
            // room added in ClassicLayoutTiles. AdLarge = big Weekly Special (bottom-left); AdSmall = the
            // small Weekly Special thumbnail to its right (retail's big+small ad pair).
            { "Play", new MenuTile(0.5f, 0.23f) },
            { "Shop", new MenuTile(0.5f, 0.23f) },
            { "MenuOne", new MenuTile(0.25f, 0.14f) },
            { "MenuTwo", new MenuTile(0.25f, 0.14f) },
            { "MenuThree", new MenuTile(0.25f, 0.14f) },
            { "MenuFour", new MenuTile(0.25f, 0.14f) },
            { "AdLarge", new MenuTile(0.7f, 0.56f) },
            { "Chat", new MenuTile(0.3f, 0.28f) },
            { "AdSmall", new MenuTile(0.3f, 0.24f) },
        };
        _playTiles = new Dictionary<string, MenuTile>
        {
            { "JoinGame", new MenuTile(0.3f, 0.2f) },
            { "NewGame", new MenuTile(0.3f, 0.2f) },
            { "ExploreMaps", new MenuTile(0.3f, 0.2f) },
            { "Back", new MenuTile(0.3f, 0.2f) },
        };
        _classicInited = true;
        _classicReady = false;
        _classicPlayActive = false;
        _classicTransitioning = false;
    }

    private void ClassicResetMenu()
    {
        _classicReady = true;
        _classicTransitioning = false;
        _classicPlayActive = false;
        if (_tiles != null) foreach (var kv in _tiles) kv.Value.Color = Color.white;
        if (_playTiles != null) foreach (var kv in _playTiles) kv.Value.Color = Color.white;
    }

    private void ClassicLayout()
    {
        float ribbon = GlobalUIRibbon.Instance.GetHeight();
        float sw = MobileMenuScale.VirtualWidth;
        float sh = MobileMenuScale.VirtualHeight;
        _classicScreenRect = new Rect(0f, ribbon, sw, sh - ribbon);
        // Left ~55% of the screen (was 0.65) so the ring sits beside the centre lobby avatar, not over it.
        _classicCanvasRect = new Rect(20f, 10f, Mathf.RoundToInt(_classicScreenRect.width * 0.55f) - 40, Mathf.RoundToInt(_classicScreenRect.height) - 20);

        Vector2 v = new Vector2(Mathf.RoundToInt(_classicCanvasRect.height * ClassicAspect), Mathf.RoundToInt(_classicCanvasRect.width * (1f / ClassicAspect)));
        if (_classicCanvasRect.height < _classicCanvasRect.width)
        {
            if (v.y < _classicCanvasRect.height) { v.x = _classicCanvasRect.width; v.y = _classicCanvasRect.width * (1f / ClassicAspect); }
            else { v.y = _classicCanvasRect.height; v.x = v.y * ClassicAspect; }
        }
        else if (v.x < _classicCanvasRect.width) { v.y = _classicCanvasRect.height; v.x = _classicCanvasRect.height * ClassicAspect; }
        else { v.x = _classicCanvasRect.width; v.y = _classicCanvasRect.width * (1f / ClassicAspect); }

        v.x = Mathf.Clamp(v.x, ClassicMinWidth, ClassicMaxWidth);
        v.y = Mathf.RoundToInt(v.x * (1f / ClassicAspect));
        int nx = 0; // left-align the ring in the left-side canvas (was centred) so the centre avatar stays clear
        int ny = Mathf.RoundToInt(_classicCanvasRect.height * 0.5f - v.y * 0.5f);
        _classicMenuRect = new Rect(nx, ny, v.x, v.y);
    }

    private void ClassicLayoutTiles()
    {
        // Vertical breathing room between the Play/Shop row, the icon row, and the ad row.
        float gap = _classicMenuRect.height * 0.035f;
        _tiles["Play"].SetRect(0f, 0f, _classicMenuRect);
        _tiles["Shop"].SetRect(_tiles["Play"].Rect.xMax, 0f, _classicMenuRect);
        float iconRowY = _tiles["Play"].Rect.yMax + gap;
        _tiles["MenuOne"].SetRect(0f, iconRowY, _classicMenuRect);
        _tiles["MenuTwo"].SetRect(_tiles["MenuOne"].Rect.xMax, iconRowY, _classicMenuRect);
        _tiles["MenuThree"].SetRect(_tiles["MenuTwo"].Rect.xMax, iconRowY, _classicMenuRect);
        _tiles["MenuFour"].SetRect(_tiles["MenuThree"].Rect.xMax, iconRowY, _classicMenuRect);
        float adRowY = _tiles["MenuOne"].Rect.yMax + gap;
        _tiles["AdLarge"].SetRect(0f, adRowY, _classicMenuRect);
        _tiles["Chat"].SetRect(_tiles["AdLarge"].Rect.xMax, adRowY, _classicMenuRect);
        _tiles["AdSmall"].SetRect(_tiles["AdLarge"].Rect.xMax, _tiles["Chat"].Rect.yMax + gap * 0.6f, _classicMenuRect);
        _playTiles["JoinGame"].SetRect(_tiles["Play"].Rect.xMax, 0f, _classicMenuRect);
        _playTiles["NewGame"].SetRect(_tiles["Play"].Rect.xMax, _playTiles["JoinGame"].Rect.yMax, _classicMenuRect);
        _playTiles["ExploreMaps"].SetRect(_tiles["Play"].Rect.xMax, _playTiles["NewGame"].Rect.yMax, _classicMenuRect);
        _playTiles["Back"].SetRect(_tiles["Play"].Rect.xMax, _playTiles["ExploreMaps"].Rect.yMax, _classicMenuRect);
    }

    private void ClassicTogglePlay()
    {
        if (_classicPlayActive && !_classicTransitioning) StartCoroutine(ClassicDeactivatePlay());
        else StartCoroutine(ClassicActivatePlay());
    }

    private IEnumerator ClassicActivatePlay()
    {
        _classicPlayActive = true;
        _classicTransitioning = true;
        foreach (var kv in _playTiles) kv.Value.Color = Color.white.SetAlpha(0f);
        foreach (var kv in _tiles)
        {
            if (kv.Key == "Play") continue;
            while (kv.Value.Color.a > 0f)
            {
                kv.Value.Color.a -= Time.deltaTime * ClassicAnimSpeed;
                yield return new WaitForEndOfFrame();
            }
            kv.Value.Color = Color.white.SetAlpha(0f);
        }
        foreach (var kv in _playTiles)
        {
            if (kv.Key == "Play") continue;
            while (kv.Value.Color.a < 1f)
            {
                kv.Value.Color.a += Time.deltaTime * ClassicAnimSpeed;
                yield return new WaitForEndOfFrame();
            }
            kv.Value.Color = Color.white;
        }
        _classicTransitioning = false;
    }

    private IEnumerator ClassicDeactivatePlay()
    {
        _classicTransitioning = true;
        foreach (var kv in _playTiles) kv.Value.Color = Color.white.SetAlpha(0f);
        foreach (var kv in _tiles)
        {
            while (kv.Value.Color.a < 1f)
            {
                kv.Value.Color.a += Time.deltaTime * ClassicAnimSpeed;
                yield return new WaitForEndOfFrame();
            }
            kv.Value.Color = Color.white;
        }
        _classicTransitioning = false;
        _classicPlayActive = false;
    }

    private void DrawClassicRing()
    {
        // Lazy init for the runtime case (toggle switched ON after OnEnable already ran): build the
        // tiles and show them immediately (no fade coroutine is running in that path).
        if (!_classicInited) { ClassicInit(); _classicReady = true; }
        ClassicLayout();
        ClassicLayoutTiles();

        // Inbox "you have unread" glow pulse (authentic).
        float pulse = (Mathf.Sin(Time.time * 6f) + 1.2f) * 0.5f;

        GUI.BeginGroup(_classicScreenRect);
        GUI.BeginGroup(_classicCanvasRect);
        GUI.BeginGroup(_classicMenuRect);

        ClassicDrawTile(_tiles["Play"], "PlayTile", ClassicTogglePlay);
        ClassicDrawTile(_tiles["Shop"], "ShopTile", delegate { MenuPageManager.Instance.LoadPage(PageType.Shop); });
        ClassicDrawTile(_tiles["MenuOne"], "ProfileTile", delegate { MenuPageManager.Instance.LoadPage(PageType.Stats); });
        bool inboxUnread = InboxManager.Instance.HasUnreadMessages || InboxManager.Instance.HasUnreadRequests;
        ClassicDrawTile(_tiles["MenuTwo"], "InboxTile", delegate { MenuPageManager.Instance.LoadPage(PageType.Inbox); }, inboxUnread ? pulse : 1f);
        ClassicDrawTile(_tiles["MenuThree"], "ClansTile", delegate { MenuPageManager.Instance.LoadPage(PageType.Clans); });
        ClassicDrawTile(_tiles["MenuFour"], "OptionsTile", delegate { PanelManager.Instance.OpenPanel(PanelType.Options); });
        ClassicDrawTile(_tiles["Chat"], "ChatTile", delegate { MenuPageManager.Instance.LoadPage(PageType.Chat); });

        DrawClassicWeeklySpecial(_tiles["AdLarge"], withTitle: true);
        DrawClassicWeeklySpecial(_tiles["AdSmall"], withTitle: false);

        if (_classicPlayActive)
        {
            ClassicDrawTile(_playTiles["JoinGame"], "JoinGameTile", delegate { GameServerController.Instance.JoinFastestServer(); });
            ClassicDrawTile(_playTiles["NewGame"], "NewGameTile", delegate { MenuPageManager.Instance.LoadPage(PageType.Play); });
            ClassicDrawTile(_playTiles["ExploreMaps"], "ExploreMapsTile", delegate { MenuPageManager.Instance.LoadPage(PageType.Training); });
            ClassicDrawTile(_playTiles["Back"], "BackTile", ClassicTogglePlay);
        }

        GUI.EndGroup();
        GUI.EndGroup();
        GUI.EndGroup();
    }

    private void DrawClassicWeeklySpecial(MenuTile tile, bool withTitle)
    {
        var promo = ItemPromotionManager.Instance.WeeklySpecial;
        if (!_classicReady || promo == null || tile.Color.a <= 0.001f)
            return;

        Color prev = GUI.color;
        GUI.color = tile.Color;
        // Framed panel like the column lobby's Weekly Special, then the promo image inset inside it.
        GUI.Box(tile.Rect, GUIContent.none, BlueStonez.window_standard_grey38);
        Rect inner = new Rect(tile.Rect.x + 5f, tile.Rect.y + 5f, tile.Rect.width - 10f, tile.Rect.height - 10f);
        promo.Texture.Draw(inner);

        // Only the large ad carries the title gradient overlay; the small thumbnail is image-only.
        if (withTitle && promo.Texture.IsLoaded)
        {
            float gh = Mathf.RoundToInt(inner.height * 0.25f);
            Rect grad = new Rect(inner.x, inner.yMax - gh, inner.width, gh);
            Texture2D gradTex = ClassicTile("ContentGradient");
            if (gradTex != null) GUI.DrawTexture(grad, gradTex);
            GUITools.LabelShadow(grad.OffsetBy(0f, inner.height * 0.05f), promo.Title, BlueStonez.label_interparkbold_18pt, tile.Color);
        }
        GUI.color = prev;

        if (!_classicTransitioning && GUITools.Button(tile.Rect, GUIContent.none, GUIStyle.none))
            MenuPageManager.Instance.LoadPage(PageType.Shop);
    }

    // A classic tile is a baked icon/label/glow graphic on a dark semi-transparent backing (the retail
    // MenuTile background). Retail's style isn't in the fork's skin, so reconstruct it: draw the imported
    // flat-grey TileNormal/TileHover tinted DARK as the backing (so the glow reads over the busy 3D scene),
    // then the tile graphic on top. Honours the tile's fade alpha, an optional content-alpha pulse for
    // unread badges, a subtle non-hover dim, and an invisible click hit-rect.
    private void ClassicDrawTile(MenuTile tile, string tileName, Action action, float contentAlpha = 1f)
    {
        if (tile.Color.a <= 0.001f) return;

        bool hover = tile.Rect.Contains(Event.current.mousePosition);
        Color prev = GUI.color;

        Texture2D backing = ClassicTile(hover ? "TileHover" : "TileNormal");
        if (backing != null)
        {
            GUI.color = new Color(0f, 0f, 0f, tile.Color.a * (hover ? 0.55f : 0.4f));
            GUI.DrawTexture(tile.Rect, backing, ScaleMode.StretchToFill);
        }

        Texture2D tex = ClassicTile(tileName);
        if (tex != null)
        {
            GUI.color = new Color(1f, 1f, 1f, tile.Color.a * contentAlpha * (hover ? 1f : 0.92f));
            GUI.DrawTexture(tile.Rect, tex, ScaleMode.ScaleToFit);
        }

        GUI.color = prev;

        if (action != null && !_classicTransitioning && GUITools.Button(tile.Rect, GUIContent.none, GUIStyle.none))
            action();
    }
}

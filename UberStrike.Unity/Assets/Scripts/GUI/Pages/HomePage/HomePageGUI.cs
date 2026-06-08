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
        public float Hover;   // 0..1 hover amount, smoothly faded (NGUI-style), drives the hover highlight
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
            // Authentic 4.3.10.1 relative tile sizes — VERBATIM from the original decomp so the rows pack
            // flush (Play 0.24 + icons 0.15 + AdLarge 0.61 == 1.0), exactly like retail. No inter-row gaps:
            // retail has none, and the added 0.035 bands were the "big gaps" between rows. AdLarge = big
            // Weekly Special (bottom-left); AdSmall = the small thumbnail to its right (retail's ad pair).
            { "Play", new MenuTile(0.5f, 0.24f) },
            { "Shop", new MenuTile(0.5f, 0.24f) },
            { "MenuOne", new MenuTile(0.25f, 0.15f) },
            { "MenuTwo", new MenuTile(0.25f, 0.15f) },
            { "MenuThree", new MenuTile(0.25f, 0.15f) },
            { "MenuFour", new MenuTile(0.25f, 0.15f) },
            { "AdLarge", new MenuTile(0.7f, 0.61f) },
            // Chat is a bigger tile (wider + taller than the icon row, like retail) centred vertically in
            // the right column between the icon row and the small ad (positioned in ClassicLayoutTiles).
            { "Chat", new MenuTile(0.3f, 0.2f) },
            { "AdSmall", new MenuTile(0.3f, 0.28f) },
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
        if (_tiles != null) foreach (var kv in _tiles) { kv.Value.Color = Color.white; kv.Value.Hover = 0f; }
        if (_playTiles != null) foreach (var kv in _playTiles) { kv.Value.Color = Color.white; kv.Value.Hover = 0f; }
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
        // Authentic flush packing (no inter-row gaps — see ClassicInit). Each row butts up against the
        // previous row's yMax, matching retail's compact ring.
        _tiles["Play"].SetRect(0f, 0f, _classicMenuRect);
        _tiles["Shop"].SetRect(_tiles["Play"].Rect.xMax, 0f, _classicMenuRect);
        float iconRowY = _tiles["Play"].Rect.yMax;
        _tiles["MenuOne"].SetRect(0f, iconRowY, _classicMenuRect);
        _tiles["MenuTwo"].SetRect(_tiles["MenuOne"].Rect.xMax, iconRowY, _classicMenuRect);
        _tiles["MenuThree"].SetRect(_tiles["MenuTwo"].Rect.xMax, iconRowY, _classicMenuRect);
        _tiles["MenuFour"].SetRect(_tiles["MenuThree"].Rect.xMax, iconRowY, _classicMenuRect);
        float adRowY = _tiles["MenuOne"].Rect.yMax;
        _tiles["AdLarge"].SetRect(0f, adRowY, _classicMenuRect);
        // Pin the small ad to the bottom (aligned with the big ad's bottom).
        float adSmallH = Mathf.RoundToInt(_classicMenuRect.height * 0.28f);
        _tiles["AdSmall"].SetRect(_tiles["AdLarge"].Rect.xMax, _tiles["AdLarge"].Rect.yMax - adSmallH, _classicMenuRect);
        // Chat FILLS the whole right-column gap between the icon row and the small ad — no empty space above
        // or below it. Its backing is drawn filled (not hugged — see the fill flag in DrawClassicRing); the
        // icon/label sits centred inside the tall pill.
        _tiles["Chat"].SetRect(_tiles["AdLarge"].Rect.xMax, adRowY, _classicMenuRect);
        _tiles["Chat"].Rect.height = _tiles["AdSmall"].Rect.yMin - adRowY;
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

        // Nested clip groups (screen -> canvas -> menu). NOTE: do NOT replace these with a GUI.matrix
        // translate — on mobile MobileMenuScale already sets a scale matrix, and multiplying another matrix
        // onto GUI.matrix here produced a "matrix needs to be invertible" error on device that broke touch
        // hit-testing on the ring (tiles needed multiple taps). The minor cosmetic cost is that the menuRect
        // group clips the very edge of the outermost tiles' hover glow — acceptable vs. broken input.
        GUI.BeginGroup(_classicScreenRect);
        GUI.BeginGroup(_classicCanvasRect);
        GUI.BeginGroup(_classicMenuRect);

        // Per-ELEMENT dark backings — each tile gets its own rounded pill hugged to its icon/label, with a
        // small uniform gap separating it from its neighbours, and a cyan hover flash. Drawn first, behind
        // the tile content.
        if (!_classicPlayActive)
        {
            ClassicTileBack(_tiles["Play"], ClassicTile("PlayTile"));
            ClassicTileBack(_tiles["Shop"], ClassicTile("ShopTile"));
            ClassicTileBack(_tiles["MenuOne"], ClassicTile("ProfileTile"));
            ClassicTileBack(_tiles["MenuTwo"], ClassicTile("InboxTile"));
            ClassicTileBack(_tiles["MenuThree"], ClassicTile("ClansTile"));
            ClassicTileBack(_tiles["MenuFour"], ClassicTile("OptionsTile"));
            ClassicTileBack(_tiles["Chat"], ClassicTile("ChatTile"), fill: true);
        }
        else
        {
            ClassicTileBack(_tiles["Play"], ClassicTile("PlayTile"));
            ClassicTileBack(_playTiles["JoinGame"], ClassicTile("JoinGameTile"));
            ClassicTileBack(_playTiles["NewGame"], ClassicTile("NewGameTile"));
            ClassicTileBack(_playTiles["ExploreMaps"], ClassicTile("ExploreMapsTile"));
            ClassicTileBack(_playTiles["Back"], ClassicTile("BackTile"));
        }

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

        if (ClassicTapped(tile.Rect, withTitle ? "WeeklySpecialLarge" : "WeeklySpecialSmall"))
            MenuPageManager.Instance.LoadPage(PageType.Shop);
    }

    // Robust single-tap hit-test for the classic ring tiles. The IMGUI GUI.Button model needs multiple taps
    // here on device: the nested clip-groups + MobileMenuScale's scale matrix desync GUI.Button's internal
    // hot/active control tracking, so the first (sometimes second) tap is dropped. We detect the tap
    // explicitly instead — MouseDown inside a tile arms it, MouseUp inside the SAME tile fires. Both read
    // Event.current.mousePosition, which Unity has already mapped into this group-local space (the same
    // space tile.Rect lives in), so it is correct under the groups + scale. Works with mouse in the Editor
    // preview too. _classicTransitioning gates clicks during the energy-wipe / page transition.
    private string _classicPressedTile;
    private bool ClassicTapped(Rect rect, string tileId)
    {
        if (_classicTransitioning) return false;
        Event e = Event.current;
        switch (e.type)
        {
            case UnityEngine.EventType.MouseDown:
                if (rect.Contains(e.mousePosition)) { _classicPressedTile = tileId; e.Use(); }
                break;
            case UnityEngine.EventType.MouseUp:
                if (_classicPressedTile == tileId)
                {
                    _classicPressedTile = null;
                    if (rect.Contains(e.mousePosition))
                    {
                        SfxManager.Play2dAudioClip(SoundEffectType.UIRibbonClick);
                        e.Use();
                        return true;
                    }
                }
                break;
        }
        return false;
    }

    // A classic tile is a baked icon/label/glow graphic (the *Tile PNG). The dark backing is no longer per
    // tile — it's a per-element pill drawn first by ClassicTileBack (with the hover flash). So this just
    // draws the crisp icon/label on top, honouring the tile's fade alpha, an optional content-alpha pulse
    // for unread badges, a subtle non-hover dim, and an invisible click hit-rect.
    private void ClassicDrawTile(MenuTile tile, string tileName, Action action, float contentAlpha = 1f)
    {
        if (tile.Color.a <= 0.001f) return;

        Color prev = GUI.color;

        Texture2D tex = ClassicTile(tileName);
        if (tex != null)
        {
            // Brighten the icon/label smoothly with the hover amount (0.9 dim → 1.0), matching the backing fade.
            GUI.color = new Color(1f, 1f, 1f, tile.Color.a * contentAlpha * Mathf.Lerp(0.9f, 1f, tile.Hover));
            GUI.DrawTexture(tile.Rect, tex, ScaleMode.ScaleToFit);
        }

        GUI.color = prev;

        if (action != null && ClassicTapped(tile.Rect, tileName))
            action();
    }

    // Per-element rounded dark pill behind one tile: spans the tile's full width MINUS a small gap on each
    // side (so neighbours are separated by a thin gap), hugged vertically to the icon/label height (so the
    // 3D scene shows between rows). On hover it "flashes": the fill warms to a cyan tint and a soft, gently
    // pulsing cyan glow haloes the pill. Drawn behind the tile content.
    private void ClassicTileBack(MenuTile tile, Texture2D tex, bool fill = false)
    {
        if (tile.Color.a <= 0.001f) return;

        Rect cell = tile.Rect;
        // Uniform thin gap on ALL four sides, so the gap between tiles is the SAME small size horizontally
        // (incl. Play↔Shop) and vertically (between rows). Tiles butt to "cell inset by gap"; a tall cell
        // whose content is much shorter is clamped to hug its content so it stays a compact pill instead of
        // a big empty box — UNLESS fill==true (Chat), which keeps the backing filling the whole cell.
        float gap = Mathf.Max(1f, _classicMenuRect.height * 0.004f);
        float vmargin = _classicMenuRect.height * 0.028f;
        float top = cell.yMin + gap, bot = cell.yMax - gap;
        if (!fill && tex != null && tex.width > 0 && tex.height > 0)
        {
            float cellScale = Mathf.Min(cell.width / tex.width, cell.height / tex.height);
            float maxH = tex.height * cellScale + vmargin * 2f;
            if (bot - top > maxH)
            {
                float cy = cell.center.y;
                top = cy - maxH * 0.5f;
                bot = cy + maxH * 0.5f;
            }
        }
        Rect back = Rect.MinMaxRect(cell.xMin + gap, top, cell.xMax - gap, bot);

        // Smoothly fade a per-tile hover amount in/out (like NGUI's UIButtonColor 0.2s tween) instead of an
        // instant pulsing flash — much cleaner. Advance the fade once per frame (on Repaint).
        bool over = cell.Contains(Event.current.mousePosition);
        if (Event.current.type == UnityEngine.EventType.Repaint)
            tile.Hover = Mathf.MoveTowards(tile.Hover, over ? 1f : 0f, Time.deltaTime / 0.12f);
        float h = tile.Hover;

        GUIStyle style = BackingStyle();
        Color prev = GUI.color;

        // Steady soft cyan glow rim behind the panel, alpha-faded by the hover amount (no pulse).
        if (h > 0.001f)
        {
            float grow = _classicMenuRect.height * 0.005f;
            GUI.color = new Color(0.32f, 0.82f, 1f, tile.Color.a * 0.5f * h);
            GUI.Box(new Rect(back.x - grow, back.y - grow, back.width + grow * 2f, back.height + grow * 2f), GUIContent.none, style);
        }
        // Fill: flat black → soft dark-cyan tint as the hover fades in.
        GUI.color = Color.Lerp(new Color(0f, 0f, 0f, tile.Color.a * 0.42f),
                               new Color(0.06f, 0.20f, 0.28f, tile.Color.a * 0.6f), h);
        GUI.Box(back, GUIContent.none, style);

        GUI.color = prev;
    }

    // Procedural rounded-rectangle panel for the classic tile backings — generated once, cached static.
    // White with a 1px anti-aliased rounded edge (signed-distance), tinted at draw time via GUI.color.
    // Used as a 9-sliced GUIStyle.background (border == corner radius) so the rounded corners stay sharp
    // no matter how the tile stretches; the straight edges/centre stretch cleanly.
    private static Texture2D _backingTex;
    private static GUIStyle _backingStyle;

    private static GUIStyle BackingStyle()
    {
        if (_backingStyle != null && _backingTex != null) return _backingStyle;

        const int size = 64;
        const int radius = 5; // rounded pill corners; small so the inter-tile gap stays uniform/tiny at corners
        _backingTex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            name = "ClassicTileBacking"
        };
        Color32[] px = new Color32[size * size];
        float half = size * 0.5f;
        float inner = half - radius;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Abs(x + 0.5f - half) - inner;
                float dy = Mathf.Abs(y + 0.5f - half) - inner;
                float outside = Mathf.Sqrt(Mathf.Max(dx, 0f) * Mathf.Max(dx, 0f) + Mathf.Max(dy, 0f) * Mathf.Max(dy, 0f));
                float sd = outside + Mathf.Min(Mathf.Max(dx, dy), 0f) - radius; // signed distance; <0 inside
                byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(0.5f - sd) * 255f);
                px[y * size + x] = new Color32(255, 255, 255, a);
            }
        }
        _backingTex.SetPixels32(px);
        _backingTex.Apply(false);

        _backingStyle = new GUIStyle { border = new RectOffset(radius, radius, radius, radius) };
        _backingStyle.normal.background = _backingTex;
        return _backingStyle;
    }
}

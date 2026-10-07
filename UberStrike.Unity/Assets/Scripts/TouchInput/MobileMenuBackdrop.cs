using UnityEngine;

/// <summary>
/// Turns the lobby into a clean, static backdrop while the on-screen-control layout editor is open
/// FROM THE MENU (the Options ▸ Controls ▸ "Customize On-Screen Controls" entry, NOT the in-match
/// "Edit Controls" button). While active it:
///   • hides the top navigation ribbon (<see cref="GlobalUIRibbon"/>),
///   • unloads the current menu page so the Play / Training / Clan / Shop UI disappears
///     (<see cref="MenuPageManager.UnloadCurrentPage"/>, which also stops the camera orbit → static),
///   • hides the local lobby avatar's renderers (body + gear + weapons),
/// leaving only the 3D spaceship scene + its now-still camera as a polished settings background.
///
/// Fully reversible: <see cref="Exit"/> re-shows the avatar, restores the ribbon, and reloads the
/// saved page (its normal 1 s camera transition doubles as a smooth return).
///
/// Self-gating: it only engages in the live lobby (a <see cref="MenuPageManager"/> is present and no
/// match is running). In a match — where the in-match editor button keeps the live game dimmed — and
/// in the Editor force-preview (no menu/server) it simply no-ops, so neither path changes.
/// </summary>
public static class MobileMenuBackdrop
{
    public static bool IsActive { get; private set; }

    private static PageType _savedPage = PageType.None;
    private static bool _ribbonWasShown;
    private static Renderer[] _avatarRenderers;
    private static bool[] _avatarRendererStates;

    public static void Enter()
    {
        if (IsActive) return;
        if (GameState.HasCurrentGame) return; // in a match: keep dimming the live game, no lobby backdrop
        if (!MenuPageManager.Exists) return;  // not in the lobby menu (e.g. Editor force-preview)

        IsActive = true;

        // 1) Top navigation ribbon.
        if (GlobalUIRibbon.Exists)
        {
            _ribbonWasShown = GlobalUIRibbon.Instance.enabled;
            GlobalUIRibbon.Instance.Hide();
        }

        // 2) Current page UI (Play / Training / Clan / Shop / Home buttons). Unload also disables the
        //    MouseOrbit, so the spaceship view holds still behind the editor.
        _savedPage = MenuPageManager.GetCurrentPage();
        MenuPageManager.Instance.UnloadCurrentPage();

        // 3) Local lobby avatar. MeshRenderer is only the body, and weapons/gear hang off child
        //    transforms, so hide every renderer under the decorator (recording prior state).
        HideAvatar();
    }

    public static void Exit()
    {
        if (!IsActive) return;
        IsActive = false;

        ShowAvatar();

        if (GlobalUIRibbon.Exists && _ribbonWasShown)
            GlobalUIRibbon.Instance.Show();

        // Reload the page we left (forceReload so it replays even though _currentPageType is now None);
        // this also rebuilds/re-shows the avatar through the normal page lifecycle.
        if (MenuPageManager.Exists && _savedPage != PageType.None)
            MenuPageManager.Instance.LoadPage(_savedPage, true);

        _savedPage = PageType.None;
    }

    private static void HideAvatar()
    {
        AvatarDecorator dec = GameState.LocalDecorator;
        if (dec == null) return;

        _avatarRenderers = dec.GetComponentsInChildren<Renderer>(true);
        _avatarRendererStates = new bool[_avatarRenderers.Length];
        for (int i = 0; i < _avatarRenderers.Length; i++)
        {
            _avatarRendererStates[i] = _avatarRenderers[i].enabled;
            _avatarRenderers[i].enabled = false;
        }
    }

    private static void ShowAvatar()
    {
        if (_avatarRenderers == null) return;
        for (int i = 0; i < _avatarRenderers.Length; i++)
        {
            // The avatar may have been rebuilt by a page reload — skip any renderer Unity destroyed.
            if (_avatarRenderers[i] != null)
                _avatarRenderers[i].enabled = _avatarRendererStates[i];
        }
        _avatarRenderers = null;
        _avatarRendererStates = null;
    }
}

using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor convenience for previewing the on-screen touch controls + customizable layout in the
/// Editor Game view — no mobile channel, no server, no match.
///
/// "Start" sets the EditorPrefs preview flag AND enters Play mode in one click (the flag must be set
/// before the [RuntimeInitializeOnLoadMethod] bootstrap runs, and it survives the play-mode domain
/// reload because it lives in EditorPrefs). The standalone layout editor then opens automatically.
/// Editor-only; no effect on player builds.
/// </summary>
public static class MobileControlsPreviewMenu
{
    private const string Start = "Tools/Mobile/Start Touch Controls Preview (Play)";
    private const string Stop  = "Tools/Mobile/Stop Touch Controls Preview";
    private const string Backdrop = "Tools/Mobile/Preview Lobby Backdrop (in Play, from lobby)";
    private const string Toggle = "Tools/Mobile/Touch Controls Preview Enabled";

    [MenuItem(Start, priority = 0)]
    private static void StartPreview()
    {
        MobileControlsBootstrap.ForcePreviewInEditor = true;
        if (!EditorApplication.isPlaying)
            EditorApplication.isPlaying = true;
        Debug.Log("[MobileControls] Preview ON — entering Play mode. The layout editor opens automatically; drag/scale the controls, then Done.");
    }

    [MenuItem(Stop, priority = 1)]
    private static void StopPreview()
    {
        MobileControlsBootstrap.ForcePreviewInEditor = false;
        if (EditorApplication.isPlaying)
            EditorApplication.isPlaying = false;
        Debug.Log("[MobileControls] Preview OFF.");
    }

    /// <summary>
    /// Engages the lobby backdrop while you are already standing in the live lobby in Play mode
    /// (the real menu, e.g. after logging in). The shipped entry point is the IsMobile-gated
    /// Options ▸ Controls ▸ "Customize On-Screen Controls" button, which does not appear on the
    /// Editor's desktop channel — so this is the Editor-only way to SEE the backdrop without a device.
    /// It opens the customize editor and hides the ribbon + menu page + avatar over the static
    /// spaceship scene. Press "Done" in the editor (or Stop Preview) to restore the menu.
    /// </summary>
    [MenuItem(Backdrop, priority = 2)]
    private static void PreviewLobbyBackdrop()
    {
        if (!EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Preview Lobby Backdrop",
                "Enter Play mode and reach the lobby first (open Assets/Scenes/Latest.unity, Play, log in), " +
                "then run this again while standing in the lobby.", "OK");
            return;
        }

        // Make sure the on-screen-control host/editor exists (it is created on this flag in the Editor).
        MobileControlsBootstrap.ForcePreviewInEditor = true;

        // Open the customize editor and engage the backdrop now, in the current lobby context. Enter()
        // self-gates (needs MenuPageManager + no current game), so this no-ops cleanly if you are not
        // actually in the lobby — and bypasses the login-time auto-open edge.
        MobileControlLayout.EditMode = true;
        MobileMenuBackdrop.Enter();

        if (MobileMenuBackdrop.IsActive)
            Debug.Log("[MobileControls] Lobby backdrop engaged — ribbon + menu page + avatar hidden behind the customize editor. Press Done to restore.");
        else
            Debug.LogWarning("[MobileControls] Backdrop did not engage. Are you in the lobby? It needs the live menu (MenuPageManager) and no active match.");
    }

    // Plain toggle (does not change play state) — for leaving it on across sessions if wanted.
    [MenuItem(Toggle, priority = 20)]
    private static void ToggleFlag()
    {
        MobileControlsBootstrap.ForcePreviewInEditor = !MobileControlsBootstrap.ForcePreviewInEditor;
    }

    [MenuItem(Toggle, true)]
    private static bool ToggleValidate()
    {
        Menu.SetChecked(Toggle, MobileControlsBootstrap.ForcePreviewInEditor);
        return true;
    }
}

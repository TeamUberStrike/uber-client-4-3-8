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

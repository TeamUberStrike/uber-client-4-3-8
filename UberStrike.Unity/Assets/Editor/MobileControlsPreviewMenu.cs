using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor convenience: toggle the on-screen touch controls on in the Game view / Device Simulator
/// without switching to a mobile channel, so the layout can be tuned in-Editor (no device build).
/// Editor-only; has no effect on player builds.
/// </summary>
public static class MobileControlsPreviewMenu
{
    private const string MenuPath = "Tools/Mobile/Toggle Touch Controls Preview";

    [MenuItem(MenuPath)]
    private static void Toggle()
    {
        MobileControlsBootstrap.ForcePreviewInEditor = !MobileControlsBootstrap.ForcePreviewInEditor;
        Debug.Log("[MobileControls] Editor touch-controls preview " +
            (MobileControlsBootstrap.ForcePreviewInEditor ? "ENABLED (enter Play mode in the Device Simulator)" : "disabled"));
    }

    [MenuItem(MenuPath, true)]
    private static bool ToggleValidate()
    {
        Menu.SetChecked(MenuPath, MobileControlsBootstrap.ForcePreviewInEditor);
        return true;
    }
}

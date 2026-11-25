using UnityEditor;
using UnityEngine;

public static class MobileMenuScalePreviewMenu
{
    private const string MenuPath = "Tools/Mobile/Preview Menu Scale";

    [MenuItem(MenuPath, priority = 21)]
    private static void Toggle()
    {
        MobileMenuScale.ForcePreviewInEditor = !MobileMenuScale.ForcePreviewInEditor;
        Debug.Log("[MobileMenuScale] Preview " + (MobileMenuScale.ForcePreviewInEditor ? "ON" : "OFF"));
    }

    [MenuItem(MenuPath, true)]
    private static bool ToggleValidate()
    {
        Menu.SetChecked(MenuPath, MobileMenuScale.ForcePreviewInEditor);
        return true;
    }
}

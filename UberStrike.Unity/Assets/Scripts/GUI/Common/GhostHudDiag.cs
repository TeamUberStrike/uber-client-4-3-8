using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// TEMPORARY diagnostic for the "ghost HUD" (elements float at the wrong position for ~1s on first page
/// open, then snap). Logs the live layout values for the first N Repaint frames of each tagged site so a
/// single device capture reveals which value (Screen size / scale / computed rect) is transient.
/// Remove once the ghost is root-caused. Lines are prefixed "[GhostDiag]".
/// </summary>
public static class GhostHudDiag
{
    public static bool Enabled = true;
    private const int MaxFrames = 150;
    private static readonly Dictionary<string, int> _frames = new Dictionary<string, int>();

    public static void Log(string tag, string detail)
    {
        if (!Enabled || Event.current == null || Event.current.type != EventType.Repaint) return;

        int f;
        _frames.TryGetValue(tag, out f);
        if (f >= MaxFrames) return;
        _frames[tag] = f + 1;

        Debug.Log(string.Format(
            "[GhostDiag {0}] f{1} t={2:N2} Screen={3}x{4} safeR={5:N0} Scale={6:N3} VW={7:N0} VH={8:N0} | {9}",
            tag, f, Time.realtimeSinceStartup, Screen.width, Screen.height,
            Screen.width - Screen.safeArea.xMax, MobileMenuScale.Scale,
            MobileMenuScale.VirtualWidth, MobileMenuScale.VirtualHeight, detail));
    }
}

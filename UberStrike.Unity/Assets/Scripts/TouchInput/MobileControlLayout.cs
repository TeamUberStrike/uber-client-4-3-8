using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

/// <summary>
/// Stores and persists the player's customized on-screen control layout (per-control position +
/// scale), like Pixel Gun 3D / CoD Mobile. Positions are kept normalized to the screen (0..1) so a
/// layout saved on one device renders consistently on another resolution/aspect.
///
/// Persistence is a single PlayerPrefs string ("id:nx,ny,scale;...") so we don't depend on the
/// project's CmunePrefs key enum. Invariant-culture float formatting keeps it locale-safe.
/// </summary>
public static class MobileControlLayout
{
    public class Placement
    {
        public float Nx;        // normalized GUI-space center X (0..1 of screen width)
        public float Ny;        // normalized GUI-space center Y (0..1 of screen height)
        public float Scale = 1f;
        public bool Hidden;     // player removed this control via the editor's red ✕ button
    }

    private const string PrefsKey = "MobileControlLayout";
    private const float MinScale = 0.5f;
    private const float MaxScale = 2.5f;

    // True while the in-game layout editor is open (read by TouchController to suspend live input).
    public static bool EditMode;

    // True when the editor should run a no-match standalone layout preview (Editor force-preview,
    // no live TouchInput / game). Set by MobileControlsBootstrap; read by MobileControlLayoutEditor.
    public static bool PreviewStandalone;

    private static Dictionary<string, Placement> _map;

    private static void EnsureLoaded()
    {
        if (_map != null) return;
        _map = new Dictionary<string, Placement>();
        Load();
    }

    /// <summary>
    /// Returns the saved placement for <paramref name="id"/>, or seeds one from the supplied default
    /// pixel center / scale if none exists yet.
    /// </summary>
    public static Placement GetOrDefault(string id, Vector2 defaultGuiCenter, float defaultScale)
    {
        EnsureLoaded();
        Placement p;
        if (!_map.TryGetValue(id, out p))
        {
            p = new Placement
            {
                Nx = Mathf.Clamp01(defaultGuiCenter.x / Mathf.Max(1f, Screen.width)),
                Ny = Mathf.Clamp01(defaultGuiCenter.y / Mathf.Max(1f, Screen.height)),
                Scale = Mathf.Clamp(defaultScale, MinScale, MaxScale),
            };
            _map[id] = p;
        }
        return p;
    }

    public static Vector2 ToPixels(Placement p)
    {
        return new Vector2(p.Nx * Screen.width, p.Ny * Screen.height);
    }

    public static void SetPixels(string id, Vector2 guiCenter, float scale)
    {
        EnsureLoaded();
        Placement p;
        if (!_map.TryGetValue(id, out p)) { p = new Placement(); _map[id] = p; }
        p.Nx = Mathf.Clamp01(guiCenter.x / Mathf.Max(1f, Screen.width));
        p.Ny = Mathf.Clamp01(guiCenter.y / Mathf.Max(1f, Screen.height));
        p.Scale = Mathf.Clamp(scale, MinScale, MaxScale);
    }

    public static void SetScale(string id, float scale)
    {
        EnsureLoaded();
        Placement p;
        if (_map.TryGetValue(id, out p))
            p.Scale = Mathf.Clamp(scale, MinScale, MaxScale);
    }

    // True if the player removed this control via the editor's ✕ button. Removed controls are skipped in
    // the live game (TouchController) but still shown — greyed — in the editor so they can be restored.
    public static bool IsHidden(string id)
    {
        EnsureLoaded();
        Placement p;
        return _map.TryGetValue(id, out p) && p.Hidden;
    }

    // Marks a control removed / restored. The caller (editor) seeds the placement at the control's current
    // spot first, so a later restore returns it exactly where it was; the fallback below only guards the
    // (practically unreachable) case of toggling an id that was never positioned.
    public static void SetHidden(string id, bool hidden)
    {
        EnsureLoaded();
        Placement p;
        if (!_map.TryGetValue(id, out p))
        {
            p = new Placement { Nx = 0.5f, Ny = 0.5f, Scale = 1f };
            _map[id] = p;
        }
        p.Hidden = hidden;
    }

    public static void Save()
    {
        EnsureLoaded();
        StringBuilder sb = new StringBuilder();
        foreach (var kv in _map)
        {
            sb.Append(kv.Key).Append(':')
              .Append(kv.Value.Nx.ToString("R", CultureInfo.InvariantCulture)).Append(',')
              .Append(kv.Value.Ny.ToString("R", CultureInfo.InvariantCulture)).Append(',')
              .Append(kv.Value.Scale.ToString("R", CultureInfo.InvariantCulture)).Append(',')
              .Append(kv.Value.Hidden ? '1' : '0').Append(';');
        }
        PlayerPrefs.SetString(PrefsKey, sb.ToString());
        PlayerPrefs.Save();
    }

    public static void Load()
    {
        if (_map == null) _map = new Dictionary<string, Placement>();
        _map.Clear();

        string data = PlayerPrefs.GetString(PrefsKey, string.Empty);
        if (string.IsNullOrEmpty(data)) return;

        foreach (string entry in data.Split(';'))
        {
            if (string.IsNullOrEmpty(entry)) continue;
            int colon = entry.IndexOf(':');
            if (colon <= 0) continue;

            string id = entry.Substring(0, colon);
            string[] parts = entry.Substring(colon + 1).Split(',');
            if (parts.Length < 3) continue;

            float nx, ny, sc;
            if (float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out nx)
                && float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out ny)
                && float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out sc))
            {
                // 4th field (Hidden) is optional — older saves without it parse as not-hidden.
                bool hidden = parts.Length >= 4 && parts[3] == "1";
                _map[id] = new Placement
                {
                    Nx = Mathf.Clamp01(nx),
                    Ny = Mathf.Clamp01(ny),
                    Scale = Mathf.Clamp(sc, MinScale, MaxScale),
                    Hidden = hidden,
                };
            }
        }
    }

    // Clears all customization; controls revert to their default placement on the next ApplyLayout.
    public static void ResetAll()
    {
        EnsureLoaded();
        _map.Clear();
        PlayerPrefs.DeleteKey(PrefsKey);
        PlayerPrefs.Save();
    }
}

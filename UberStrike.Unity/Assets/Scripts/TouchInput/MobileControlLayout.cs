#if UNITY_EDITOR || UNITY_IOS || UNITY_IPHONE || UNITY_ANDROID
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
        public float Rotation;  // degrees, D-pad only
    }

    private const string PrefsKey = "MobileControlLayout";
    // Size limits: 1.6x minimum (twice the previous 0.8x floor) and 3.75x maximum
    // (1.5x the previous 2.5x cap). Buttons can't be shrunk into dots or inflated off-screen.
    public const float MinScale = 1.6f;
    public const float MaxScale = 3.75f;
    public const float MinRotation = -90f;
    public const float MaxRotation = 90f;

    // True while the in-game layout editor is open (read by TouchController to suspend live input).
    public static bool EditMode;

    private static Dictionary<string, Placement> _map;

    private static void EnsureLoaded()
    {
        if (_map != null) return;
        _map = new Dictionary<string, Placement>();
        Load();
    }

    /// <summary>
    /// Returns the saved placement for <paramref name="id"/>, or seeds one from the supplied default
    /// pixel center / scale / rotation if none exists yet.
    /// </summary>
    public static Placement GetOrDefault(string id, Vector2 defaultGuiCenter, float defaultScale, float defaultRotation = 0f)
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
                Rotation = defaultRotation,
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

    public static void SetRotation(string id, float rotation)
    {
        EnsureLoaded();
        Placement p;
        if (_map.TryGetValue(id, out p))
            p.Rotation = Mathf.Clamp(rotation, MinRotation, MaxRotation);
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
              .Append(kv.Value.Rotation.ToString("R", CultureInfo.InvariantCulture)).Append(';');
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
                // Layouts saved before rotation support keep the classic 15° D-pad tilt.
                float rot = 15f;
                if (parts.Length >= 4)
                {
                    float parsedRot;
                    if (float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out parsedRot))
                        rot = Mathf.Clamp(parsedRot, MinRotation, MaxRotation);
                }

                _map[id] = new Placement
                {
                    Nx = Mathf.Clamp01(nx),
                    Ny = Mathf.Clamp01(ny),
                    Scale = Mathf.Clamp(sc, MinScale, MaxScale),
                    Rotation = rot,
                };
            }
        }
    }

    // Clears all customization; controls revert to their default placement on the next ApplyLayout.
    /// <summary>
    /// Clears the in-memory placements so the next ApplyLayout re-seeds every control at its default.
    /// Deliberately does NOT touch the persisted PlayerPrefs: the editor's Cancel (Load + ApplyLayout)
    /// can therefore revert a reset back to the last saved layout, while Save persists the defaults
    /// if the reset is confirmed.
    /// </summary>
    public static void ResetAll()
    {
        EnsureLoaded();
        _map.Clear();
    }
}
#endif

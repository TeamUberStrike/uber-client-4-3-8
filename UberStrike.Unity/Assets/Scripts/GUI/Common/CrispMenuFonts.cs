using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Keeps menu text sharp when the menu is scaled up (desktop 1440p / 4K, see MobileMenuScale).
///
/// The menu fonts are TTFs imported at one fixed pixel size (e.g. 11 px), and the menu is scaled with
/// GUI.matrix, so at 2x every glyph is an 11 px bitmap stretched to 22 px: blurry. IMGUI has no way to
/// rasterise text at the scaled size, so for each menu font a copy is made whose glyphs are rendered
/// from the same TTF at the real on-screen size (font size * scale), with the glyph quads kept at the
/// original size. Under the scaled matrix every texel then lands on one screen pixel. Advances stay the
/// original ones, so text widths and the whole layout are unchanged.
///
/// The copies are put into the GUI skins' styles (BlueStonez, StormFront, ...), which every menu draws
/// with; at scale 1 the original fonts go back. TTF-imported fonts always draw with their built-in
/// texture, so each copy starts from a "slot" (Resources/CrispFonts/Slots, made by the editor script
/// CrispFontAssets): a custom font with the original's metrics and no texture, which draws with its
/// material. A new copy is made per scale, as IMGUI caches laid-out text per font.
/// </summary>
public static class CrispMenuFonts
{
    private const string SlotFolder = "CrispFonts/Slots/";

    private class Entry
    {
        public Font Original;
        public CharacterInfo[] OriginalInfo;
        public Font Slot;
        public Font Source;
        public Font Crisp;
    }

    // Original menu font -> entry, and any crisp copy (current or older) -> its original.
    private static readonly Dictionary<Font, Entry> _entries = new Dictionary<Font, Entry>();
    private static readonly Dictionary<Font, Font> _originalOf = new Dictionary<Font, Font>();
    private static readonly HashSet<Font> _slots = new HashSet<Font>();
    private static Font _bold;
    private static Font _medium;
    private static float _scale = 1f;
    private static float _built = -1f;
    private static float _nextScan;
    private static bool _rebuilt;
    private static bool _hooked;

    /// <summary>Called from MobileMenuScale.Begin with the current menu scale. Cheap when nothing changed.</summary>
    public static void Ensure(float scale)
    {
        if (scale < 1.01f) scale = 1f;
        bool changed = Mathf.Abs(scale - _scale) > 0.001f;
        if (!changed && Time.unscaledTime < _nextScan) return;
        _nextScan = Time.unscaledTime + 2f;
        _scale = scale;

        if (!_hooked)
        {
            _hooked = true;
            Font.textureRebuilt += OnTextureRebuilt;
            Application.quitting += () => SwapSkinFonts(1f);
        }

        // Skins and pages load fonts over time, so look for new menu fonts now and then.
        bool found = false;
        foreach (Font font in Resources.FindObjectsOfTypeAll<Font>())
        {
            if (font == null || font.dynamic || !font.name.StartsWith("InterparkGothic") || _entries.ContainsKey(font) || _originalOf.ContainsKey(font) || _slots.Contains(font))
                continue;
            Font slot = Resources.Load<Font>(SlotFolder + font.name);
            if (slot == null) continue;
            _slots.Add(slot);
            _entries[font] = new Entry { Original = font, OriginalInfo = font.characterInfo, Slot = slot };
            found = true;
        }

        if (scale != 1f && (found || Mathf.Abs(_built - scale) > 0.001f))
            Build(scale);
        SwapSkinFonts(scale);
    }

    private static void Build(float scale)
    {
        // All sizes share one glyph atlas per source font, and every request can grow (rebuild) it, which
        // moves the glyphs already in it. So request every size first, until a full pass causes no rebuild,
        // and only then read the glyph positions.
        for (int tries = 0; tries < 4; tries++)
        {
            _rebuilt = false;
            foreach (Entry entry in _entries.Values)
            {
                if (entry.Original == null) continue;
                if (entry.Source == null) entry.Source = entry.Original.name.Contains("Medium") ? LoadMedium() : LoadBold();
                if (entry.Source != null) entry.Source.RequestCharactersInTexture(Characters(entry), Size(entry, scale), FontStyle.Normal);
            }
            if (!_rebuilt) break;
        }

        foreach (Entry entry in _entries.Values)
        {
            if (entry.Original == null || entry.Source == null) continue;
            // Older copies are kept: styles copied from the skin while they were active may still use them.
            entry.Crisp = MakeCrisp(entry, scale);
            _originalOf[entry.Crisp] = entry.Original;
        }
        _built = scale;
    }

    private static Font MakeCrisp(Entry entry, float scale)
    {
        int size = Size(entry, scale);
        CharacterInfo[] infos = new CharacterInfo[entry.OriginalInfo.Length];
        for (int i = 0; i < infos.Length; i++)
        {
            CharacterInfo original = entry.OriginalInfo[i];
            CharacterInfo info = original;
            CharacterInfo hi;
#pragma warning disable 618
            if (entry.Source.GetCharacterInfo((char)original.index, out hi, size, FontStyle.Normal))
            {
                info.uv = hi.uv;
                info.flipped = hi.flipped;
                // Glyph quad in original (menu) units: the hi-res glyph divided by the scale. The advance
                // (width) stays the original one so text widths and layout don't move.
                info.vert = new Rect(hi.vert.x / scale, hi.vert.y / scale, hi.vert.width / scale, hi.vert.height / scale);
            }
            else
            {
                info.vert = new Rect(0f, 0f, 0f, 0f);
            }
#pragma warning restore 618
            infos[i] = info;
        }

        // A copy of the slot (same metrics as the original, no built-in texture), so it draws with our material.
        Font crisp = Object.Instantiate(entry.Slot);
        crisp.name = entry.Original.name + " (crisp " + scale.ToString("0.##") + "x)";
        Material material = new Material(entry.Original.material);
        material.mainTexture = entry.Source.material.mainTexture;
        crisp.material = material;
        crisp.characterInfo = infos;
        return crisp;
    }

    // Point every style of every loaded GUI skin at the crisp copies (or back at the originals at scale 1).
    private static void SwapSkinFonts(float scale)
    {
        foreach (GUISkin skin in Resources.FindObjectsOfTypeAll<GUISkin>())
        {
            if (skin == null) continue;
            skin.font = Pick(skin.font, scale);
            foreach (GUIStyle style in skin)
                if (style != null) style.font = Pick(style.font, scale);
        }
    }

    private static Font Pick(Font current, float scale)
    {
        if (current == null) return null;
        Font original;
        if (!_originalOf.TryGetValue(current, out original)) original = current;
        Entry entry;
        if (!_entries.TryGetValue(original, out entry)) return current;
        return scale == 1f || entry.Crisp == null ? original : entry.Crisp;
    }

    private static int Size(Entry entry, float scale)
    {
        return Mathf.Max(1, Mathf.RoundToInt(entry.Original.fontSize * scale));
    }

    private static string Characters(Entry entry)
    {
        char[] chars = new char[entry.OriginalInfo.Length];
        for (int i = 0; i < chars.Length; i++) chars[i] = (char)entry.OriginalInfo[i].index;
        return new string(chars);
    }

    private static void OnTextureRebuilt(Font font)
    {
        if (font != _bold && font != _medium) return;
        _rebuilt = true;
        // Glyphs moved: rebuild the copies on the next Ensure (Build itself reads positions only after
        // its requests settle).
        _built = -1f;
        _nextScan = 0f;
    }

    private static Font LoadBold()
    {
        if (_bold == null) _bold = Resources.Load<Font>("CrispFonts/InterparkGothicBold");
        return _bold;
    }

    private static Font LoadMedium()
    {
        if (_medium == null) _medium = Resources.Load<Font>("CrispFonts/InterparkGothicMedium");
        return _medium;
    }
}

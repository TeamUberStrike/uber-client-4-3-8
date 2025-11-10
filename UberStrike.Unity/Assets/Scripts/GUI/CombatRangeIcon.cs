using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class CombatRangeIcon : MonoSingleton<CombatRangeIcon>
{
    [SerializeField]
    private Texture2D[] _closeRange;
    [SerializeField]
    private Texture2D[] _midRange;
    [SerializeField]
    private Texture2D[] _farRange;

    [SerializeField]
    private Texture _rangeClose;
    [SerializeField]
    private Texture _rangeMedium;
    [SerializeField]
    private Texture _rangeFar;
    [SerializeField]
    private Texture _rangeBackground;

    [SerializeField]
    private Texture2D _rangeIconClose;
    [SerializeField]
    private Texture2D _rangeIconCloseMid;
    [SerializeField]
    private Texture2D _rangeIconFar;
    [SerializeField]
    private Texture2D _rangeIconMid;
    [SerializeField]
    private Texture2D _rangeIconMidFar;

    //public List<Weapon> test;
    //public CombatRangeTier map;
    //public GUISkin skin;

    //[Serializable]
    //public class Weapon
    //{
    //    public int Tier;
    //    public CombatRangeCategory Range;
    //}

    //void OnGUI()
    //{
    //    int index = 0;
    //    foreach (var t in test)
    //    {
    //        DrawWeaponRangeIcon(new Rect(100, (120 * ++index), 100, 100), new ItemWeapon() { CombatRange = t.Range, Tier = t.Tier });
    //        DrawRecommendedCombatRange(new Rect(100, (120 * index), 100, 100), map, new ItemWeapon() { CombatRange = t.Range, Tier = t.Tier });
    //    }
    //    DrawWeaponRangeIcon(new Rect(100, (120 * ++index), 100, 100), test.ConvertAll<ItemWeapon>(t => new ItemWeapon() { CombatRange = t.Range, Tier = t.Tier }).ToArray());
    //    DrawRecommendedCombatRange(new Rect(100, (120 * index), 100, 100), map, test.ConvertAll<ItemWeapon>(t => new ItemWeapon() { CombatRange = t.Range, Tier = t.Tier }).ToArray());

    //    GuiManager.DrawTooltip();
    //}

    // Use this for initialization
    void OnEnable()
    {
        //BlueStonez.Initialize(skin);

        if (_closeRange.Length != 5) throw new Exception("Close Range icons in CombatRangeIcon class not set!");
        if (_midRange.Length != 5) throw new Exception("Medium Range icons in CombatRangeIcon class not set!");
        if (_farRange.Length != 5) throw new Exception("Far Range icons in CombatRangeIcon class not set!");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="rect">Area where to draw the weapon range icon</param>
    /// <param name="ranges">Combat ranges of all weapons</param>
    public void DrawWeaponRangeIcon(Rect rect, params WeaponItem[] weapons)
    {
        int closeTier, medTier, farTier;
        GetBestTiersPerCombatRange(out closeTier, out medTier, out farTier, weapons);

        GUI.DrawTexture(rect, _closeRange[closeTier]);
        GUI.DrawTexture(rect, _midRange[medTier]);
        GUI.DrawTexture(rect, _farRange[farTier]);
    }

    public void DrawWeaponRangeIcon2(Rect rect, params  WeaponItem[] weapons)
    {
        int closeTier, medTier, farTier;
        GetBestTiersPerCombatRange(out closeTier, out medTier, out farTier, weapons);

        GUI.color = new Color(1, 0, 0, 0.25f * closeTier);
        GUI.DrawTexture(rect, _rangeClose);
        GUI.color = new Color(1, 0, 0, 0.25f * medTier);
        GUI.DrawTexture(rect, _rangeMedium);
        GUI.color = new Color(1, 0, 0, 0.25f * farTier);
        GUI.DrawTexture(rect, _rangeFar);
        GUI.color = Color.white;
        GUI.DrawTexture(rect, _rangeBackground);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="rect">Area where to draw the overlay the recommendation</param>
    /// <param name="mapRange">Combat range of the map</param>
    /// <param name="weaponRanges">Combat ranges of all weapons</param>
    public void DrawRecommendedCombatRange(Rect rect, CombatRangeTier mapRange, params WeaponItem[] weapons)
    {
        int closeTier, medTier, farTier;
        GetBestTiersPerCombatRange(out closeTier, out medTier, out farTier, weapons);

        //create tooltips
        if (rect.Contains(Event.current.mousePosition))
        {
            string closeRange = CreateToolTip(closeTier, "You need a shotgun or a pistol here !");
            string midRange = CreateToolTip(medTier, "You need a splatter, a canon or a machinegun here !");
            string longRange = CreateToolTip(farTier, "You need a sniper rifle here !");

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Combat Efficiency");
            builder.Append("Long: ").AppendLine(longRange);
            builder.Append("Medium: ").AppendLine(midRange);
            builder.Append("Close: ").AppendLine(closeRange);
            GUI.tooltip = builder.ToString();
        }

        //create flashy warning pattern
        CombatRangeCategory warningRange = 0;
        if (closeTier - mapRange.CloseRange < 0) warningRange |= CombatRangeCategory.Close;
        if (medTier - mapRange.MediumRange < 0) warningRange |= CombatRangeCategory.Medium;
        if (farTier - mapRange.LongRange < 0) warningRange |= CombatRangeCategory.Far;

        if (warningRange != 0)
        {
            // flash a few times and then wait a bit.
            // float alpha = (Mathf.Sin(Time.time * 9) + 1) * 0.5f * Mathf.Sin(Time.time);
            float alpha = (Mathf.Sin(Time.time * 9) + 1) * 0.3f;
            GUI.color = Color.white.SetAlpha(alpha);
            foreach (var t in GetRangeTextures(warningRange))
            {
                GUI.DrawTexture(rect, t);
            }
            GUI.color = Color.white;
        }

        _warningRange = (int)warningRange;
    }

    public int _warningRange;

    private static string CreateToolTip(int tier, string defaultText)
    {
        switch (tier)
        {
            case 0: return "Weak";
            case 1: return "Average";
            case 2: return "Good";
            case 3: return "Excellent";
            default: return defaultText;
        }
    }

    private static void GetBestTiersPerCombatRange(out int close, out int medium, out int far, WeaponItem[] weapons)
    {
        close = 0; medium = 0; far = 0;
        foreach (var w in weapons)
        {
            if (w != null && IsCloseRange(w.Configuration.CombatRange)) close = Mathf.Max(close, w.Configuration.Tier);
            if (w != null && IsMidRange(w.Configuration.CombatRange)) medium = Mathf.Max(medium, w.Configuration.Tier);
            if (w != null && IsFarRange(w.Configuration.CombatRange)) far = Mathf.Max(far, w.Configuration.Tier);
        }
        close = Mathf.Min(close, 3);
        medium = Mathf.Min(medium, 3);
        far = Mathf.Min(far, 3);
    }

    private IEnumerable<Texture2D> GetRangeTextures(CombatRangeCategory range)
    {
        switch (range)
        {
            case CombatRangeCategory.Close: return new[] { _closeRange[4] };
            case CombatRangeCategory.Medium: return new[] { _midRange[4] };
            case CombatRangeCategory.Far: return new[] { _farRange[4] };
            case CombatRangeCategory.MediumFar: return new[] { _midRange[4], _farRange[4] };
            case CombatRangeCategory.CloseMedium: return new[] { _closeRange[4], _midRange[4] };
            case CombatRangeCategory.CloseMediumFar: return new[] { _closeRange[4], _midRange[4], _farRange[4] };
            case CombatRangeCategory.CloseFar: return new[] { _closeRange[4], _farRange[4] };
            default: return new Texture2D[] { };
        }
    }

    public static bool IsCloseRange(CombatRangeCategory range)
    {
        return (range & CombatRangeCategory.Close) == CombatRangeCategory.Close;
    }

    public static bool IsMidRange(CombatRangeCategory range)
    {
        return (range & CombatRangeCategory.Medium) == CombatRangeCategory.Medium;
    }

    public static bool IsFarRange(CombatRangeCategory range)
    {
        return (range & CombatRangeCategory.Far) == CombatRangeCategory.Far;
    }

    public Texture2D GetIconByRange(CombatRangeCategory range)
    {
        switch (range)
        {
            case CombatRangeCategory.Close:
                return _rangeIconClose;
            case CombatRangeCategory.CloseMedium:
                return _rangeIconCloseMid;
            case CombatRangeCategory.Far:
                return _rangeIconFar;
            case CombatRangeCategory.Medium:
                return _rangeIconMid;
            case CombatRangeCategory.MediumFar:
                return _rangeIconMidFar;
            default:
                Debug.LogWarning("Cannot find corresponding icon for range - [" + range + "].");
                return _rangeIconMidFar;
        }
    }
}
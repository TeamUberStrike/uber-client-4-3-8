using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Headless/Editor verification for GuiFontOverrideFix. Proves the inert bitmap-font fontSize/fontStyle
/// override is cleared on the ACTUAL style objects the price rows draw with — without needing a server or
/// device. The IMGUI warning fires IFF (effective font is non-dynamic) AND (fontSize != 0 OR fontStyle !=
/// Normal); after the fix the second clause is false, so the warning cannot fire.
/// Run: Tools ▸ Mobile ▸ Verify Font Override Fix, or -executeMethod GuiFontOverrideFixVerify.Run
/// </summary>
public static class GuiFontOverrideFixVerify
{
    private static readonly string[] Targets =
    {
        "label_interparkmed_11pt_left",
        "label_interparkmed_11pt_right",
        "label_interparkbold_11pt_left",
        "label_interparkbold_11pt_right",
    };

    [MenuItem("Tools/Mobile/Verify Font Override Fix")]
    public static void Run()
    {
        var sb = new StringBuilder();
        sb.AppendLine("[FontFixVerify] ==== BlueStonez price-style override check ====");

        GUISkin skin = BlueStonez.Skin; // triggers static init (loads skin + builds styles)
        if (skin == null) { Debug.LogError("[FontFixVerify] BlueStonez.Skin is null"); return; }
        Font def = skin.font;

        bool allPass = true;

        // 1) Force the shipped-style "dirty" override back on, to prove the warning condition reproduces
        //    AND that the fix then clears it (independent of whether Apply already ran at editor load).
        foreach (string name in Targets)
        {
            GUIStyle s = skin.GetStyle(name);
            Font f = s.font != null ? s.font : def;
            bool dynamic = f != null && f.dynamic;
            s.fontSize = 11;
            s.fontStyle = FontStyle.Bold;
            bool wouldWarnBefore = !dynamic && (s.fontSize != 0 || s.fontStyle != FontStyle.Normal);
            sb.AppendLine(string.Format("  {0}: font='{1}' dynamic={2}  BEFORE fontSize={3} style={4} wouldWarn={5}",
                name, f != null ? f.name : "<skin default null>", dynamic, s.fontSize, s.fontStyle, wouldWarnBefore));
        }

        // 2) Apply the fix.
        GuiFontOverrideFix.NeutralizeAll();

        // 3) Re-check the same objects.
        foreach (string name in Targets)
        {
            GUIStyle s = skin.GetStyle(name);
            Font f = s.font != null ? s.font : def;
            bool dynamic = f != null && f.dynamic;
            bool wouldWarnAfter = !dynamic && (s.fontSize != 0 || s.fontStyle != FontStyle.Normal);
            if (wouldWarnAfter) allPass = false;
            sb.AppendLine(string.Format("  {0}: AFTER fontSize={1} style={2} wouldWarn={3}",
                name, s.fontSize, s.fontStyle, wouldWarnAfter));
        }

        // 4) Whole-skin sweep: how many custom styles still meet the warn condition after the fix?
        int remaining = 0, total = 0;
        if (skin.customStyles != null)
        {
            foreach (GUIStyle s in skin.customStyles)
            {
                if (s == null) continue;
                total++;
                Font f = s.font != null ? s.font : def;
                if (f != null && !f.dynamic && (s.fontSize != 0 || s.fontStyle != FontStyle.Normal))
                    remaining++;
            }
        }
        sb.AppendLine(string.Format("  whole-skin sweep: {0}/{1} custom styles still wouldWarn after fix (expect 0)", remaining, total));
        if (remaining != 0) allPass = false;

        sb.AppendLine("[FontFixVerify] RESULT: " + (allPass ? "PASS — no style will emit the font-override warning" : "FAIL — see above"));
        Debug.Log(sb.ToString());
    }
}

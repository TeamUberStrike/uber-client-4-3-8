using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// In-game editor for the customizable on-screen control layout. While open it dims the screen,
/// draws every repositionable control with a drag handle, and lets the player drag to move and use
/// a slider to scale the selected control, then Save / Reset / Done. Persisted via
/// <see cref="MobileControlLayout"/>.
///
/// All input goes through IMGUI events (Event.current), which are synthesized from touches on
/// device and from the mouse in the Unity Editor / Device Simulator — so it is fully previewable
/// in-Editor without a device build.
/// </summary>
public class MobileControlLayoutEditor : MonoBehaviour
{
    private string _selectedId;
    private bool _dragging;
    private Vector2 _dragOffset;
    private Rect _toolbarRect;   // the floating control panel rect = the drag-exclusion zone
    private Texture2D _pixel;

    private float _ui = 1f;      // UI scale (screen-height based) so the panel isn't tiny on a phone
    private GUIStyle _titleStyle, _btnStyle, _labelStyle, _sliderStyle, _thumbStyle, _handleLabelStyle;

    private void Awake()
    {
        _pixel = Texture2D.whiteTexture;
    }

    // Built once (during OnGUI, when GUI.skin exists). Sizes scale with the screen so buttons/slider
    // are big enough to tap on a high-DPI phone (the old default-skin toolbar was unreadably small).
    private void EnsureStyles()
    {
        if (_btnStyle != null) return;
        int big = Mathf.RoundToInt(19f * _ui);
        _btnStyle = new GUIStyle(GUI.skin.button) { fontSize = big, fontStyle = FontStyle.Bold };
        _titleStyle = new GUIStyle(GUI.skin.label) { fontSize = big, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = false };
        _labelStyle = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(15f * _ui), alignment = TextAnchor.MiddleCenter };
        _handleLabelStyle = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(13f * _ui), alignment = TextAnchor.UpperCenter };
        _sliderStyle = new GUIStyle(GUI.skin.horizontalSlider) { fixedHeight = 16f * _ui };
        _thumbStyle = new GUIStyle(GUI.skin.horizontalSliderThumb) { fixedWidth = 34f * _ui, fixedHeight = 34f * _ui };
    }

    // The floating control panel: centered horizontally, just below the top safe-area inset, sized so
    // NOTHING sits on a screen edge or under the notch (Pixel-Gun-3D style). Returns its rect, which also
    // becomes the drag-exclusion zone so taps on the panel never move a control behind it.
    private Rect ComputePanelRect(bool hasSelection)
    {
        Rect sa = Screen.safeArea;                       // y-up from bottom
        float topInset = Screen.height - sa.yMax;        // px from top edge to safe-area top
        float sideInset = Mathf.Max(sa.x, Screen.width - sa.xMax);

        float pad = 24f * _ui;
        float pw = Mathf.Min(700f * _ui, Screen.width - 2f * (sideInset + pad));
        float ph = (hasSelection ? 184f : 116f) * _ui;
        float px = (Screen.width - pw) * 0.5f;
        float py = topInset + 18f * _ui;
        return new Rect(px, py, pw, ph);
    }

    private void OnDisable()
    {
        // Safety net: never leave the lobby torn down if this host is disabled while editing.
        if (MobileMenuBackdrop.IsActive)
        {
            MobileControlLayout.EditMode = false;
            MobileMenuBackdrop.Exit();
            _editModePrev = false;
        }
    }

    private bool _autoOpened;
    private bool _editModePrev;

    private void Update()
    {
        // In the Editor standalone preview, open the layout editor automatically so the draggable
        // controls are visible the moment Play starts (no button to find).
        if (MobileControlLayout.PreviewStandalone && !_autoOpened)
        {
            MobileControlLayout.EditMode = true;
            _autoOpened = true;
        }

        // On the edit-mode edges, engage / release the lobby backdrop (hides the menu UI + avatar and
        // shows the spaceship scene as a clean settings background). MobileMenuBackdrop self-gates to
        // the lobby, so this is a no-op in a match and in the Editor force-preview.
        bool editMode = MobileControlLayout.EditMode;
        if (editMode && !_editModePrev) MobileMenuBackdrop.Enter();
        else if (!editMode && _editModePrev) MobileMenuBackdrop.Exit();
        _editModePrev = editMode;

        // Freeze look/move while editing so dragging never leaks into gameplay.
        if (editMode)
        {
            TouchInput.WishDirection = Vector2.zero;
            TouchInput.WishLook = Vector2.zero;
        }
    }

    private void OnGUI()
    {
        // Only draws while editing. The customize editor is opened explicitly from
        // Options ▸ Controls ▸ "Customize On-Screen Controls" (sets EditMode), so there is NO floating
        // "Edit Controls" button cluttering gameplay, the lobby, or the Editor preview.
        if (!MobileControlLayout.EditMode) return;

        DrawEditor();
    }

    private void DrawEditor()
    {
        // Live handles from the running control set, or synthesized handles for the no-match preview.
        List<TouchInput.LayoutHandle> handles = TouchInput.Exists
            ? TouchInput.Instance.GetLayoutHandles()
            : BuildStandaloneHandles();

        // Dim the world behind the editor. With the lobby backdrop active the menu UI + avatar are
        // already hidden and we want the spaceship scene to read as the background, so use a light
        // scrim (just enough to keep the white control outlines/labels legible) instead of the heavy
        // in-match dim.
        _ui = Mathf.Clamp(Screen.height / 720f, 1f, 2.5f);
        EnsureStyles();

        Color prev = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, MobileMenuBackdrop.IsActive ? 0.2f : 0.5f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _pixel);
        GUI.color = prev;

        // The floating control panel (centered, off all edges). Computed before HandleDrag so its rect
        // is the drag-exclusion zone; drawn after the handles so it sits on top.
        _toolbarRect = ComputePanelRect(!string.IsNullOrEmpty(_selectedId));

        // Draw each control + its outline + label.
        foreach (TouchInput.LayoutHandle hd in handles)
        {
            bool selected = (hd.Id == _selectedId);

            if (hd.Icon != null)
            {
                GUI.DrawTexture(hd.Boundary, hd.Icon, ScaleMode.StretchToFill, true);
            }
            else
            {
                GUI.color = new Color(0.35f, 0.55f, 1f, 0.85f);
                GUI.Box(hd.Boundary, GUIContent.none);
                GUI.color = Color.white;
            }

            DrawOutline(hd.Boundary, selected ? Color.yellow : new Color(1f, 1f, 1f, 0.6f), selected ? 3f : 1f);

            GUI.Label(new Rect(hd.Boundary.x - 10, hd.Boundary.yMax + 2, Mathf.Max(hd.Boundary.width + 20, 90), 20 * _ui), hd.DisplayName, _handleLabelStyle);
        }

        HandleDrag(handles);

        DrawToolbar(handles);
    }

    private void HandleDrag(List<TouchInput.LayoutHandle> handles)
    {
        Event e = Event.current;
        Vector2 mouse = e.mousePosition;

        switch (e.type)
        {
            case EventType.MouseDown:
                if (_toolbarRect.Contains(mouse)) break; // toolbar owns this click
                for (int i = handles.Count - 1; i >= 0; i--) // top-most first
                {
                    if (handles[i].Boundary.Contains(mouse))
                    {
                        _selectedId = handles[i].Id;
                        _dragging = true;
                        _dragOffset = mouse - handles[i].Boundary.center;
                        e.Use();
                        break;
                    }
                }
                break;

            case EventType.MouseDrag:
                if (_dragging && !string.IsNullOrEmpty(_selectedId))
                {
                    Vector2 center = mouse - _dragOffset;
                    // Keep controls on-screen and off the top notch (safe-area aware). The panel itself
                    // is the drag-exclusion zone (handled on MouseDown), so controls may sit beside it.
                    float topInset = Screen.height - Screen.safeArea.yMax;
                    center.x = Mathf.Clamp(center.x, 24f, Screen.width - 24f);
                    center.y = Mathf.Clamp(center.y, topInset + 24f, Screen.height - 24f);
                    float scale = GetScale(handles, _selectedId);
                    MobileControlLayout.SetPixels(_selectedId, center, scale);
                    if (TouchInput.Exists) TouchInput.Instance.ApplyLayout();
                    e.Use();
                }
                break;

            case EventType.MouseUp:
                if (_dragging)
                {
                    _dragging = false;
                    e.Use();
                }
                break;
        }
    }

    private void DrawToolbar(List<TouchInput.LayoutHandle> handles)
    {
        bool hasSel = !string.IsNullOrEmpty(_selectedId);
        Rect panel = _toolbarRect;
        float pad = 18f * _ui;

        // Panel background + outline.
        GUI.color = new Color(0.06f, 0.07f, 0.10f, 0.93f);
        GUI.DrawTexture(panel, _pixel);
        GUI.color = Color.white;
        DrawOutline(panel, new Color(1f, 1f, 1f, 0.22f), 2f);

        // Title.
        GUI.Label(new Rect(panel.x + pad, panel.y + 10f * _ui, panel.width - 2f * pad, 28f * _ui),
            hasSel ? "Selected: " + DisplayName(handles, _selectedId)
                   : "Customize Controls — drag to move, tap to select",
            _titleStyle);

        // Size slider (only when a control is selected), between title and buttons.
        if (hasSel)
        {
            float sy = panel.y + 48f * _ui;
            GUI.Label(new Rect(panel.x + pad, sy, 60f * _ui, 30f * _ui), "Size", _labelStyle);
            float cur = GetScale(handles, _selectedId);
            float sliderX = panel.x + pad + 64f * _ui;
            float sliderW = panel.width - 2f * pad - 64f * _ui - 60f * _ui;
            float next = GUI.HorizontalSlider(new Rect(sliderX, sy + 6f * _ui, sliderW, 30f * _ui), cur, 0.6f, 2.2f, _sliderStyle, _thumbStyle);
            if (!Mathf.Approximately(next, cur))
            {
                MobileControlLayout.SetScale(_selectedId, next);
                if (TouchInput.Exists) TouchInput.Instance.ApplyLayout();
            }
            GUI.Label(new Rect(panel.xMax - pad - 56f * _ui, sy, 56f * _ui, 30f * _ui), cur.ToString("N2"), _labelStyle);
        }

        // Button row: Default (revert to factory) | Cancel (discard, keep last saved) | Save (persist + close).
        float bh = 50f * _ui;
        float by = panel.yMax - pad - bh;
        float gap = 10f * _ui;
        float bw = (panel.width - 2f * pad - 2f * gap) / 3f;

        GUI.backgroundColor = new Color(0.85f, 0.7f, 0.2f);   // Default — gold
        if (GUI.Button(new Rect(panel.x + pad, by, bw, bh), "Default", _btnStyle))
        {
            MobileControlLayout.ResetAll();
            if (TouchInput.Exists) TouchInput.Instance.ApplyLayout();
            _selectedId = null;
        }
        GUI.backgroundColor = new Color(0.8f, 0.3f, 0.3f);     // Cancel — red
        if (GUI.Button(new Rect(panel.x + pad + bw + gap, by, bw, bh), "Cancel", _btnStyle))
        {
            MobileControlLayout.Load();   // discard unsaved edits → revert to last saved layout
            if (TouchInput.Exists) TouchInput.Instance.ApplyLayout();
            _selectedId = null;
            MobileControlLayout.EditMode = false;
        }
        GUI.backgroundColor = new Color(0.3f, 0.75f, 0.35f);   // Save — green
        if (GUI.Button(new Rect(panel.x + pad + 2f * (bw + gap), by, bw, bh), "Save", _btnStyle))
        {
            MobileControlLayout.Save();
            MobileControlLayout.EditMode = false;
        }
        GUI.backgroundColor = Color.white;
    }

    private static float GetScale(List<TouchInput.LayoutHandle> handles, string id)
    {
        foreach (TouchInput.LayoutHandle h in handles)
            if (h.Id == id) return h.Scale;
        return 1f;
    }

    private static string DisplayName(List<TouchInput.LayoutHandle> handles, string id)
    {
        foreach (TouchInput.LayoutHandle h in handles)
            if (h.Id == id) return h.DisplayName;
        return id;
    }

    // The full set of customizable control ids (matches TouchInput's layout key map + weapon changer).
    private static readonly string[] StandaloneIds =
    {
        "joystick", "fire", "secondaryFire", "multiSecondaryFire", "jump", "crouch", "menu", "chat", "score", "weaponChanger",
        "quickItem1", "quickItem2", "quickItem3",
    };

    // Builds editable handles WITHOUT a live TouchInput (no-match preview): each control is placed
    // at its saved (or default) layout, with the icon pulled straight from MobileIcons.
    private static List<TouchInput.LayoutHandle> BuildStandaloneHandles()
    {
        var list = new List<TouchInput.LayoutHandle>();
        foreach (string id in StandaloneIds)
        {
            Texture icon = IconFor(id);
            MobileControlLayout.Placement p = MobileControlLayout.GetOrDefault(id, TouchInput.DefaultGuiCenter(id), 1f);
            Vector2 c = MobileControlLayout.ToPixels(p);

            float iw = (icon != null) ? icon.width : 64f;
            float ih = (icon != null) ? icon.height : 64f;
            float bw = iw * p.Scale;
            float bh = ih * p.Scale;

            list.Add(new TouchInput.LayoutHandle
            {
                Id = id,
                DisplayName = TouchInput.DisplayNameFor(id),
                Boundary = new Rect(c.x - bw / 2f, c.y - bh / 2f, bw, bh),
                Icon = icon,
                Scale = p.Scale,
            });
        }
        return list;
    }

    private static Texture IconFor(string id)
    {
        switch (id)
        {
            case "joystick": return MobileIcons.JoystickOuter;
            case "fire": return MobileIcons.FireIcon;
            case "secondaryFire":
            case "multiSecondaryFire": return MobileIcons.SecondFireIcon;
            case "jump": return MobileIcons.JumpIcon;
            case "crouch": return MobileIcons.CrouchIcon;
            case "menu": return MobileIcons.MenuIcon;
            case "chat": return MobileIcons.ChatIcon;
            case "score": return MobileIcons.ScoreboardIcon;
            case "quickItem1":
            case "quickItem2":
            case "quickItem3": return ConsumableHudTextures.AmmoBlue;
            case "weaponChanger":
            {
                Texture2D[] w = MobileIcons.WeaponIcons;
                return (w != null && w.Length > 0) ? w[w.Length - 1] : null;
            }
            default: return null;
        }
    }

    private void DrawOutline(Rect r, Color color, float thickness)
    {
        Color prev = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(new Rect(r.x, r.y, r.width, thickness), _pixel);
        GUI.DrawTexture(new Rect(r.x, r.yMax - thickness, r.width, thickness), _pixel);
        GUI.DrawTexture(new Rect(r.x, r.y, thickness, r.height), _pixel);
        GUI.DrawTexture(new Rect(r.xMax - thickness, r.y, thickness, r.height), _pixel);
        GUI.color = prev;
    }
}

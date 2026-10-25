#if UNITY_EDITOR || UNITY_IOS || UNITY_IPHONE || UNITY_ANDROID
using System.Collections.Generic;
using UnityEngine;
using DeviceApplication = UnityEngine.Device.Application;

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
    private bool _pinchActive;
    private bool _editModePrev;
    private bool _ribbonWasEnabled;
    private float _pinchStartDist;
    private float _pinchBaseScale;
    private bool _dragging;
    private Vector2 _dragOffset;
    private Rect _toolbarRect;
    private Texture2D _pixel;
    private MeshGUIText _hintText;

    private static bool ShouldShow
    {
        get
        {
            if (ApplicationDataManager.IsMobile || DeviceApplication.isMobilePlatform) return true;
#if UNITY_EDITOR
            return MobileMenuScale.ForcePreviewInEditor;
#else
            return false;
#endif
        }
    }

    private void Awake()
    {
        _pixel = Texture2D.whiteTexture;
    }

    private void Update()
    {
        bool editMode = MobileControlLayout.EditMode;

        // Hide the ribbon (PROFILE / CHAT bar) while editing, restore it afterwards.
        if (editMode && !_editModePrev)
        {
            _ribbonWasEnabled = GlobalUIRibbon.Exists && GlobalUIRibbon.Instance.IsEnabled;
            if (_ribbonWasEnabled) GlobalUIRibbon.Instance.Hide();
            if (_hintText != null) _hintText.Show();
        }
        else if (!editMode && _editModePrev)
        {
            if (_ribbonWasEnabled && GlobalUIRibbon.Exists) GlobalUIRibbon.Instance.Show();
            if (_hintText != null) _hintText.Hide();
            if (HudAssets.Exists) WeaponsHud.Instance.QuickItems.RefreshPosition();
        }
        _editModePrev = editMode;

        // Freeze look/move while editing so dragging never leaks into gameplay.
        if (editMode)
        {
            TouchInput.WishDirection = Vector2.zero;
            TouchInput.WishLook = Vector2.zero;
            UpdatePinchScale();
            UpdateTouchDrag();
#if UNITY_EDITOR
            // Mouse scroll wheel resizes the selected control (quick debugging).
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.0001f && !string.IsNullOrEmpty(_selectedId) && !_dragging)
            {
                float cur = GetScale(TouchInput.Instance.GetLayoutHandles(), _selectedId);
                float next = Mathf.Clamp(cur + scroll * 0.1f, 0.5f, 2.5f);
                MobileControlLayout.SetScale(_selectedId, next);
                TouchInput.Instance.ApplyLayout();
            }
#endif
        }
        else if (_pinchActive)
        {
            _pinchActive = false;
        }
    }

    // Raw-touch selection + drag fallback. On iOS the IMGUI mouse synthesis from touches is not
    // reliable for this editor (HandleDrag's MouseDown never fired -> buttons couldn't be selected
    // or moved, and everything else is gated while EditMode is on -> total lockup). Touch.position
    // is bottom-left origin; the layout boundaries are top-left (GUI) space, so flip Y.
    private void UpdateTouchDrag()
    {
        Touch[] touches = TouchSource.Touches;
        if (touches.Length != 1 || _pinchActive) return; // pinch path owns two-finger input

        Touch t = touches[0];
        Vector2 pos = new Vector2(t.position.x, Screen.height - t.position.y);

        switch (t.phase)
        {
            case TouchPhase.Began:
                if (_toolbarRect.Contains(pos)) return;
                {
                    List<TouchInput.LayoutHandle> handles = TouchInput.Exists
                        ? TouchInput.Instance.GetLayoutHandles()
                        : null;
                    if (handles == null) return;
                    for (int i = handles.Count - 1; i >= 0; i--)
                    {
                        if (handles[i].Boundary.Contains(pos))
                        {
                            _selectedId = handles[i].Id;
                            _dragging = true;
                            _dragOffset = pos - handles[i].Boundary.center;
                            return;
                        }
                    }
                }
                break;

            case TouchPhase.Moved:
                if (_dragging && !string.IsNullOrEmpty(_selectedId) && TouchInput.Exists)
                {
                    Vector2 center = pos - _dragOffset;
                    center.x = Mathf.Clamp(center.x, 24f, Screen.width - 24f);
                    center.y = Mathf.Clamp(center.y, _toolbarRect.height + 24f, Screen.height - 24f);
                    float scale = GetScale(TouchInput.Instance.GetLayoutHandles(), _selectedId);
                    MobileControlLayout.SetPixels(_selectedId, center, scale);
                    TouchInput.Instance.ApplyLayout();
                }
                break;

            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                _dragging = false;
                break;
        }
    }

    // Two-finger pinch on a selected control scales it (smaller/bigger).
    private void UpdatePinchScale()
    {
        if (_dragging) return;

        Touch[] touches = TouchSource.Touches;
        if (touches.Length < 2)
        {
            if (_pinchActive) _pinchActive = false;
            return;
        }

        float dist = Vector2.Distance(touches[0].position, touches[1].position);
        if (!_pinchActive)
        {
            _pinchActive = true;
            _pinchStartDist = Mathf.Max(1f, dist);
            _pinchBaseScale = string.IsNullOrEmpty(_selectedId)
                ? 1f
                : GetScale(TouchInput.Instance.GetLayoutHandles(), _selectedId);
            return;
        }

        if (!string.IsNullOrEmpty(_selectedId))
        {
            float next = Mathf.Clamp(_pinchBaseScale * (dist / _pinchStartDist), 0.5f, 2.5f);
            MobileControlLayout.SetScale(_selectedId, next);
            TouchInput.Instance.ApplyLayout();
        }
    }

    private void OnGUI()
    {
        if (!ShouldShow) return;
        if (!TouchInput.Exists) return;

        if (!MobileControlLayout.EditMode)
        {
            return;
        }

        DrawEditor();
    }

    private void DrawEditor()
    {
        List<TouchInput.LayoutHandle> handles = TouchInput.Instance.GetLayoutHandles();

        // Reserve a top toolbar (next to the ribbon's PROFILE / CHAT tabs); drags that start here
        // are ignored (let buttons/slider work). The toolbar buttons are drawn in MobileMenuScale
        // space, so the exclusion zone must be their RAW pixel footprint, not a fixed 96px.
        float barH = (2f + 34f + 8f) * MobileMenuScale.Scale;
        _toolbarRect = new Rect(0, 0, Screen.width, barH);

        DrawMovementPreview();

        // Draw each control: soft backdrop, icon, colored border and a name badge above it.
        foreach (TouchInput.LayoutHandle hd in handles)
        {
            bool selected = (hd.Id == _selectedId);

            if (hd.Icon != null)
            {
                GUI.DrawTexture(hd.Boundary, hd.Icon, ScaleMode.StretchToFill, true);
            }

            Color border = selected ? new Color(1f, 0.85f, 0.2f) : new Color(0.45f, 0.65f, 1f, 0.9f);
            DrawOutline(hd.Boundary, border, selected ? 3f : 2f);

            // Name badge: a small pill above the button.
            GUIContent name = new GUIContent(hd.DisplayName);
            Vector2 textSize = BlueStonez.label_interparkbold_11pt.CalcSize(name);
            Rect tagRect = new Rect(hd.Boundary.center.x - textSize.x / 2 - 8, hd.Boundary.y - 22, textSize.x + 16, 20);
            GUI.color = selected ? new Color(1f, 0.85f, 0.2f, 0.95f) : new Color(0.10f, 0.13f, 0.20f, 0.9f);
            GUI.DrawTexture(tagRect, _pixel);
            GUI.color = selected ? Color.black : Color.white;
            GUI.Label(tagRect, name, BlueStonez.label_interparkbold_11pt);
            GUI.color = Color.white;
        }

        HandleDrag(handles);

        DrawToolbar(handles);
    }

    private void DrawMovementPreview()
    {
        if (!TouchInput.Exists) return;

        // The D-pad is now a real editable handle (drawn by the handle loop), so only draw the
        // joystick outline here, and only for single-input mode.
        if (ApplicationDataManager.ApplicationOptions.UseMultiTouch) return;

        if (TouchInput.Instance.Joystick != null)
        {
            Rect r = TouchInput.Instance.Joystick.Boundary;
            DrawOutline(r, new Color(0.45f, 0.65f, 1f, 0.9f), 1f);
            GUI.Label(new Rect(r.x + 10, r.yMax - 24, 160, 18), "Joystick area");
        }
    }

    private void HandleDrag(List<TouchInput.LayoutHandle> handles)
    {
        Event e = Event.current;
        Vector2 mouse = e.mousePosition;

        switch (e.type)
        {
            case EventType.MouseDown:
                if (_toolbarRect.Contains(mouse)) break; // toolbar owns this click
                Debug.Log("[LayoutDebug] mousedown at " + mouse);
                for (int i = handles.Count - 1; i >= 0; i--) // top-most first
                {
                    Debug.Log("[LayoutDebug]   handle " + handles[i].Id + " boundary=" + handles[i].Boundary);
                    if (handles[i].Boundary.Contains(mouse))
                    {
                        Debug.Log("[LayoutDebug]   HIT " + handles[i].Id);
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
                    center.x = Mathf.Clamp(center.x, 0, Screen.width);
                    center.y = Mathf.Clamp(center.y, _toolbarRect.height + 24f, Screen.height);
                    float scale = GetScale(handles, _selectedId);
                    MobileControlLayout.SetPixels(_selectedId, center, scale);
                    TouchInput.Instance.ApplyLayout();
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
        // Draw in the ribbon's scaled space so the buttons sit on the exact same vertical row as the
        // PROFILE / CHAT tabs (next to them on the right).
        Matrix4x4 scaleMatrix = MobileMenuScale.Begin();
        float vw = MobileMenuScale.VirtualWidth;
        float bw = 140, bh = 34;
        float totalW = 4 * bw + 3 * 8;
        float x0 = (vw - totalW) / 2;
        float by = 2;

        string modeLabel = ApplicationDataManager.ApplicationOptions.UseMultiTouch ? "Switch Single" : "Switch Multi";
        if (GUITools.Button(new Rect(x0, by, bw, bh), new GUIContent(modeLabel), StormFront.ButtonBlue))
        {
            TouchInput.Instance.ToggleMovementMode();
            _selectedId = null;
        }
        if (GUITools.Button(new Rect(x0 + (bw + 8), by, bw, bh), new GUIContent(LocalizedStrings.ResetDefaults), StormFront.ButtonBlue))
        {
            MobileControlLayout.ResetAll();
            TouchInput.Instance.ApplyLayout();
            _selectedId = null;
        }
        if (GUITools.Button(new Rect(x0 + 2 * (bw + 8), by, bw, bh), new GUIContent(LocalizedStrings.CancelCaps), StormFront.ButtonBlue))
        {
            MobileControlLayout.Load();
            TouchInput.Instance.ApplyLayout();
            _selectedId = null;
            MobileControlLayout.EditMode = false;
        }
        if (GUITools.Button(new Rect(x0 + 3 * (bw + 8), by, bw, bh), new GUIContent("Save"), StormFront.ButtonBlue))
        {
            MobileControlLayout.Save();
            MobileControlLayout.EditMode = false;
        }

        if (_hintText == null)
        {
            _hintText = new MeshGUIText("Drag buttons to move them, pinch to resize.", HudAssets.Instance.InterparkBitmapFont, TextAnchor.MiddleCenter);
            HudStyleUtility.Instance.SetBlueStyle(_hintText);
            _hintText.Scale = new Vector2(0.6f, 0.6f);
        }
        _hintText.Position = new Vector2(Screen.width / 2, Screen.height / 2 - 100);
        _hintText.Draw();

        MobileMenuScale.End(scaleMatrix);
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
#endif

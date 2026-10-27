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
    private float _pinchStartAngle;
    private float _pinchBaseScale;
    private float _pinchBaseRotation;
    private bool _dragging;         // actively moving the selected control
    private bool _dragArmed;        // pointer down on a control, not yet moved past the threshold
    private int _dragFingerId = -1; // finger that armed the drag; only it may move the control
    private Vector2 _dragStartPos;  // GUI-space position where the pointer went down
    private Vector2 _dragOffset;
    private Rect _toolbarButtonsRect;   // raw-pixel rect of the editor toolbar buttons (reserved field)

    // Finger/pointer must move this far (squared, 10px) before a touch becomes a drag, so a
    // second finger can still start a pinch on the control.
    private const float DragThresholdSqr = 100f;
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
                float next = Mathf.Clamp(cur + scroll * 0.1f, MobileControlLayout.MinScale, MobileControlLayout.MaxScale);
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

    // Raw-touch selection + drag. On iOS the IMGUI mouse synthesis from touches is not
    // reliable for this editor (HandleDrag's MouseDown never fired -> buttons couldn't be selected
    // or moved, and everything else is gated while EditMode is on -> total lockup). Touch.position
    // is bottom-left origin; the layout boundaries are top-left (GUI) space, so flip Y.
    //
    // A touch only ARMS the selected control; it becomes a drag once the finger actually moves
    // past a small threshold. Otherwise a second finger landing to pinch would find the control
    // already "dragging" and the pinch would never engage.
    private void UpdateTouchDrag()
    {
        Touch[] touches = TouchSource.Touches;

        // If the finger that armed the drag is gone (its Ended frame can be swallowed by the
        // length!=1 guard when a second finger is/was also down), disarm immediately — otherwise
        // the remaining finger becomes touches[0] and its movement, measured against the stale
        // baseline, would yank the control toward that finger.
        if (_dragArmed || _dragging)
        {
            bool live = false;
            for (int i = 0; i < touches.Length; i++)
            {
                if (touches[i].fingerId == _dragFingerId && touches[i].phase != TouchPhase.Ended && touches[i].phase != TouchPhase.Canceled)
                {
                    live = true;
                    break;
                }
            }
            if (!live)
            {
                _dragging = false;
                _dragArmed = false;
                _dragFingerId = -1;
            }
        }

        if (touches.Length == 0)
        {
            _dragging = false;
            _dragArmed = false;
            _dragFingerId = -1;
            return;
        }
        if (touches.Length != 1 || _pinchActive) return; // pinch path owns two-finger input

        Touch t = touches[0];
        Vector2 pos = new Vector2(t.position.x, Screen.height - t.position.y);

        switch (t.phase)
        {
            case TouchPhase.Began:
                if (_toolbarButtonsRect.Contains(pos)) return; // toolbar owns this area
                if (SelectControlAtGui(pos))
                {
                    _dragArmed = true;
                    _dragging = false;
                    _dragFingerId = t.fingerId;
                    _dragStartPos = pos;
                    _dragOffset = pos - HandleCenter(_selectedId);
                }
                break;

            case TouchPhase.Moved:
                if (t.fingerId != _dragFingerId) break; // only the arming finger can drag

                if (_dragArmed && !_dragging && (pos - _dragStartPos).sqrMagnitude > DragThresholdSqr)
                    _dragging = true;

                if (_dragging && !string.IsNullOrEmpty(_selectedId) && TouchInput.Exists)
                {
                    Vector2 center = ClampDragCenter(pos - _dragOffset);
                    float scale = GetScale(TouchInput.Instance.GetLayoutHandles(), _selectedId);
                    MobileControlLayout.SetPixels(_selectedId, center, scale);
                    TouchInput.Instance.ApplyLayout();
                }
                break;

            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                if (t.fingerId != _dragFingerId) break;
                _dragging = false;
                _dragArmed = false;
                _dragFingerId = -1;
                break;
        }
    }

    // Two-finger pinch on a selected control scales it (smaller/bigger). When the D-pad is
    // selected, twisting the two fingers additionally rotates it.
    private void UpdatePinchScale()
    {
        Touch[] touches = TouchSource.Touches;
        if (touches.Length < 2)
        {
            if (_pinchActive)
            {
                _pinchActive = false;
                // The drag gesture must re-arm after a pinch; re-baseline any pending drag so
                // leftover pinch movement doesn't yank the control once the fingers lift.
                _dragging = false;
                if (touches.Length == 1 && _dragArmed && touches[0].fingerId == _dragFingerId)
                    _dragStartPos = new Vector2(touches[0].position.x, Screen.height - touches[0].position.y);
            }
            return;
        }

        // Two fingers on screen: the pinch owns the input from now on. Cancel any in-flight or
        // armed drag immediately, otherwise the drag path (touch or synthesized mouse) would
        // keep running and yank the control toward the second finger's position.
        _dragging = false;

        float dist = Vector2.Distance(touches[0].position, touches[1].position);
        Vector2 fingerVec = touches[1].position - touches[0].position;
        float angle = Mathf.Atan2(fingerVec.y, fingerVec.x) * Mathf.Rad2Deg;

        if (!_pinchActive)
        {
            _pinchActive = true;
            _pinchStartDist = Mathf.Max(1f, dist);
            _pinchStartAngle = angle;
            _pinchBaseScale = string.IsNullOrEmpty(_selectedId)
                ? 1f
                : GetScale(TouchInput.Instance.GetLayoutHandles(), _selectedId);
            _pinchBaseRotation = (_selectedId == "dpad" && TouchInput.Exists && TouchInput.Instance.Dpad != null)
                ? TouchInput.Instance.Dpad.Rotation
                : 0f;
            return;
        }

        if (string.IsNullOrEmpty(_selectedId))
            return;

        float nextScale = Mathf.Clamp(_pinchBaseScale * (dist / _pinchStartDist), MobileControlLayout.MinScale, MobileControlLayout.MaxScale);
        MobileControlLayout.SetScale(_selectedId, nextScale);

        // Only the D-pad is rotatable; buttons keep their upright orientation.
        if (_selectedId == "dpad")
        {
            // Touch positions are y-up while the GUI rotation is y-down, so a clockwise finger
            // twist makes the touch-space angle DECREASE; negate the delta so the D-pad turns
            // the same way the fingers do.
            float delta = Mathf.DeltaAngle(_pinchStartAngle, angle);
            MobileControlLayout.SetRotation(_selectedId, _pinchBaseRotation - delta);
        }

        TouchInput.Instance.ApplyLayout();
    }

    // Selects the control under the given GUI-space position and returns true when one was hit.
    // Hit-testing is rotation-aware for the D-pad, so its rotated corners register correctly.
    private bool SelectControlAtGui(Vector2 guiPos)
    {
        if (!TouchInput.Exists) return false;
        if (_toolbarButtonsRect.Contains(guiPos)) return false; // the toolbar buttons own this area

        List<TouchInput.LayoutHandle> handles = TouchInput.Instance.GetLayoutHandles();
        for (int i = handles.Count - 1; i >= 0; i--) // top-most first
        {
            if (handles[i].Id == "dpad" && TouchInput.Instance.Dpad != null)
            {
                TouchDPad dp = TouchInput.Instance.Dpad;
                Vector2 pivot = new Vector2(handles[i].Boundary.x + 155f * dp.Scale, handles[i].Boundary.y + 103f * dp.Scale);
                if (handles[i].Boundary.Contains(RotatePoint(guiPos, pivot, -dp.Rotation)))
                {
                    _selectedId = handles[i].Id;
                    return true;
                }
            }
            else if (handles[i].Boundary.Contains(guiPos))
            {
                _selectedId = handles[i].Id;
                return true;
            }
        }
        return false;
    }

    private static Vector2 HandleCenter(string id)
    {
        if (!TouchInput.Exists) return Vector2.zero;
        List<TouchInput.LayoutHandle> handles = TouchInput.Instance.GetLayoutHandles();
        for (int i = 0; i < handles.Count; i++)
            if (handles[i].Id == id) return handles[i].Boundary.center;
        return Vector2.zero;
    }

    private static Vector2 RotatePoint(Vector2 p, Vector2 pivot, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cosR = Mathf.Cos(rad);
        float sinR = Mathf.Sin(rad);
        Vector2 d = p - pivot;
        return pivot + new Vector2(d.x * cosR - d.y * sinR, d.x * sinR + d.y * cosR);
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

        // The toolbar buttons (Switch mode / Reset / Cancel / Save) are drawn in MobileMenuScale
        // space, so compute their RAW pixel footprint: only this rect is reserved — controls can
        // be moved freely everywhere else, including the top corners next to the toolbar.
        float ms = MobileMenuScale.Scale;
        float bw = 140f;
        float bh = 34f;
        float totalW = 4f * bw + 3f * 8f;
        float x0 = (MobileMenuScale.VirtualWidth - totalW) / 2f;
        _toolbarButtonsRect = new Rect(x0 * ms, 2f * ms, totalW * ms, bh * ms);

        // Dim the world behind the editor so the markers stay legible no matter what the paused
        // game shows behind them (a bright sky used to swallow the blue outline of the top-right
        // weapon changer).
        Color prev = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.65f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _pixel);
        GUI.color = prev;

        // Draw each control: soft backdrop, icon, colored border and a name badge above it.
        foreach (TouchInput.LayoutHandle hd in handles)
        {
            bool selected = (hd.Id == _selectedId);
            bool isDpad = (hd.Id == "dpad");

            // Render the D-pad exactly like the game does: rotated around the cluster pivot
            // (TouchDPad.Draw uses GUIUtility.RotateAroundPivot(_rotation, _centerPosition)).
            // For the D-pad, rotate the draw matrix around the cluster pivot so BOTH the icon
            // and the rect marker follow its current rotation. The name badge stays upright.
            Matrix4x4 m = GUI.matrix;
            if (isDpad && TouchInput.Exists && TouchInput.Instance.Dpad != null)
            {
                TouchDPad dp = TouchInput.Instance.Dpad;
                Rect dr = dp.DpadBoundary;
                GUIUtility.RotateAroundPivot(dp.Rotation, new Vector2(dr.x + 155f * dp.Scale, dr.y + 103f * dp.Scale));
            }

            if (hd.Icon != null)
            {
                GUI.DrawTexture(hd.Boundary, hd.Icon, ScaleMode.StretchToFill, true);
            }

            Color border = selected ? new Color(1f, 0.85f, 0.2f) : new Color(0.2f, 0.5f, 1f, 1f);
            DrawOutline(hd.Boundary, border, selected ? 3f : 3f);

            GUI.matrix = m;

            // Name badge: a small pill above the button, always upright for readability.
            GUIContent name = new GUIContent(hd.DisplayName);
            Vector2 textSize = BlueStonez.label_interparkbold_11pt.CalcSize(name);
            Rect tagRect = new Rect(hd.Boundary.center.x - textSize.x / 2 - 8, hd.Boundary.y - 22, textSize.x + 16, 20);
            GUI.color = selected ? new Color(1f, 0.85f, 0.2f, 0.95f) : new Color(0.10f, 0.13f, 0.20f, 0.9f);
            GUI.DrawTexture(tagRect, _pixel);
            GUI.color = selected ? Color.black : Color.white;
            GUI.Label(tagRect, name, BlueStonez.label_interparkbold_11pt);
            GUI.color = Color.white;
        }

        // The joystick zone marker is drawn LAST so it stays visible on top of the corner
        // buttons it shares its corner with.
        DrawMovementPreview();

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
            DrawOutline(r, new Color(0.2f, 0.5f, 1f, 1f), 3f);
            GUI.color = new Color(0.10f, 0.13f, 0.20f, 0.9f);
            GUI.DrawTexture(new Rect(r.x + 4, r.y + 4, 110, 20), _pixel);
            GUI.color = Color.white;
            GUI.Label(new Rect(r.x + 4, r.y + 4, 110, 20), "Joystick area");
        }
    }

    private void HandleDrag(List<TouchInput.LayoutHandle> handles)
    {
        // On touch devices Unity also synthesizes mouse events from touches, but with different
        // timing than Input.touches — a second finger's tap can surface as MouseDown/MouseDrag
        // a frame before the touch list shows two fingers. The touch paths (UpdateTouchDrag /
        // UpdatePinchScale) own all input there, so ignore IMGUI mouse events while real
        // touches exist.
        if (Input.touchCount > 0) return;

        Event e = Event.current;
        Vector2 mouse = e.mousePosition;

        switch (e.type)
        {
            case EventType.MouseDown:
                if (e.button != 0) break; // only the left pointer selects/arms; the right button is the second pinch finger
                if (_pinchActive) break; // a pinch owns the input
                if (_toolbarButtonsRect.Contains(mouse)) break; // toolbar buttons own this click
                if (SelectControlAtGui(mouse))
                {
                    // Armed, not yet dragging — a second pointer can still start a pinch.
                    _dragArmed = true;
                    _dragging = false;
                    _dragStartPos = mouse;
                    _dragOffset = mouse - HandleCenter(_selectedId);
                    e.Use();
                }
                break;

            case EventType.MouseDrag:
                if (e.button != 0) break; // the second pointer never drags
                // On device the synthesized mouse follows the first pinch finger; don't let its
                // movement turn the gesture into a drag while a pinch is active.
                if (_pinchActive) { e.Use(); break; }

                if (_dragArmed && !_dragging && (mouse - _dragStartPos).sqrMagnitude > DragThresholdSqr)
                    _dragging = true;

                if (_dragging && !string.IsNullOrEmpty(_selectedId))
                {
                    Vector2 center = ClampDragCenter(mouse - _dragOffset);
                    float scale = GetScale(handles, _selectedId);
                    MobileControlLayout.SetPixels(_selectedId, center, scale);
                    TouchInput.Instance.ApplyLayout();
                    e.Use();
                }
                break;

            case EventType.MouseUp:
                if (e.button != 0) break; // only the left pointer owns the armed/drag state
                if (_dragging || _dragArmed)
                {
                    _dragging = false;
                    _dragArmed = false;
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
            _hintText = new MeshGUIText("Drag to move, pinch to resize, twist to rotate the D-Pad.", HudAssets.Instance.InterparkBitmapFont, TextAnchor.MiddleCenter);
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

    // Keeps the dragged control's full footprint on screen: the center can't come closer to an
    // edge than half the control's size, so a control can never be dragged off-screen and lost.
    // Movement is otherwise free — controls may go anywhere on screen, including up to the top.
    // For the rotated D-pad the clamp uses its axis-aligned footprint at the current degree.
    private Vector2 ClampDragCenter(Vector2 center)
    {
        float halfW = 24f;
        float halfH = 24f;
        if (!string.IsNullOrEmpty(_selectedId) && TouchInput.Exists)
        {
            List<TouchInput.LayoutHandle> handles = TouchInput.Instance.GetLayoutHandles();
            for (int i = 0; i < handles.Count; i++)
            {
                if (handles[i].Id == _selectedId)
                {
                    halfW = handles[i].Boundary.width * 0.5f;
                    halfH = handles[i].Boundary.height * 0.5f;

                    if (_selectedId == "dpad" && TouchInput.Instance.Dpad != null)
                    {
                        float rad = TouchInput.Instance.Dpad.Rotation * Mathf.Deg2Rad;
                        float cosR = Mathf.Abs(Mathf.Cos(rad));
                        float sinR = Mathf.Abs(Mathf.Sin(rad));
                        float w = handles[i].Boundary.width;
                        float h = handles[i].Boundary.height;
                        halfW = (w * cosR + h * sinR) * 0.5f;
                        halfH = (h * cosR + w * sinR) * 0.5f;
                    }
                    break;
                }
            }
        }

        float maxX = Mathf.Max(halfW, Screen.width - halfW);
        float maxY = Mathf.Max(halfH, Screen.height - halfH);
        center.x = Mathf.Clamp(center.x, Mathf.Min(halfW, maxX), maxX);
        center.y = Mathf.Clamp(center.y, Mathf.Min(halfH, maxY), maxY);
        return center;
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

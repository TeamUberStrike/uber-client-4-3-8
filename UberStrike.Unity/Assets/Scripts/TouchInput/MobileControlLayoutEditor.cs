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
    private Rect _toolbarRect;
    private Texture2D _pixel;

    private void Awake()
    {
        _pixel = Texture2D.whiteTexture;
    }

    private void Update()
    {
        // Freeze look/move while editing so dragging never leaks into gameplay.
        if (MobileControlLayout.EditMode)
        {
            TouchInput.WishDirection = Vector2.zero;
            TouchInput.WishLook = Vector2.zero;
        }
    }

    private void OnGUI()
    {
        if (!TouchInput.Exists) return;

        if (!MobileControlLayout.EditMode)
        {
            DrawOpenButton();
            return;
        }

        DrawEditor();
    }

    private void DrawOpenButton()
    {
        // Entry point: a small button shown during a match. (Can also be opened from Options later.)
        if (!GameState.HasCurrentGame) return;

        float w = 160, h = 32;
        Rect r = new Rect((Screen.width - w) * 0.5f, 6, w, h);
        if (GUI.Button(r, "Edit Controls"))
        {
            MobileControlLayout.EditMode = true;
            _selectedId = null;
            _dragging = false;
        }
    }

    private void DrawEditor()
    {
        List<TouchInput.LayoutHandle> handles = TouchInput.Instance.GetLayoutHandles();

        // Dim the world behind the editor.
        Color prev = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.5f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _pixel);
        GUI.color = prev;

        // Reserve a bottom toolbar; drags that start here are ignored (let buttons/slider work).
        float barH = 96;
        _toolbarRect = new Rect(0, Screen.height - barH, Screen.width, barH);

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

            GUI.Label(new Rect(hd.Boundary.x - 10, hd.Boundary.yMax + 2, Mathf.Max(hd.Boundary.width + 20, 90), 18), hd.DisplayName);
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
                    center.x = Mathf.Clamp(center.x, 0, Screen.width);
                    center.y = Mathf.Clamp(center.y, 0, Screen.height - 96);
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
        GUI.color = new Color(0.05f, 0.05f, 0.08f, 0.92f);
        GUI.DrawTexture(_toolbarRect, _pixel);
        GUI.color = Color.white;

        GUILayout.BeginArea(new Rect(8, _toolbarRect.y + 6, Screen.width - 16, 90));

        GUILayout.BeginHorizontal();
        GUILayout.Label(string.IsNullOrEmpty(_selectedId)
            ? "Drag a control to move it. Tap one to select, then scale below."
            : "Selected: " + DisplayName(handles, _selectedId));
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Reset All", GUILayout.Width(110), GUILayout.Height(30)))
        {
            MobileControlLayout.ResetAll();
            TouchInput.Instance.ApplyLayout();
            _selectedId = null;
        }
        if (GUILayout.Button("Done", GUILayout.Width(90), GUILayout.Height(30)))
        {
            MobileControlLayout.Save();
            MobileControlLayout.EditMode = false;
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (!string.IsNullOrEmpty(_selectedId))
        {
            GUILayout.Label("Size", GUILayout.Width(40));
            float cur = GetScale(handles, _selectedId);
            float next = GUILayout.HorizontalSlider(cur, 0.6f, 2.2f, GUILayout.Width(Mathf.Min(360, Screen.width - 160)));
            if (!Mathf.Approximately(next, cur))
            {
                MobileControlLayout.SetScale(_selectedId, next);
                TouchInput.Instance.ApplyLayout();
            }
            GUILayout.Label(cur.ToString("N2"), GUILayout.Width(50));
        }
        GUILayout.EndHorizontal();

        GUILayout.EndArea();
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

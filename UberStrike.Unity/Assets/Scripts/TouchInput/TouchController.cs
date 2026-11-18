using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Cmune.Util;
using System;

public class TouchController : Singleton<TouchController>
{
    public float GUIAlpha = 1;

    // Player opacity for on-screen controls; draw only, hit areas unchanged.
    public static float Opacity
    {
        get { return ApplicationDataManager.ApplicationOptions != null ? ApplicationDataManager.ApplicationOptions.TouchControlsOpacity : 1f; }
    }

    public static Color Tint { get { return new Color(1, 1, 1, Opacity); } }

    public Color DrawColor(float minAlpha)
    {
        return new Color(1, 1, 1, Mathf.Clamp(GUIAlpha, minAlpha, 1.0f) * Opacity);
    }

    private TouchController()
    {
        _controls = new List<TouchBaseControl>();
        UnityRuntime.Instance.OnUpdate += OnUpdate;
        UnityRuntime.Instance.OnGui += OnGui;
    }

    void OnUpdate()
    {
#if UNITY_EDITOR || UNITY_IOS || UNITY_IPHONE || UNITY_ANDROID || UNITY_WEBGL
        // While the layout editor is open it owns the screen: don't process live touch input
        // (so dragging a button to reposition it doesn't also fire/move).
        if (MobileControlLayout.EditMode) return;
#endif

        foreach (TouchBaseControl control in _controls)
        {
            if (!control.Enabled) continue;
            control.FirstUpdate();
            foreach (Touch touch in TouchSource.Touches)
            {
                control.UpdateTouches(touch);
            }
            control.FinalUpdate();
        }
    }

    void OnGui()
    {
#if UNITY_EDITOR || UNITY_IOS || UNITY_IPHONE || UNITY_ANDROID || UNITY_WEBGL
        // The layout editor draws the customizable controls itself while open.
        if (MobileControlLayout.EditMode) return;
#endif

        foreach (TouchBaseControl control in _controls)
        {
            if (!control.Enabled || !control.Visible) continue;
            GUI.color = Tint;
            control.Draw();
        }
        GUI.color = Color.white;
    }

    public void AddControl(TouchBaseControl control)
    {
        _controls.Add(control);
    }

    public void RemoveControl(TouchBaseControl control)
    {
        _controls.Remove(control);
    }

    List<TouchBaseControl> _controls;
}


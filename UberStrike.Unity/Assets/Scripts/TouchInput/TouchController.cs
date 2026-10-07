using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Cmune.Util;
using System;

public class TouchController : Singleton<TouchController>
{
    public float GUIAlpha = 1;

    private TouchController()
    {
        _controls = new List<TouchBaseControl>();
        UnityRuntime.Instance.OnUpdate += OnUpdate;
        UnityRuntime.Instance.OnGui += OnGui;
    }

    void OnUpdate()
    {
        // While the layout editor is open it owns the screen: don't process live touch input
        // (so dragging a button to reposition it doesn't also fire/move).
        if (MobileControlLayout.EditMode) return;

        foreach (TouchBaseControl control in _controls)
        {
            if (!control.Enabled || control.Removed) continue;
            control.FirstUpdate();
            foreach (Touch touch in Input.touches)
            {
                control.UpdateTouches(touch);
            }
            control.FinalUpdate();
        }
    }

    void OnGui()
    {
        // The layout editor draws the customizable controls itself while open.
        if (MobileControlLayout.EditMode) return;

        foreach (TouchBaseControl control in _controls)
        {
            if (!control.Enabled || control.Removed) continue;
            control.Draw();
        }
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


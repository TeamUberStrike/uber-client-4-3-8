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
        foreach (TouchBaseControl control in _controls)
        {
            if (!control.Enabled) continue;
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
        foreach (TouchBaseControl control in _controls)
        {
            if (!control.Enabled) continue;
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


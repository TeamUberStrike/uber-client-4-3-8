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
#if UNITY_EDITOR || UNITY_IOS || UNITY_IPHONE || UNITY_ANDROID || UNITY_WEBGL
        // While the layout editor is open it owns the screen: don't process live touch input
        // (so dragging a button to reposition it doesn't also fire/move).
        if (MobileControlLayout.EditMode) return;
        // The end-of-match screen owns it too: the player is paused there, and the pause, chat and scoreboard
        // buttons must not sit over the results (the pause button would close them).
        if (GamePageManager.IsCurrentPage(PageType.EndOfMatch))
        {
            // A scoreboard held as the match ended never sees its finger lift here, so release it.
            if (TouchInput.Exists) TouchInput.Instance.ReleaseScoreboardForEndOfMatch();
            return;
        }
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
        // No touch controls over the end-of-match screen (see OnUpdate).
        if (GamePageManager.IsCurrentPage(PageType.EndOfMatch)) return;
#endif

        foreach (TouchBaseControl control in _controls)
        {
            if (!control.Enabled || !control.Visible) continue;
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


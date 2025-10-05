
using UnityEngine;
using System;

public class GuiLockController : AutoMonoBehaviour<GuiLockController>
{
    void Awake()
    {
        enabled = false;

        Alpha = 0.6f;
    }

    public static void LockApplication()
    {
        IsApplicationLocked = true;
        LockingDepth = GuiDepth.Popup;
        Instance.enabled = true;
    }

    public static bool IsApplicationLocked { get; private set; }

    public static float Alpha { get; private set; }

    public static GuiDepth LockingDepth { get; private set; }

    public static bool IsEnabled { get; private set; }

    public static bool IsLocked(params GuiDepth[] levels)
    {
        if (IsEnabled)
        {
            return Array.Exists(levels, l => l == LockingDepth);
        }
        else
        {
            return false;
        }
    }

    public static void EnableLock(GuiDepth depth)
    {
        if (IsApplicationLocked) return;

        if (!IsEnabled || LockingDepth > depth)
        {
            LockingDepth = depth;

            IsEnabled = true;

            Instance.enabled = IsEnabled;
        }
    }

    public static void ReleaseLock(GuiDepth depth)
    {
        if (IsApplicationLocked) return;

        if (IsEnabled && LockingDepth == depth)
        {
            IsEnabled = false;

            Instance.enabled = IsEnabled;
        }
    }

    private void OnGUI()
    {
        GUI.depth = (int)LockingDepth + 1;

        //If the Ribbon is disabled, don't listen to any clicks
        if ((Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseUp))
        {
            Event.current.Use();
        }

        //Grey out the GLobal UI Ribbon
        GUI.color = new Color(1, 1, 1, Alpha);
        GUI.Button(new Rect(0, 0, Screen.width + 5, Screen.height + 5), string.Empty, BlueStonez.box_grey31);
        GUI.color = Color.white;
    }
}
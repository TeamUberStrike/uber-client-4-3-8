using System;
using System.Collections.Generic;
using UnityEngine;

public class PopupSystem : AutoMonoBehaviour<PopupSystem>
{
    #region Fields

    private GuiDepth _lastLockDepth = 0;
    private PopupStack<IPopupDialog> _popups = new PopupStack<IPopupDialog>();

    #endregion

    private void OnGUI()
    {
        ReleaseOldLock();

        if (_popups.Count > 0)
        {
            IPopupDialog popup = _popups.Peek();
            _lastLockDepth = popup.Depth;
            GUI.depth = (int)_lastLockDepth;
            popup.OnGUI();

            if (Event.current.type == EventType.layout)
            {
                GuiLockController.EnableLock(_lastLockDepth);
            }
        }

        GuiManager.DrawTooltip();
    }

    private void ReleaseOldLock()
    {
        if (Event.current.type == EventType.layout)
        {
            if (_popups.Count > 0)
            {
                if (_lastLockDepth != _popups.Peek().Depth)
                    GuiLockController.ReleaseLock(_lastLockDepth);
            }
            else
            {
                GuiLockController.ReleaseLock(_lastLockDepth);
                enabled = false;
            }
        }
    }

    public static void Show(IPopupDialog popup)
    {
        Instance._popups.Push(popup);
        Instance.enabled = true;
    }

    public static void ShowMessage(string title, string text, AlertType flag, Action ok)
    {
        ShowMessage(title, text, flag, ok, null);
    }

    public static void ShowMessage(string title, string text, AlertType flag, Action ok, Action cancel)
    {
        Show(new GeneralPopupDialog(title, text, flag, ok, cancel));
    }

    public static IPopupDialog ShowMessage(string title, string text, AlertType flag, Action ok, string okCaption, Action cancel, string cancelCaption, ActionType type)
    {
        IPopupDialog dialog = new GeneralPopupDialog(title, text, flag, ok, okCaption, cancel, cancelCaption, type);
        Show(dialog);
        return dialog;
    }

    public static IPopupDialog ShowMessage(string title, string text, AlertType flag, Action ok, string okCaption, Action cancel, string cancelCaption)
    {
        IPopupDialog dialog = new GeneralPopupDialog(title, text, flag, ok, okCaption, cancel, cancelCaption, ActionType.None);
        Show(dialog);
        return dialog;
    }

    public static IPopupDialog ShowMessage(string title, string text, AlertType flag, string okCaption, Action ok)
    {
        IPopupDialog dialog = new GeneralPopupDialog(title, text, flag, ok, okCaption);
        Show(dialog);
        return dialog;
    }

    public static ProgressPopupDialog ShowProgress(string title, string text, ProgressPopupDialog.Progress progress = null)
    {
        ProgressPopupDialog dialog = new ProgressPopupDialog(title, text, progress);
        Show(dialog);
        return dialog;
    }

    public static IPopupDialog ShowItems(string title, string text, List<IUnityItem> items)
    {
        IPopupDialog dialog = new ItemListPopupDialog(title, text, items);
        Show(dialog);
        return dialog;
    }

    public static IPopupDialog ShowItem(IUnityItem item)
    {
        IPopupDialog dialog = new ItemListPopupDialog(item);
        Show(dialog);
        return dialog;
    }

    public static IPopupDialog ShowMessage(string title, string text)
    {
        IPopupDialog dialog = new GeneralPopupDialog(title, text, AlertType.OK);
        Show(dialog);
        return dialog;
    }

    public static IPopupDialog ShowMessage(string title, string text, AlertType flag)
    {
        IPopupDialog dialog = new GeneralPopupDialog(title, text, flag);
        Show(dialog);
        return dialog;
    }

    public static void HideMessage(IPopupDialog dialog)
    {
        if (dialog != null)
        {
            Instance._popups.Remove(dialog);
            dialog.OnHide();
        }
    }

    public static bool IsAnyPopupOpen
    {
        get { return Instance._popups.Count > 0; }
    }

    public static void ClearAll()
    {
        Instance._popups.CLear();
    }

    private static bool IsCurrentPopup(IPopupDialog dialog)
    {
        return (Instance._popups.Count > 0 && Instance._popups.Peek() == dialog);
    }

    public static string CurrentPopupName
    {
        get { return (Instance._popups.Count > 0) ? Instance._popups.Peek().ToString() : string.Empty; }
    }

    public enum AlertType
    {
        OK = 0,
        OKCancel = 1,
        Cancel = 2,
        None = 3,
    }

    public enum ActionType
    {
        None,
        Negative,
        Positive,
    }
}
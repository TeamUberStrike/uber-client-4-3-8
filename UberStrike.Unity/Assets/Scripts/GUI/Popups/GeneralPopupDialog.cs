using System;
using UnityEngine;

/// <summary>
/// A PopupDialog is a window to display some information.
/// </summary>
public class GeneralPopupDialog : BasePopupDialog
{
    public GeneralPopupDialog(string title, string text, PopupSystem.AlertType flag, Action ok, string okCaption, Action cancel, string cancelCaption, PopupSystem.ActionType actionType)
    {
        Text = text;
        Title = title;

        _alertType = flag;
        _actionType = actionType;

        _callbackOk = ok;
        _callbackCancel = cancel;

        _okCaption = okCaption;
        _cancelCaption = cancelCaption;
    }

    public GeneralPopupDialog(string title, string text)
        : this(title, text, PopupSystem.AlertType.None, null, string.Empty, null, string.Empty, PopupSystem.ActionType.None)
    { }

    public GeneralPopupDialog(string title, string text, PopupSystem.AlertType flag)
        : this(title, text, flag, null, string.Empty, null, string.Empty, PopupSystem.ActionType.None)
    { }

    public GeneralPopupDialog(string title, string text, PopupSystem.AlertType flag, Action ok, string okCaption)
        : this(title, text, flag, ok, okCaption, null, string.Empty, PopupSystem.ActionType.None)
    { }

    public GeneralPopupDialog(string title, string text, PopupSystem.AlertType flag, Action action)
        : this(title, text, flag, action, string.Empty, null, string.Empty, PopupSystem.ActionType.None)
    { }

    public GeneralPopupDialog(string title, string text, PopupSystem.AlertType flag, Action ok, Action cancel)
        : this(title, text, flag, ok, string.Empty, cancel, string.Empty, PopupSystem.ActionType.None)
    { }

    protected override void DrawPopupWindow()
    {
        GUI.Label(new Rect(17, 55, _size.x - 34, _size.y - 100), Text, BlueStonez.label_interparkbold_13pt);
    }
}
using System;
using UnityEngine;

/// <summary>
/// 
/// </summary>
public abstract class BasePopupDialog : IPopupDialog
{
    public string Text { get; set; }
    public string Title { get; set; }

    protected Vector2 _size = new Vector2(320, 240);
    protected PopupSystem.ActionType _actionType = PopupSystem.ActionType.None;
    protected PopupSystem.AlertType _alertType;
    protected string _okCaption = string.Empty;
    protected string _cancelCaption = string.Empty;
    protected Action _callbackOk;
    protected Action _callbackCancel;
    protected Action _onGUIAction;

    public virtual void OnHide() { }

    public void SetAlertType(PopupSystem.AlertType type)
    {
        _alertType = type;
    }

    public void OnGUI()
    {
        Rect rect = new Rect((GUITools.ScreenWidth - _size.x) * 0.5f, (GUITools.ScreenHeight - _size.y - 56) * 0.5f, _size.x, _size.y);
        GUI.BeginGroup(rect, GUIContent.none, BlueStonez.window_standard_grey38);
        {
            GUI.Label(new Rect(0, 0, _size.x, 56), Title, BlueStonez.tab_strip);

            DrawPopupWindow();

            switch (_alertType)
            {
                case PopupSystem.AlertType.OK:
                    DoOKButton();
                    break;
                case PopupSystem.AlertType.Cancel:
                    DoCancelButton();
                    break;
                case PopupSystem.AlertType.OKCancel:
                    DoOKCancelButtons();
                    break;
            }
        }
        GUI.EndGroup();
    }

    protected abstract void DrawPopupWindow();

    public GuiDepth Depth { get { return GuiDepth.Popup; } }

    private void DoOKButton()
    {
        GUIStyle style = BlueStonez.button;
        switch (_actionType)
        {
            case PopupSystem.ActionType.Negative: style = BlueStonez.button_red; break;
            case PopupSystem.ActionType.Positive: style = BlueStonez.button_green; break;
        }

        Rect rect = new Rect((_size.x - 120) * 0.5f, _size.y - 40, 120, 32);
        GUIContent content = new GUIContent(string.IsNullOrEmpty(_okCaption) ? LocalizedStrings.OkCaps : _okCaption);

        if (GUITools.Button(rect, content, style))
        {
            PopupSystem.HideMessage(this);

            if (_callbackOk != null) _callbackOk();
        }
    }

    private void DoOKCancelButtons()
    {
        GUIStyle style = BlueStonez.button;

        //CANCEL button
        Rect rect = new Rect(_size.x * 0.5f + 5, _size.y - 40, 120, 32);
        GUIContent content = new GUIContent(string.IsNullOrEmpty(_cancelCaption) ? LocalizedStrings.CancelCaps : _cancelCaption);

        GUI.color = Color.white;
        if (GUITools.Button(rect, content, style))
        {
            PopupSystem.HideMessage(this);

            if (_callbackCancel != null) _callbackCancel();
        }

        //OK button
        switch (_actionType)
        {
            case PopupSystem.ActionType.Negative: style = BlueStonez.button_red; break;
            case PopupSystem.ActionType.Positive: style = BlueStonez.button_green; break;
        }

        rect = new Rect(_size.x * 0.5f - 125, _size.y - 40, 120, 32);
        content = new GUIContent(string.IsNullOrEmpty(_okCaption) ? LocalizedStrings.OkCaps : _okCaption);

        if (GUITools.Button(rect, content, style))
        {
            PopupSystem.HideMessage(this);

            if (_callbackOk != null) _callbackOk();
        }
    }

    private void DoCancelButton()
    {
        GUIStyle style = BlueStonez.button;
        switch (_actionType)
        {
            case PopupSystem.ActionType.Negative: style = BlueStonez.button_red; break;
            case PopupSystem.ActionType.Positive: style = BlueStonez.button_green; break;
        }

        Rect rect = new Rect((_size.x - 120) * 0.5f, _size.y - 40, 120, 32);
        GUIContent content = new GUIContent(string.IsNullOrEmpty(_cancelCaption) ? LocalizedStrings.CancelCaps : _cancelCaption);

        if (GUITools.Button(rect, content, style))
        {
            PopupSystem.HideMessage(this);

            if (_callbackCancel != null) _callbackCancel();
        }
    }
}
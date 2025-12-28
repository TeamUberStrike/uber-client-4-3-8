using System;
using UnityEngine;
using Cmune.DataCenter.Common.Entities;

/// <summary>
/// 
/// </summary>
public class ProgressPopupDialog : GeneralPopupDialog
{
    private Progress _progress;

    public float ManualProgress { get; set; }
    public bool ForceWaitingTexture { get; set; }

    public ProgressPopupDialog(string title, string text, Progress progress = null, bool forceWaitingTexture = false)
        : base(title, text)
    {
        _progress = progress;
        ForceWaitingTexture = forceWaitingTexture;
    }

    protected override void DrawPopupWindow()
    {
        GUI.Label(new Rect(17, 95, _size.x - 34, 32), Text, BlueStonez.label_interparkbold_11pt);
        if (ApplicationDataManager.IsMobile || ForceWaitingTexture)
        {
            WaitingTexture.Draw(new Vector2(_size.x / 2, 160));
        }
        else
        {
            if (_progress != null)
            {
                DrawLevelBar(new Rect(17, 125, _size.x - 34, 16), _progress(), ColorScheme.ProgressBar);
            }
            else
            {
                DrawLevelBar(new Rect(17, 125, _size.x - 34, 16), ManualProgress, ColorScheme.ProgressBar);
            }
        }
    }

    private void DrawLevelBar(Rect position, float amount, Color barColor)
    {
        GUI.BeginGroup(position);
        {
            GUI.Label(new Rect(0, 0, position.width, 12), GUIContent.none, BlueStonez.progressbar_background);
            GUI.color = barColor;
            GUI.Label(new Rect(2, 2, (float)(position.width - 4) * Mathf.Clamp01(amount), 8), string.Empty, BlueStonez.progressbar_thumb);
            GUI.color = Color.white;
        }
        GUI.EndGroup();
    }

    public void ShowCancelButton(Action action)
    {
        _callbackCancel = action;
        _cancelCaption = LocalizedStrings.Cancel;
        _alertType = PopupSystem.AlertType.Cancel;
        _actionType = PopupSystem.ActionType.None;
    }

    public delegate float Progress();
}
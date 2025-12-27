using System;
using UnityEngine;

/// <summary>
/// 
/// </summary>
public class ProgressPopupDialog : GeneralPopupDialog
{
    private Progress _progress;

    public float ManualProgress { get; set; }

    public ProgressPopupDialog(string title, string text, Progress progress = null)
        : base(title, text)
    {
        _progress = progress;
    }

    protected override void DrawPopupWindow()
    {
        // Use safe style access with fallback
        GUIStyle textStyle = BlueStonez.label_interparkbold_11pt ?? GUI.skin.label;
        
        GUI.Label(new Rect(17, 95, _size.x - 34, 32), Text, textStyle);

        if (_progress != null)
        {
            DrawLevelBar(new Rect(17, 125, _size.x - 34, 16), _progress(), ColorScheme.ProgressBar);
        }
        else
        {
            DrawLevelBar(new Rect(17, 125, _size.x - 34, 16), ManualProgress, ColorScheme.ProgressBar);
        }
    }

    private void DrawLevelBar(Rect position, float amount, Color barColor)
    {
        GUI.BeginGroup(position);
        {
            // Store original color
            Color originalColor = GUI.color;
            
            // Draw background with dark gray color
            GUI.color = new Color(0.3f, 0.3f, 0.3f, 1f); // Dark background
            GUI.Label(new Rect(0, 0, position.width, 12), GUIContent.none, BlueStonez.SafeProgressBarBackground);
            
            // Draw progress thumb with specified color
            GUI.color = barColor;
            GUI.Label(new Rect(2, 2, (float)(position.width - 4) * Mathf.Clamp01(amount), 8), string.Empty, BlueStonez.SafeProgressBarThumb);
            
            // Restore original color
            GUI.color = originalColor;
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
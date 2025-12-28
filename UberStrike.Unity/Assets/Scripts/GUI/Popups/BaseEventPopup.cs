using System;
using UnityEngine;

public abstract class BaseEventPopup : IPopupDialog
{
    private const float BerpSpeed = 2.5f;

    protected int Width = 650;
    protected int Height = 330;
    protected bool ClickAnywhereToExit = true;
    private float _startTime = 0;

    protected Action _onCloseButtonClicked;

    public string Text { get; set; }

    public string Title { get; set; }

    public GuiDepth Depth { get { return GuiDepth.Event; } }

    public float Scale
    {
        get
        {
            if (_startTime > (Time.time - 1))
            {
                return Mathfx.Berp(0.01f, 1, (Time.time - _startTime) * BerpSpeed);
            }
            else
            {
                return 1;
            }
        }
    }

    /// <summary>
    /// Must be implemented for every Event Popup and is called within OnGUI
    /// </summary>
    /// <param name="rect"></param>
    protected abstract void DrawGUI(Rect rect);

    /// <summary>
    /// Called after the popup sytem removed the popup
    /// </summary>
    public virtual void OnHide() { }

    public void OnGUI()
    {
        //on the first draw call we set the start time
        if (_startTime == 0) _startTime = Time.time;

        GUI.color = Color.white.SetAlpha(Scale);
        float offsetX = (Screen.width - Width) * 0.5f;
        float offsetY = GlobalUIRibbon.HEIGHT + (Screen.height - GlobalUIRibbon.HEIGHT - Height) * 0.5f;
        Rect rect = new Rect(offsetX, offsetY, Width, 64 + Height - (64 * Scale));

        GUI.Label(new Rect(0, 0, 200, 20), offsetX + " " + offsetY);
        GUI.Box(new Rect(offsetX - 1, offsetY - 1, rect.width + 2, rect.height + 2), GUIContent.none, BlueStonez.window);
        GUI.BeginGroup(rect);
        {
            //always guarantee the exit through 'x'
            if (GUI.Button(new Rect(rect.width - 20, 0, 20, 20), "X", BlueStonez.friends_hidden_button))
            {
                Close();
            }

            //draw content
            DrawGUI(rect);
        }
        GUI.EndGroup();
        GUI.color = Color.white;

        //if clicked anything that is not a button, we exit
        if (ClickAnywhereToExit && Event.current.type == EventType.mouseDown && !rect.Contains(Event.current.mousePosition))
        {
            Event.current.Use();
            Close();
        }

        OnAfterGUI();
    }

    public virtual void OnAfterGUI() { }

    private void Close()
    {
        PopupSystem.HideMessage(this);

        if (_onCloseButtonClicked != null)
        {
            _onCloseButtonClicked();
        }
    }
}
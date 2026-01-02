using System;
using UnityEngine;

public abstract class LotteryPopupDialog : IPopupDialog
{
    protected enum State
    {
        Normal,
        Rolled,
    }

    private const float BerpSpeed = 2.5f;

    protected int Width = 650;
    protected int Height = 330;
    protected bool ClickAnywhereToExit = true;

    protected State _state = State.Normal;
    protected Action _onLotteryRolled;
    protected Action _onLotteryReturned;
    protected bool _showExitButton = true;

    public string Text { get; set; }

    public string Title { get; set; }

    public bool IsLotteryReturned { get; protected set; }

    public bool IsVisible { get; set; }

    public bool IsUIDisabled { get; set; }

    public bool IsWaiting { get; set; }

    public GuiDepth Depth
    {
        get { return GuiDepth.Event; }
    }

    public void OnGUI()
    {
        Rect rect = GetPosition();

        GUI.Box(rect, GUIContent.none, BlueStonez.window);

        GUITools.PushGUIState();
        GUI.enabled = !IsUIDisabled;

        GUI.BeginGroup(rect);
        {
#if !UNITY_ANDROID && !UNITY_IPHONE
            if (_showExitButton && GUI.Button(new Rect(rect.width - 20, 0, 20, 20), "X", BlueStonez.friends_hidden_button))
#else
            if (_showExitButton && GUI.Button(new Rect(rect.width - 45, 0, 45, 45), "X", BlueStonez.friends_hidden_button))
#endif
            {
                PopupSystem.HideMessage(this);
                LotteryAudioPlayer.Instance.Stop();
                BackgroundMusicPlayer.Instance.Play();
            }

            DrawPlayGUI(rect);
        }
        GUI.EndGroup();
        GUITools.PopGUIState();

        if (IsWaiting)
        {
            WaitingTexture.Draw(rect.center);
        }

        //if clicked anything that is not a button, we exit
        if (ClickAnywhereToExit && Event.current.type == EventType.mouseDown && !rect.Contains(Event.current.mousePosition))//GUI.Button(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none, GUIStyle.none))
        {
            ClosePopup();
            Event.current.Use();
        }

        OnAfterGUI();
    }

    public virtual void OnAfterGUI()
    { }

    public virtual void OnHide()
    { }

    public void SetRollCallback(Action onLotteryRolled)
    {
        _onLotteryRolled = onLotteryRolled;
    }

    public void SetLotteryReturnedCallback(Action onLotteryReturned)
    {
        _onLotteryReturned = onLotteryReturned;
    }

    public abstract LotteryWinningPopup ShowReward();

    protected abstract void DrawPlayGUI(Rect rect);

    protected void DrawNaviArrows(Rect rect, LotteryShopItem item)
    {
        if (GUI.Button(new Rect(rect.width * 0.5f - 95, rect.height - 42, 20, 20), GUIContent.none, BlueStonez.button_left))
        {
            PopupSystem.HideMessage(this);
            LotteryManager.Instance.ShowPreviousItem(item);
            SfxManager.Play2dAudioClip(SoundEffectType.UIButtonClick);
        }
        if (GUI.Button(new Rect(rect.width * 0.5f + 75, rect.height - 42, 20, 20), GUIContent.none, BlueStonez.button_right))
        {
            PopupSystem.HideMessage(this);
            LotteryManager.Instance.ShowNextItem(item);
            SfxManager.Play2dAudioClip(SoundEffectType.UIButtonClick);
        }
    }

    protected void ClosePopup()
    {
        PopupSystem.HideMessage(this);
    }

    private Rect GetPosition()
    {
        float offsetX = (Screen.width - Width) * 0.5f;
        float offsetY = GlobalUIRibbon.HEIGHT + (Screen.height - GlobalUIRibbon.HEIGHT - Height) * 0.5f;

        return new Rect(offsetX, offsetY, Width, Height);
    }
}
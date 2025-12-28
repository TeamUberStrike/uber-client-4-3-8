using Cmune.Util;
using UberStrike.Realtime.Common;
using UnityEngine;

public class PlayerStateMsgHud : Singleton<PlayerStateMsgHud>
{
    public delegate void OnButtonClickedDelegate();

    public float PermanentMsgHeight
    {
        get { return Screen.height * 0.05f; }
    }

    public float TemporaryMsgHeight
    {
        get { return Screen.height * 0.08f; }
    }

    public Vector2 PermanentMsgPosition
    {
        get
        {
            return new Vector2(Screen.width / 2,
                Screen.height * 0.42f + Screen.height * 0.58f * (1 - CameraRectController.Instance.Width));
        }
    }

    public Vector2 TemporaryMsgPosition
    {
        get
        {
            return new Vector2(Screen.width / 2,
                Screen.height * 0.5f + Screen.height * 0.6f * (1 - CameraRectController.Instance.Width));
        }
    }

    public bool TemporaryMsgEnabled
    {
        get { return _temporaryMsgText.IsVisible; }
        set
        {
            if (value) _temporaryMsgText.Show();
            else _temporaryMsgText.Hide();
        }
    }

    public bool PermanentMsgEnabled
    {
        get { return _permanentMsgText.IsVisible; }
        set
        {
            if (value) _permanentMsgText.Show();
            else _permanentMsgText.Hide();
        }
    }

    public OnButtonClickedDelegate OnButtonClicked { get; set; }

    public bool ButtonEnabled { get; set; }

    public string ButtonCaption { get; set; }

    public void Draw()
    {
        if (ButtonEnabled)
        {
#if !UNITY_ANDROID && !UNITY_IPHONE
            if (GameState.CurrentSpace != null && GUITools.Button(new Rect(Screen.width * CameraRectController.Instance.Width * 0.5f - 100,
                Screen.height * 0.5f + Screen.height * 0.6f * (1 - CameraRectController.Instance.Width), 200, 50),
#else
            if (GameState.CurrentSpace != null && GUITools.Button(new Rect(Screen.width * CameraRectController.Instance.Width * 0.5f - 150,
                Screen.height * 0.5f + Screen.height * 0.6f * (1 - CameraRectController.Instance.Width), 300, 75),
#endif
                    new GUIContent(ButtonCaption), _buttonGuiStyle))
            {
                if (OnButtonClicked != null)
                {
                    OnButtonClicked();
                }
            }
        }
    }

    public void DisplayNone()
    {
        _temporaryMsgText.Text = "";
        _permanentMsgText.Text = "";
    }

    public void DisplayRespawnTimeMsg(int remainingSeconds)
    {
        _temporaryMsgText.Text = LocalizedStrings.Respawn + ": " + remainingSeconds;
        _temporaryMsgText.Color = Color.white;
        _temporaryMsgText.Position = TemporaryMsgPosition;
        ButtonEnabled = false;
    }

    public void DisplayClickToRespawnMsg()
    {
        if (ApplicationDataManager.IsMobile)
        {
            _temporaryMsgText.Text = LocalizedStrings.TapToRespawn;
        }
        else
        {
            _temporaryMsgText.Text = LocalizedStrings.ClickToRespawn;
        }
        _temporaryMsgText.Color = Color.white;
        _temporaryMsgText.Position = TemporaryMsgPosition;
        ButtonEnabled = false;
    }

    public void DisplayDisconnectionTimeoutMsg(int remainingSeconds)
    {
        _temporaryMsgText.Show();
        _temporaryMsgText.Text = LocalizedStrings.DisconnectionIn + " " + remainingSeconds;
        _temporaryMsgText.Color = Color.red;
        _temporaryMsgText.Position = TemporaryMsgPosition;
    }

    public void DisplayWaitingForOtherPlayerMsg()
    {
        _permanentMsgText.Text = LocalizedStrings.WaitingForOtherPlayers;
        _permanentMsgText.Color = Color.white;
        _permanentMsgText.Position = PermanentMsgPosition;
    }

    public void DisplaySpectatorFollowingMsg(CharacterInfo info)
    {
        string playerName = info == null ? LocalizedStrings.Nobody : info.PlayerName;
        _permanentMsgText.Text = string.Format("{0} {1}", LocalizedStrings.Following, playerName);
        _permanentMsgText.Color = Color.white;
        _permanentMsgText.Position = PermanentMsgPosition;
    }

    public void DisplaySpectatorModeMsg()
    {
        _permanentMsgText.Text = LocalizedStrings.SpectatorMode;
        _permanentMsgText.Color = Color.white;
        _permanentMsgText.Position = PermanentMsgPosition;
    }

    #region Private
    private PlayerStateMsgHud()
    {
        _temporaryMsgText = new MeshGUIText("", HudAssets.Instance.InterparkBitmapFont, TextAnchor.MiddleCenter);
        _permanentMsgText = new MeshGUIText("", HudAssets.Instance.InterparkBitmapFont, TextAnchor.MiddleCenter);

        ResetHud();
        TemporaryMsgEnabled = true;
        PermanentMsgEnabled = true;

        CmuneEventHandler.AddListener<OnSetPlayerTeamEvent>(OnTeamChange);
        CmuneEventHandler.AddListener<CameraWidthChangeEvent>(OnCameraRectChange);
    }

    private void ResetHud()
    {
        ResetStyle();
        ResetTransform();
    }

    private void ResetStyle()
    {
        HudStyleUtility.Instance.SetNoShadowStyle(_temporaryMsgText);
        _temporaryMsgText.Color = Color.white;
        _temporaryMsgText.ShadowColorAnim.Alpha = 0.0f;
        HudStyleUtility.Instance.SetNoShadowStyle(_permanentMsgText);
        _permanentMsgText.Color = Color.white;
        _permanentMsgText.ShadowColorAnim.Alpha = 0.0f;
        _buttonGuiStyle = StormFront.ButtonBlue;
    }

    private void OnTeamChange(OnSetPlayerTeamEvent ev)
    {
        switch (ev.TeamId)
        {
            case TeamID.BLUE:
            case TeamID.NONE:
                _buttonGuiStyle = StormFront.ButtonBlue;
                break;
            case TeamID.RED:
                _buttonGuiStyle = StormFront.ButtonRed;
                break;
        }
    }

    private void OnCameraRectChange(CameraWidthChangeEvent ev)
    {
        ResetTransform();
    }

    private void ResetTransform()
    {
        float tmpMsgScale = TemporaryMsgHeight / _temporaryMsgText.TextBounds.y;
        _temporaryMsgText.Scale = new Vector2(tmpMsgScale, tmpMsgScale);
        _temporaryMsgText.Position = TemporaryMsgPosition;
        float pmtMsgScale = PermanentMsgHeight / _temporaryMsgText.TextBounds.y;
        _permanentMsgText.Scale = new Vector2(pmtMsgScale, pmtMsgScale);
        _permanentMsgText.Position = PermanentMsgPosition;
    }

    private MeshGUIText _temporaryMsgText;
    private MeshGUIText _permanentMsgText;
    private GUIStyle _buttonGuiStyle;

    #endregion
}

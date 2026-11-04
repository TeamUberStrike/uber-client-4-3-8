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

    public Vector2 KillerMsgPosition
    {
        get { return new Vector2(Screen.width / 2, Screen.height * 0.8f); }
    }

    public bool TemporaryMsgEnabled
    {
        get { return _temporaryMsgText.IsVisible; }
        set
        {
            if (value) { _temporaryMsgText.Show(); _killerMsgText.Show(); _killerHpText.Show(); _killerApText.Show(); }
            else { _temporaryMsgText.Hide(); _killerMsgText.Hide(); _killerHpText.Hide(); _killerApText.Hide(); }
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
        if (GameState.HasCurrentGame && GameState.LocalPlayer.IsGamePaused &&
            !PopupSystem.IsAnyPopupOpen && GUITools.Button(
                new Rect(Screen.width * CameraRectController.Instance.Width - 240, Screen.height - 150, 220, 50),
                new GUIContent("Chat"), StormFront.ButtonBlue))
        {
            SceneGuiController.OpenChat();
        }
        if (ButtonEnabled)
        {
            if (GameState.CurrentSpace != null && GUITools.Button(new Rect(Screen.width * CameraRectController.Instance.Width * 0.5f - 100,
                Screen.height * 0.5f + Screen.height * 0.6f * (1 - CameraRectController.Instance.Width), 200, 50),
                    new GUIContent(ButtonCaption), _buttonGuiStyle))
            {
                if (OnButtonClicked != null)
                {
                    OnButtonClicked();
                }
            }
        }
        // killer HP/AP icons (4.7.1 HUD sprites) next to the numbers
        if (_killerHp.Length > 0 && _killerMsgText.IsVisible && _killerMsgText.Text.Length > 0)
        {
            if (_hpIcon == null) _hpIcon = Resources.Load<Texture2D>("KillerHud/HealthIcon");
            if (_apIcon == null) _apIcon = Resources.Load<Texture2D>("KillerHud/ArmorIcon");
            Color previous = GUI.color;
            GUI.color = KillerHpColor;
            if (_hpIcon != null) GUI.DrawTexture(_hpIconRect, _hpIcon);
            GUI.color = KillerApColor;
            if (_apIcon != null) GUI.DrawTexture(_apIconRect, _apIcon);
            GUI.color = previous;
        }
    }

    // Respawn button replaces the countdown text; the killer lines stay until the player respawns.
    public void HideRespawnText()
    {
        _temporaryMsgText.Hide();
    }

    public void DisplayNone()
    {
        _temporaryMsgText.Text = "";
        _permanentMsgText.Text = "";
        _killerMsgText.Text = "";
        _killerHpText.Text = "";
        _killerApText.Text = "";
    }

    public void SetKiller(UberStrike.Realtime.Common.CharacterInfo killer)
    {
        if (killer == null)
        {
            ClearKiller();
            return;
        }
        _killerStats = string.Format(LocalizedStrings.NKilledYou, killer.PlayerName);
        _killerHp = Mathf.Max(0, (int)killer.Health).ToString();
        _killerAp = killer.Armor.ArmorPoints.ToString();
    }

    private void ClearKiller()
    {
        _killerStats = "";
        _killerHp = "";
        _killerAp = "";
        _killerMsgText.Text = "";
        _killerHpText.Text = "";
        _killerApText.Text = "";
    }

    public void DisplayRespawnTimeMsg(int remainingSeconds)
    {
        _temporaryMsgText.Text = LocalizedStrings.Respawn + ": " + remainingSeconds;
        _temporaryMsgText.Color = Color.white;
        _temporaryMsgText.Position = TemporaryMsgPosition;
        DisplayKillerMsg();
        ButtonEnabled = false;
    }

    public void DisplayClickToRespawnMsg()
    {
        _temporaryMsgText.Text = LocalizedStrings.ClickToRespawn;
        _temporaryMsgText.Color = Color.white;
        _temporaryMsgText.Position = TemporaryMsgPosition;
        DisplayKillerMsg();
        ButtonEnabled = false;
    }

    // "Aries killed you", below it: [HP icon] 45   [AP icon] 20
    private void DisplayKillerMsg()
    {
        _killerMsgText.Text = _killerStats;
        _killerMsgText.Color = Color.white;
        _killerMsgText.Position = KillerMsgPosition;
        _killerHpText.Text = _killerHp;
        _killerApText.Text = _killerAp;
        _killerHpText.Color = KillerHpColor;
        _killerApText.Color = KillerApColor;
        LayoutKillerStats();
    }

    // HP in the turquoise of the health pickups (HealthPickups.psd), AP in the orange of the armor pickups.
    private static readonly Color KillerHpColor = ColorConverter.RgbToColor(76, 212, 185);
    private static readonly Color KillerApColor = ColorConverter.RgbToColor(255, 128, 34);

    // Stats line size and spacing match the 4.7.1 (Steam) killed-by panel: numbers 0.6x the name line.
    private const float KillerStatsScale = 0.6f;

    private void LayoutKillerStats()
    {
        float icon = PermanentMsgHeight * 0.54f;
        float gap = icon * 0.3f;
        float groupGap = icon * 1.05f;
        float total = icon + gap + _killerHpText.Size.x + groupGap + icon + gap + _killerApText.Size.x;
        Vector2 line = KillerMsgPosition + new Vector2(0, PermanentMsgHeight * 1.6f);
        float x = line.x - total * 0.5f;
        _hpIconRect = new Rect(x, line.y - icon * 0.5f, icon, icon);
        _killerHpText.Position = new Vector2(x + icon + gap, line.y);
        x += icon + gap + _killerHpText.Size.x + groupGap;
        _apIconRect = new Rect(x, line.y - icon * 0.5f, icon, icon);
        _killerApText.Position = new Vector2(x + icon + gap, line.y);
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

    public void DisplaySpectatorFollowingMsg(UberStrike.Realtime.Common.CharacterInfo info)
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
        _killerMsgText = new MeshGUIText("", HudAssets.Instance.InterparkBitmapFont, TextAnchor.MiddleCenter);
        _killerHpText = new MeshGUIText("", HudAssets.Instance.InterparkBitmapFont, TextAnchor.MiddleLeft);
        _killerApText = new MeshGUIText("", HudAssets.Instance.InterparkBitmapFont, TextAnchor.MiddleLeft);

        ResetHud();
        TemporaryMsgEnabled = true;
        PermanentMsgEnabled = true;

        CmuneEventHandler.AddListener<OnPlayerRespawnEvent>(ev => ClearKiller());
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
        HudStyleUtility.Instance.SetNoShadowStyle(_killerMsgText);
        _killerMsgText.Color = Color.white;
        _killerMsgText.ShadowColorAnim.Alpha = 0.0f;
        foreach (MeshGUIText t in new MeshGUIText[] { _killerHpText, _killerApText })
        {
            HudStyleUtility.Instance.SetNoShadowStyle(t);
            t.Color = Color.white;
            t.ShadowColorAnim.Alpha = 0.0f;
        }
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
        _killerMsgText.Scale = new Vector2(pmtMsgScale, pmtMsgScale);
        _killerMsgText.Position = KillerMsgPosition;
        _killerHpText.Scale = new Vector2(pmtMsgScale, pmtMsgScale) * KillerStatsScale;
        _killerApText.Scale = new Vector2(pmtMsgScale, pmtMsgScale) * KillerStatsScale;
        LayoutKillerStats();
    }

    private MeshGUIText _temporaryMsgText;
    private MeshGUIText _permanentMsgText;
    private MeshGUIText _killerMsgText;
    private string _killerStats = "";
    private MeshGUIText _killerHpText;
    private MeshGUIText _killerApText;
    private string _killerHp = "";
    private string _killerAp = "";
    private Rect _hpIconRect;
    private Rect _apIconRect;
    private Texture2D _hpIcon;
    private Texture2D _apIcon;
    private GUIStyle _buttonGuiStyle;

    #endregion
}

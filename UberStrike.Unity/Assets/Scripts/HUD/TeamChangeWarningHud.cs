using UnityEngine;
using System.Collections;

public class TeamChangeWarningHud : Singleton<TeamChangeWarningHud>
{
    public bool Enabled
    {
        get { return _warningMsg.IsVisible; }
        set
        {
            if (value) _warningMsg.Show();
            else _warningMsg.Hide();
        }
    }

    public bool IsDisplaying
    {
        get { return _isTextDisplaying; }
        private set
        {
            _isTextDisplaying = value;
            if (value)
            {
                _warningMsg.StopFading();
                _isFadingOutText = false;
                _nextHideTime = Time.time + _textDisplayTime;
            }
            else
            {
                _warningMsg.Alpha = 0.0f;
                _isFadingOutText = false;
            }
        }
    }

    public void Draw()
    {
        if (Enabled == false)
        {
            return;
        }
        DrawWarningMsg();
    }

    public void ClearMsg()
    {
        _warningMsg.Text = "";
        IsDisplaying = false;
    }

    public void DisplayWarningMsg(string warningMsg, Color msgColor)
    {
        IsDisplaying = true;

        _warningMsg.Text = warningMsg;
        HudStyleUtility.Instance.SetNoShadowStyle(_warningMsg);
        _warningMsg.Color = msgColor;
        _warningMsg.ShadowColorAnim.Alpha = 0.0f;
    }

    #region Private
    private TeamChangeWarningHud()
    {
        _warningMsg = new MeshGUIText("", HudAssets.Instance.InterparkBitmapFont, TextAnchor.MiddleCenter);
        _animFadeOutTime = 1.0f;
        _textDisplayTime = 2.0f;

        ResetTransform();
        Enabled = true;
    }

    private void ResetTransform()
    {
        _curScaleFactor = 0.45f;
        _warningMsg.Scale = new Vector2(_curScaleFactor, _curScaleFactor);
        _warningMsg.Position = new Vector2(Screen.width / 2, Screen.height * 0.3f);
    }

    private void DrawWarningMsg()
    {
        if (IsDisplaying && Time.time > _nextHideTime)
        {
            if (_isFadingOutText == false)
            {
                _warningMsg.FadeAlphaTo(0.0f, _animFadeOutTime, EaseType.Out);
                _isFadingOutText = true;
            }
            else if (Time.time > _nextHideTime + _animFadeOutTime)
            {
                IsDisplaying = false;
            }
        }
        _warningMsg.Draw();
        _warningMsg.ShadowColorAnim.Alpha = 0.0f;
    }

    private float _curScaleFactor;
    private MeshGUIText _warningMsg;

    private bool _isTextDisplaying;
    private float _textDisplayTime;
    private float _animFadeOutTime;
    private float _nextHideTime;
    private bool _isFadingOutText;
    #endregion
}

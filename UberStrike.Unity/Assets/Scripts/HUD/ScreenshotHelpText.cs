using UnityEngine;
using Cmune.Util;

class ScreenshotHud : Singleton<ScreenshotHud>
{
    public bool Enable
    {
        get
        {
            return _helpText.IsVisible;
        }
        set
        {
            if (value) _helpText.Show();
            else _helpText.Hide();
        }
    }

    public void Draw()
    {
        if (Event.current.type == EventType.keyDown && Event.current.keyCode == KeyCode.P 
            && GameState.CurrentGame.IsMatchRunning && GameState.LocalCharacter.IsAlive)
        {
            HudDrawFlagGroup.Instance.IsScreenshotMode = !HudDrawFlagGroup.Instance.IsScreenshotMode;
        }
        if (Enable != HudDrawFlagGroup.Instance.IsScreenshotMode)
        {
            Enable = HudDrawFlagGroup.Instance.IsScreenshotMode;
        }
        _helpText.Draw();
    }

    private ScreenshotHud()
    {
        _helpText = new MeshGUITextFormat("Screenshot mode\nPress [P] to enable HUD",
            HudAssets.Instance.InterparkBitmapFont, TextAlignment.Right, HudStyleUtility.Instance.SetNoShadowStyle);
        ResetTransform();

        Enable = false;
        CmuneEventHandler.AddListener<ScreenResolutionEvent>(OnScreenResolutionChange);
    }

    private void OnScreenResolutionChange(ScreenResolutionEvent ev)
    {
        ResetTransform();
    }

    private void ResetTransform()
    {
        float scaleFactor = 0.2f;
        _helpText.Scale = new Vector2(scaleFactor, scaleFactor);
        _helpText.LineGap = _helpText.Rect.height * 0.1f;
        _helpText.Position = new Vector2(Screen.width * 0.99f, Screen.height * 0.99f - _helpText.Rect.height);
    }

    private MeshGUITextFormat _helpText;
}

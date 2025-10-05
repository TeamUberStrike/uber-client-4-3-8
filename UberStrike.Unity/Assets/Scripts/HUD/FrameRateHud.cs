using Cmune.Util;
using UnityEngine;

class FrameRateHud : Singleton<FrameRateHud>
{
    public bool Enable
    {
        get
        {
            return _frameRateText.IsVisible;
        }
        set
        {
            if (ApplicationDataManager.BuildType == Cmune.DataCenter.Common.Entities.BuildType.Dev)
            {
                value = false;
            }
            if (value) _frameRateText.Show();
            else _frameRateText.Hide();
        }
    }

    public void Draw()
    {
        string newFrameRateText = ApplicationDataManager.FrameRate; 
        if (_frameRateText.Text != newFrameRateText)
        {
            _frameRateText.Text = newFrameRateText;
        }
        _frameRateText.Draw();
    }

    private FrameRateHud()
    {
        _frameRateText = new MeshGUIText("",
            HudAssets.Instance.InterparkBitmapFont, TextAnchor.UpperRight);

        HudStyleUtility.Instance.SetNoShadowStyle(_frameRateText);
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
        _frameRateText.Scale = new Vector2(scaleFactor, scaleFactor);
        _frameRateText.Position = new Vector2(Screen.width * 0.99f, Screen.height * 0.01f);
    }

    private MeshGUIText _frameRateText;
}

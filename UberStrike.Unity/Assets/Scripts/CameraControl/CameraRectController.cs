using UnityEngine;
using Cmune.Util;

public class CameraWidthChangeEvent
{
    public float Width { get; set; }
}

public class CameraRectController : Singleton<CameraRectController>
{
    public float Width
    {
        get { return _cameraWidthAnim.Value; }
        set { _cameraWidthAnim.Value = value; }
    }

    public void AnimCameraWidth(float destWidth, float time, EaseType easeType)
    {
        _cameraWidthAnim.AnimTo(destWidth, time, easeType);
    }

    #region Private
    private CameraRectController()
    {
        _cameraWidthAnim = new FloatAnim((float oldValue, float newValue) =>
        {
            if (GameState.CurrentSpace != null && GameState.CurrentSpace.Camera != null)
            {
                GameState.CurrentSpace.Camera.rect = new Rect(0.0f, 0.0f, newValue, 1.0f);
                CmuneEventHandler.Route(new CameraWidthChangeEvent()
                {
                    Width = GameState.CurrentSpace.Camera.rect.width
                });
            }
        }, 1.0f);
    }

    private FloatAnim _cameraWidthAnim;
    #endregion
}

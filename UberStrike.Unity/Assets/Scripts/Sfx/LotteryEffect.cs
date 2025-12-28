using UnityEngine;
using System.Collections;

public class LotteryEffect : MonoBehaviour
{
    private const float MAX_DURATION = 2;
    private const float FADE_TIME = 1.5f;

    private float _time = 0;
    private float _alpha = 1;
    private float _cameraAlpha;

    private RenderTexture _renderTexture;

    [SerializeField]
    private Camera _renderCamera;

    private void Awake()
    {
        if (_renderCamera)
        {
            _renderTexture = new RenderTexture(Screen.width, Screen.height, 16, RenderTextureFormat.ARGB32);
            _renderCamera.targetTexture = _renderTexture;
            _cameraAlpha = _renderCamera.backgroundColor.a;
        }
    }

    private void Update()
    {
        if (_time > FADE_TIME)
        {
            _alpha = Mathf.Clamp01(_alpha - Time.deltaTime);
            _renderCamera.backgroundColor.SetAlpha(Mathf.Min(_cameraAlpha, _alpha));
        }

        if (_time > MAX_DURATION)
        {
            GameObject.Destroy(gameObject);

            if (_renderTexture)
            {
                _renderTexture.Release();
            }
        }

        _time += Time.deltaTime;
    }

    private void OnGUI()
    {
        if (_renderTexture)
        {
            GUI.depth = (int)GuiDepth.Sfx;
            GUI.color = new Color(1, 1, 1, _alpha);

            Rect rect = new Rect((Screen.width - _renderTexture.width) / 2, (Screen.height - _renderTexture.height) / 2, _renderTexture.width, _renderTexture.height);
            GUI.DrawTexture(rect, _renderTexture);

            GUI.color = Color.white;
        }
    }
}
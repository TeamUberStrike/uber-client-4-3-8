using UnityEngine;

public class DynamicTexture
{
    private Texture2D _texture;
    private string _url;
    private State _state = 0;
    private float _alpha = 0;

    private enum State
    {
        None = 0,
        Loading,
        Failed,
        Success,
    }

    public float Aspect { get { return _texture != null ? _texture.height / (float)_texture.width : 1; } }

    public DynamicTexture(string url, bool loadNow = false)
    {
        _url = url;
        _texture = new Texture2D(1, 1);

        if (loadNow)
        {
            _state = State.Loading;
            _texture = TextureLoader.Instance.LoadImage(_url);
        }
    }

    public void Draw(Rect rect)
    {
        switch (_state)
        {
            case State.None:
                {
                    _state = State.Loading;
                    _texture = TextureLoader.Instance.LoadImage(_url);
                    break;
                }
            case State.Loading:
                {
                    switch (TextureLoader.Instance.GetState(_texture))
                    {
                        case 1: _state = State.Success; break;
                        case -1: _state = State.Failed; break;
                        case 2: _state = State.Failed; break;
                    }

                    WaitingTexture.Draw(rect.center);
                    break;
                }
            case State.Success:
                {
                    _alpha = Mathf.Lerp(_alpha, 1, Time.deltaTime);
                    float alpha = GUI.enabled ? _alpha : Mathf.Min(_alpha, 0.5f);
                    GUI.color = new Color(1, 1, 1, alpha);
                    GUI.DrawTexture(rect, _texture);
                    GUI.color = Color.white;
                    break;
                }
            case State.Failed:
                {
                    GUI.Label(rect, "N/A", BlueStonez.label_ingamechat);
                    break;
                }
        }
    }
}
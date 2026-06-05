using UnityEngine;

public class DynamicTexture
{
    private Texture2D _texture;
    private string _url;
    private State _state = 0;
    private float _alpha = 0;
    private bool _hasDrawn;

    private enum State
    {
        None = 0,
        Loading,
        Failed,
        Success,
    }

    public float Aspect { get { return _texture != null ? _texture.height / (float)_texture.width : 1; } }

    /// <summary>
    /// True once the image has finished downloading and decoding — queryable WITHOUT calling Draw()
    /// (it reads TextureLoader's live state for our texture). Lets a caller hold a panel hidden until
    /// its prewarmed image is ready, so it appears complete instead of flashing an empty box. Returns
    /// false for a never-loaded/never-preloaded texture and true for a directly-supplied preloaded one.
    /// </summary>
    public bool IsLoaded
    {
        get { return _state == State.Success || TextureLoader.Instance.GetState(_texture) == 1; }
    }

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

    public DynamicTexture(Texture2D preloaded)
    {
        _url = null;
        _texture = preloaded;
        _state = State.Success;
        _alpha = 1;
    }

    /// <summary>
    /// Start the texture download NOW (e.g. during the "Setting Up..." loading screen) instead of
    /// lazily on the first Draw(). TextureLoader caches by URL, so the later Draw() reuses the same
    /// (already downloading/decoded) texture — the panel/list paints filled instead of streaming in
    /// over ~1s on first open. Safe to call repeatedly and before any Draw; no-op without a URL.
    /// </summary>
    public void Preload()
    {
        if (_state != State.None || string.IsNullOrEmpty(_url))
            return;
        _state = State.Loading;
        _texture = TextureLoader.Instance.LoadImage(_url);
    }

    public void Draw(Rect rect)
    {
        // Resolve any pending download to its FINAL state BEFORE drawing. The old code, on the frame a
        // download finished, set Success but still drew one more spinner frame (image appeared the frame
        // after). Resolving first means a finished texture draws its image this same frame — no spinner
        // flash. And if the image was already loaded before its very first Draw (i.e. it was prewarmed
        // during the loading screen), show it SOLID with no fade: the ~0.17s fade only makes sense for an
        // image that pops in while the user is watching, not one that was ready before the panel appeared.
        if (_state == State.None)
        {
            _state = State.Loading;
            _texture = TextureLoader.Instance.LoadImage(_url);
        }
        if (_state == State.Loading)
        {
            switch (TextureLoader.Instance.GetState(_texture))
            {
                case 1: _state = State.Success; if (!_hasDrawn) _alpha = 1f; break;
                case -1: _state = State.Failed; break;
                case 2: _state = State.Failed; break;
            }
        }
        _hasDrawn = true;

        switch (_state)
        {
            case State.Loading:
                WaitingTexture.Draw(rect.center);
                break;
            case State.Success:
                _alpha = Mathf.MoveTowards(_alpha, 1f, Time.deltaTime * 6f);
                float alpha = GUI.enabled ? _alpha : Mathf.Min(_alpha, 0.5f);
                GUI.color = new Color(1, 1, 1, alpha);
                GUI.DrawTexture(rect, _texture);
                GUI.color = Color.white;
                break;
            case State.Failed:
                GUI.Label(rect, "N/A", BlueStonez.label_ingamechat);
                break;
        }
    }
}
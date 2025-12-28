using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextureLoader : Singleton<TextureLoader>
{
    private Dictionary<string, Texture2D> _cache;
    private Dictionary<int, int> _state;
    private Texture2D _fallback;

    private TextureLoader()
    {
        _cache = new Dictionary<string, Texture2D>();
        _state = new Dictionary<int, int>();
        _fallback = CreateDefaultTexture();
    }

    public Texture2D LoadImage(string url, Texture2D placeholder = null)
    {
        Texture2D texture;
        if (!string.IsNullOrEmpty(url))
        {
            if (!_cache.TryGetValue(url, out texture))
            {
                texture = CreatePlaceholder(placeholder);
                _cache[url] = texture;
                MonoRoutine.Start(DownloadTexture(url, texture));
            }
        }
        else
        {
            texture = _fallback;
        }
        return texture;
    }

    #region Private Methods

    private Texture2D CreateDefaultTexture()
    {
        return new Texture2D(1, 1, TextureFormat.RGB24, false);
    }

    public int GetState(Texture2D texture)
    {
        int state;
        if (_state.TryGetValue(texture.GetInstanceID(), out state))
            return state;
        else return -1;
    }

    private IEnumerator DownloadTexture(string url, Texture2D texture)
    {
        using (WWW www = new WWW(url))
        {
            _state[texture.GetInstanceID()] = 0;
            yield return www;

            if (www.isDone && string.IsNullOrEmpty(www.error))
            {
                _state[texture.GetInstanceID()] = 1;
                www.LoadImageIntoTexture(texture);
            }
            else
            {
                _state[texture.GetInstanceID()] = 2;
                Debug.LogError("DownloadTexture url '" + url + "' failed with error: " + www.error);
            }
        }
    }

    private Texture2D CreatePlaceholder(Texture2D placeholder = null)
    {
        Texture2D texture;
        if (placeholder != null)
        {
            texture = GameObject.Instantiate(placeholder) as Texture2D;
        }
        else
        {
            texture = CreateDefaultTexture();
        }
        return texture;
    }

    #endregion
}
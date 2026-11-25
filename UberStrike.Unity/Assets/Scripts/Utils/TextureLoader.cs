using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextureLoader : Singleton<TextureLoader>
{
    private Dictionary<string, Texture2D> _cache;
    private Dictionary<int, int> _state;
    private HashSet<string> _failedUrls; // blacklist so dead URLs don't retry
    private Texture2D _fallback;

    private TextureLoader()
    {
        _cache = new Dictionary<string, Texture2D>();
        _state = new Dictionary<int, int>();
        _failedUrls = new HashSet<string>();
        _fallback = CreateDefaultTexture();
    }

    public Texture2D LoadImage(string url, Texture2D placeholder = null)
    {
        Texture2D texture;
        if (!string.IsNullOrEmpty(url))
        {
            // Skip retry for URLs we already saw fail this session — the shop
            // tab used to spawn dozens of coroutines hammering dead
            // content.cmune.com URLs, each logging a Debug.LogError on failure
            // that cascaded through DebugConsoleManager.SendExceptionReport.
            if (_failedUrls.Contains(url))
            {
                return _fallback;
            }
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
        // Creating the WWW/UnityWebRequest can throw synchronously (e.g.
        // InvalidOperationException "Insecure connection not allowed" on
        // release builds, or UriFormatException on malformed URLs). If that
        // escapes the coroutine it surfaces as an unhandled exception in
        // OnGUI, so degrade to the placeholder instead.
        WWW www;
        try
        {
            www = new WWW(url);
        }
        catch (System.Exception ex)
        {
            _state[texture.GetInstanceID()] = 2;
            _failedUrls.Add(url);
            if (Debug.isDebugBuild)
                Debug.LogWarning("[TextureLoader] Failed to start download for " + url + " (" + ex.Message + ")");
            yield break;
        }

        using (www)
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
                _failedUrls.Add(url);
                // Downgraded from Debug.LogError to Debug.LogWarning + no-op in
                // the exception-report pipeline. The shop used to burst ~50-100
                // LogErrors at tab-open, each routed through DebugConsoleManager
                // which added significant frame overhead.
                if (Debug.isDebugBuild)
                    Debug.LogWarning("[TextureLoader] URL unreachable (suppressed after first): " + url);
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
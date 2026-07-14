using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class TextureLoader : Singleton<TextureLoader>
{
    private Dictionary<string, Texture2D> _cache;
    private Dictionary<int, int> _state;
    private HashSet<string> _failedUrls; // blacklist so dead URLs don't retry
    private Texture2D _fallback;
    private string _diskCacheDir;        // null => persistent disk cache disabled (couldn't create dir)

    private TextureLoader()
    {
        _cache = new Dictionary<string, Texture2D>();
        _state = new Dictionary<int, int>();
        _failedUrls = new HashSet<string>();
        _fallback = CreateDefaultTexture();

        // On-disk image cache. The in-memory _cache only lives for the session, so every cold launch used
        // to re-download every CDN image (promo, map icons, shop art) — which is the residual ~0.5s
        // "Weekly Special" first-open flicker: on the first lobby open of a launch the promo is still
        // streaming from the CDN. With a disk cache, every launch AFTER the first decodes those images from
        // local storage in well under a frame (see DownloadTexture), so the IsLoaded gate + the login
        // loading-screen wait both pass instantly and the lobby reveals with the panel already filled.
        //
        // We use temporaryCachePath (iOS: Library/Caches) — the correct home for re-downloadable data:
        // it's NOT backed up to iCloud and the OS may purge it under storage pressure (worst case = one
        // re-download). persistentDataPath (Documents) would be wrong here — backed up + can draw App
        // Store review attention for storing non-user, re-downloadable content.
        try
        {
            _diskCacheDir = Path.Combine(Application.temporaryCachePath, "imgcache");
            if (!Directory.Exists(_diskCacheDir))
                Directory.CreateDirectory(_diskCacheDir);
        }
        catch
        {
            _diskCacheDir = null; // read-only / sandboxed FS — fall back to network-only, never fatal
        }
    }

    // cacheToDisk: persist + reuse the decoded image across launches (default). Pass false for sources
    // whose content can change behind a STABLE url — e.g. Facebook friend avatars — where a disk hit
    // would show a stale picture. Such sources stay network-only (in-session memory cache still applies).
    public Texture2D LoadImage(string url, Texture2D placeholder = null, bool cacheToDisk = true)
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
                MonoRoutine.Start(DownloadTexture(url, texture, cacheToDisk));
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

    private IEnumerator DownloadTexture(string url, Texture2D texture, bool cacheToDisk)
    {
        int id = texture.GetInstanceID();
        _state[id] = 0;

        // 1) Disk-cache hit: decode locally and skip the network entirely. This runs synchronously
        //    (StartCoroutine executes up to the first yield in-frame), so on every launch after the first
        //    GetState() returns 1 the moment LoadImage() returns — the promo / map icons paint filled with
        //    no first-open streaming. Tiny images, so the main-thread decode is cheap; a corrupt/partial
        //    file just falls through to a fresh download below (and overwrites it). A null cachePath
        //    (caching disabled, or cacheToDisk:false) disables both the read here and the write below.
        string cachePath = cacheToDisk ? GetCachePath(url) : null;
        if (cachePath != null && File.Exists(cachePath) && TryLoadFromDisk(cachePath, texture))
        {
            _state[id] = 1;
            yield break;
        }

        // 2) Cache miss (or first-ever launch): download from the CDN as before, then persist the raw
        //    bytes so the NEXT launch takes path (1). Cache key is the URL; this assumes a URL's content
        //    is immutable (true here — the weekly promo gets a fresh URL each week, map/shop art is static
        //    per item). Old promo files accumulate slowly (~1/week, a few KB each); a size/age prune could
        //    be added if that ever matters.
        using (WWW www = new WWW(url))
        {
            yield return www;

            if (www.isDone && string.IsNullOrEmpty(www.error))
            {
                _state[id] = 1;
                www.LoadImageIntoTexture(texture);
                WriteDiskCache(cachePath, www.bytes);
            }
            else
            {
                _state[id] = 2;
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

    // Stable per-URL cache filename. string.GetHashCode() isn't stable across runs/platforms under
    // IL2CPP, so hash the URL ourselves (FNV-1a 64-bit — ample for the handful of image URLs in play).
    private string GetCachePath(string url)
    {
        if (_diskCacheDir == null || string.IsNullOrEmpty(url))
            return null;
        return Path.Combine(_diskCacheDir, Fnv1aHex(url) + ".img");
    }

    private static string Fnv1aHex(string s)
    {
        ulong hash = 14695981039346656037UL; // FNV offset basis
        for (int i = 0; i < s.Length; i++)
        {
            hash ^= s[i];
            hash *= 1099511628211UL;          // FNV prime
        }
        return hash.ToString("x16");
    }

    private bool TryLoadFromDisk(string path, Texture2D texture)
    {
        try
        {
            byte[] bytes = File.ReadAllBytes(path);
            return bytes != null && bytes.Length > 0 && texture.LoadImage(bytes);
        }
        catch
        {
            return false; // unreadable/corrupt — caller re-fetches from the network
        }
    }

    private void WriteDiskCache(string path, byte[] bytes)
    {
        if (path == null || bytes == null || bytes.Length == 0)
            return;
        try
        {
            // Write to a temp file then move into place, so a crash/kill mid-write can't leave a
            // half-written image that a later launch would decode as corrupt.
            string tmp = path + ".tmp";
            File.WriteAllBytes(tmp, bytes);
            if (File.Exists(path))
                File.Delete(path);
            File.Move(tmp, path);
        }
        catch
        {
            // Disk full / read-only / sandboxed — caching is best-effort, never fatal.
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

using System;
using System.Collections;
using UnityEngine;

public static class CacheManager
{
    public static bool IsAuthorized { get; private set; }

    static CacheManager()
    {
        IsAuthorized = false;
    }

    public static bool RunAuthorization(string license)
    {
        IsAuthorized = false;

        if (!string.IsNullOrEmpty(license))
        {
            string product = string.Empty;
            string domain = string.Empty;
            long size = -1;
            int expiration = -1;
            string sig = string.Empty;

            string[] licenseData = license.Split(' ');

            if (licenseData.Length >= 4)
            {
                product = licenseData[0];
                domain = licenseData[1];
                size = int.Parse(licenseData[2]);
                sig = licenseData[3];
            }

            if (licenseData.Length == 5)
                expiration = int.Parse(licenseData[4]);

            // Caching.Authorize was removed in newer Unity versions
            // Modern Unity handles caching authorization internally
            IsAuthorized = true;
        }

        return IsAuthorized;
    }

    public static IEnumerator LoadAssetBundle(string url, Action<float> progress)
    {
        WWW www;

        if (Application.platform == RuntimePlatform.WebGLPlayer && IsAuthorized)
            www = WWW.LoadFromCacheOrDownload(url, 1);
        else
            www = new WWW(url);

        while (!www.isDone)
        {
            if (progress != null) progress(www.progress);
            yield return new WaitForEndOfFrame();
        }

        // Initialize the Assetbundle so it becomes available as a Scene
        if (string.IsNullOrEmpty(www.error))
        {
#pragma warning disable 0219
            AssetBundle assetBundle = www.assetBundle;
#pragma warning restore
        }
        else
        {
            Debug.LogError("WWW Error when calling URL=" + url + "; ERROR=" + www.error);
        }

        // Clean up the WWW since we already initialized the AssetBundle
        www.Dispose();
        www = null;

        if (progress != null) progress(1);
    }
}
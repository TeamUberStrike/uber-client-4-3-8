using UnityEngine;
using System.Collections;

public class PreloaderUtil
{
    public static void StartQuitOnLoadError()
    {
        Application.Quit();
    }

    public static string GetMainSceneUrl()
    {
        string url = string.Empty;

        switch (Application.platform)
        {
            case RuntimePlatform.OSXPlayer:
                {
                    url = "file://" + Application.dataPath + "/Data/" + ApplicationDataManager.MainFilename + ".unity3d";
                    break;
                }
            case RuntimePlatform.WindowsPlayer:
                {
                    url = "file://" + Application.dataPath + "/" + ApplicationDataManager.MainFilename + ".unity3d";
                    break;
                }
            case RuntimePlatform.WebGLPlayer:
                {
                    // Modern WebGL builds host index.html next to UberStrikeMain.unity3d,
                    // so resolve the bundle from the directory of the hosting page.
                    string pageUrl = Application.absoluteURL;
                    int mark = pageUrl.IndexOfAny(new char[] { '?', '#' });
                    if (mark >= 0) pageUrl = pageUrl.Substring(0, mark);
                    int slash = pageUrl.LastIndexOf('/');
                    string baseUrl = (slash >= 0) ? pageUrl.Substring(0, slash + 1) : pageUrl;
                    url = baseUrl + ApplicationDataManager.MainFilename + ".unity3d";
                    break;
                }
        }

        return url;
    }
}

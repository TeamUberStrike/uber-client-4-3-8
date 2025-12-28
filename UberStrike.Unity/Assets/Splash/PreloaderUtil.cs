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
            case RuntimePlatform.OSXWebPlayer:
            case RuntimePlatform.WindowsWebPlayer:
                {
                    url = Application.absoluteURL.Replace(ApplicationDataManager.HeaderFilename, ApplicationDataManager.MainFilename);
                    break;
                }
        }

        return url;
    }
}

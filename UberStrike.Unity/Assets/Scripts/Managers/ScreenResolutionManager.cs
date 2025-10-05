using System.Collections.Generic;
using UnityEngine;

public static class ScreenResolutionManager
{
    #region Fields

    private static List<Resolution> resolutions = new List<Resolution>();

    #endregion

    #region Properties

    private static bool IsHighestResolution
    {
        get { return CurrentResolutionIndex == resolutions.Count - 1; }
    }

    //private static bool IsOneMinusHighestResolution
    //{
    //    get { return CurrentResolutionIndex == resolutions.Count - 2; }
    //}

    public static List<Resolution> Resolutions
    {
        get { return resolutions; }
    }

    public static int InitialResolutionIndex
    {
        // Get the middle resolution (note, this should not be called when the app is already running)
        get { return resolutions.Count - 1; }
    }

    public static int CurrentResolutionIndex
    {
        get { return resolutions.FindIndex(r => r.width == Screen.width && r.height == Screen.height); }
    }

    public static bool IsFullScreen
    {
        get { return Screen.fullScreen; }
        set
        {
            // If we go into windowed mode, make sure we are one minus native resolution (bottom of the game off the screen)
            if (!Application.isWebPlayer && value == false && IsHighestResolution)
            {
                SetTwoMinusMaxResolution();
            }
            else
            {
                Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, value);
            }
            
            ApplicationDataManager.ApplicationOptions.IsFullscreen = value;
            
            ApplicationDataManager.ApplicationOptions.SaveApplicationOptions();

            // if (Application.isWebPlayer)
            //else
            //{
            //if (resolutions != null && resolutions.Count > 0)
            //{
            //    // If we are going full screen and we are one minus highest res, just put us in native res in full screen
            //    if (value && IsOneMinusHighestResolution)
            //    {
            //        int newResIndex = resolutions.Count - 1;
            //        Screen.SetResolution(resolutions[newResIndex].width, resolutions[newResIndex].height, value);
            //    }
            //    else
            //    {
            //        int newResIndex = Mathf.Clamp(CurrentResolutionIndex, 0, resolutions.Count - 1);
            //        Screen.SetResolution(resolutions[newResIndex].width, resolutions[newResIndex].height, value);
            //    }
            //}
            // }
        }
    }

    #endregion

    static ScreenResolutionManager()
    {
        foreach (Resolution resolution in Screen.resolutions)
        {
            if (resolution.width > 800) resolutions.Add(resolution);
        }

        if (resolutions.Count == 0)
        {
            resolutions.Add(Screen.currentResolution);
        }
    }

    public static void SetResolution(int index, bool fullscreen)
    {
        int max = resolutions.Count - 1;

        int newResIndex = Mathf.Clamp(index, 0, max);

        if (!Application.isWebPlayer && newResIndex == max && !fullscreen)
            fullscreen = true;

        if (newResIndex >= 0 && newResIndex < resolutions.Count)
        {
            Screen.SetResolution(resolutions[newResIndex].width, resolutions[newResIndex].height, fullscreen);

            // Update the Application Player Prefs to reflect the selected resolution
            ApplicationDataManager.ApplicationOptions.ScreenResolution = newResIndex;
        }
    }

    /// <summary>
    /// Sets the client in a windowed resolution one minus the current max resolution of the screen. Use this for a kind of maxmized windowed mode. Only useable on the desktop client.
    /// </summary>
    public static void SetTwoMinusMaxResolution()
    {
        if (Application.isWebPlayer)
        {
            Debug.LogError("SetOneMinusMaxResolution() should only be called from the desktop client");
            return;
        }

        if (resolutions.Count > 2)  // We have two or more resolutions to choose from, choose two minus the max
        {
            Vector2 desiredResolution = new Vector2(resolutions[resolutions.Count - 3].width, resolutions[resolutions.Count - 3].height);
            Screen.SetResolution((int)desiredResolution.x, (int)desiredResolution.y, false);
        }
        else if (resolutions.Count == 2) // We have only two resolutions, choose one minus the max
        {
            Vector2 desiredResolution = new Vector2(resolutions[1].width, resolutions[1].height);
            Screen.SetResolution((int)desiredResolution.x, (int)desiredResolution.y, false);
        }
        else if (resolutions.Count == 1) // We have only one resolution, so choose it
        {
            Vector2 desiredResolution = new Vector2(resolutions[0].width, resolutions[0].height);
            Screen.SetResolution((int)desiredResolution.x, (int)desiredResolution.y, false);
        }
        else // Something went wrong, we have no supported resolutions
        {
            Debug.LogError("ScreenResolutionManager: Screen.resolutions does not contain any supported resolutions.");
        }
    }

    public static void SetFullScreenMaxResolution()
    {
        if (resolutions.Count == 0)
        {
            Debug.LogError("SetFullScreenMaxResolution: No suitable resolution available in the Resolutions array.");
            return;
        }

        // Get the max resolution of the screen
        int desiredResolutionIndex = resolutions.Count - 1;
        Vector2 desiredResolution = new Vector2(resolutions[desiredResolutionIndex].width, resolutions[desiredResolutionIndex].height);

        // If we're not already in Full Screen, do it!
        if (!Screen.fullScreen)
        {
            Screen.SetResolution((int)desiredResolution.x, (int)desiredResolution.y, true);

            // Update the Application Player Prefs to reflect the selected resolution
            ApplicationDataManager.ApplicationOptions.ScreenResolution = desiredResolutionIndex;
        }
    }

    public static void DecreaseResolution()
    {
        SetResolution(CurrentResolutionIndex - 1, Screen.fullScreen);
    }

    public static void IncreaseResolution()
    {
        SetResolution(CurrentResolutionIndex + 1, Screen.fullScreen);
    }
}

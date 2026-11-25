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
            if (Application.platform != RuntimePlatform.WebGLPlayer && value == false && IsHighestResolution)
            {
                SetTwoMinusMaxResolution();
            }
            else
            {
                // Keep the resolution picked in the options instead of jumping to the desktop resolution.
                Resolution saved;
                if (TryGetSavedResolution(out saved))
                    Screen.SetResolution(saved.width, saved.height, value);
                else
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
        // Unity 2022's Screen.resolutions returns one entry per unique
        // (width, height, refreshRate) combo. On monitors that expose
        // multiple refresh rates (60/74/144 Hz, etc.) this produces
        // many duplicates at the same width×height — issue #46 in the
        // public repo: "up to 9 same" 1920×1080 entries, only one
        // clickable, wrong one occasionally toggled. Unity 3.5.5
        // effectively had one rate per mode so no dedupe was needed.
        // Dedupe by (width, height); keep the first (highest refresh
        // rate on Windows, because Screen.resolutions is sorted
        // ascending by size then descending by refresh rate at same
        // size — the first occurrence is the one the user will feel
        // is "correct" when Screen.SetResolution picks a refresh rate
        // Unity defaults to max available anyway).
        HashSet<long> seen = new HashSet<long>();
        foreach (Resolution resolution in Screen.resolutions)
        {
            if (resolution.width <= 800) continue;
            long key = ((long)resolution.width << 32) | (uint)resolution.height;
            if (!seen.Add(key)) continue;
            resolutions.Add(resolution);
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

        if (Application.platform != RuntimePlatform.WebGLPlayer && newResIndex == max && !fullscreen)
            fullscreen = true;

        if (newResIndex >= 0 && newResIndex < resolutions.Count)
        {
            Screen.SetResolution(resolutions[newResIndex].width, resolutions[newResIndex].height, fullscreen);

            // Update the Application Player Prefs to reflect the selected resolution
            SaveResolution(newResIndex, fullscreen);
        }
    }

    // Remember the picked resolution (by size, the list index changes with the monitor) and the
    // fullscreen state, so RestoreSavedResolution can put them back on the next start.
    private static void SaveResolution(int index, bool fullscreen)
    {
        ApplicationOptions options = ApplicationDataManager.ApplicationOptions;
        options.ScreenResolution = index;
        options.ScreenWidth = resolutions[index].width;
        options.ScreenHeight = resolutions[index].height;
        options.IsFullscreen = fullscreen;
        options.SaveApplicationOptions();
    }

    private static bool TryGetSavedResolution(out Resolution saved)
    {
        ApplicationOptions options = ApplicationDataManager.ApplicationOptions;
        int index = options != null ? resolutions.FindIndex(r => r.width == options.ScreenWidth && r.height == options.ScreenHeight) : -1;
        saved = index >= 0 ? resolutions[index] : default(Resolution);
        return index >= 0;
    }

    /// <summary>
    /// Desktop only, at startup: Unity starts a fullscreen player at the desktop resolution, so put
    /// back the resolution and fullscreen state picked in the options. -screen-* launch arguments win.
    /// </summary>
    public static void RestoreSavedResolution()
    {
        if (Application.platform != RuntimePlatform.WindowsPlayer && Application.platform != RuntimePlatform.OSXPlayer && Application.platform != RuntimePlatform.LinuxPlayer)
            return;
        foreach (string arg in System.Environment.GetCommandLineArgs())
            if (arg.StartsWith("-screen-")) return;

        Resolution saved;
        if (!TryGetSavedResolution(out saved)) return;

        bool fullscreen = ApplicationDataManager.ApplicationOptions.IsFullscreen;
        // A window at the desktop size hangs off the screen; Unity's own saved window size is used instead.
        if (!fullscreen && saved.width == resolutions[resolutions.Count - 1].width && saved.height == resolutions[resolutions.Count - 1].height)
            return;
        if (Screen.width != saved.width || Screen.height != saved.height || Screen.fullScreen != fullscreen)
        {
            Debug.Log("Screen: restoring " + saved.width + "x" + saved.height + (fullscreen ? " fullscreen" : " windowed") + " (started at " + Screen.width + "x" + Screen.height + (Screen.fullScreen ? " fullscreen)" : " windowed)"));
            Screen.SetResolution(saved.width, saved.height, fullscreen);
        }
    }

    /// <summary>
    /// Desktop only, on quit: remember the screen as it is (also after Unity's own Alt+Enter), so the
    /// next start comes back the same.
    /// </summary>
    public static void SaveCurrentScreen()
    {
        if (Application.platform != RuntimePlatform.WindowsPlayer && Application.platform != RuntimePlatform.OSXPlayer && Application.platform != RuntimePlatform.LinuxPlayer)
            return;
        if (ApplicationDataManager.ApplicationOptions == null)
            return;

        int index = resolutions.FindIndex(r => r.width == Screen.width && r.height == Screen.height);
        if (index >= 0)
        {
            SaveResolution(index, Screen.fullScreen);
        }
        else
        {
            ApplicationDataManager.ApplicationOptions.IsFullscreen = Screen.fullScreen;
            ApplicationDataManager.ApplicationOptions.SaveApplicationOptions();
        }
    }

    /// <summary>
    /// Sets the client in a windowed resolution one minus the current max resolution of the screen. Use this for a kind of maxmized windowed mode. Only useable on the desktop client.
    /// </summary>
    public static void SetTwoMinusMaxResolution()
    {
        if (Application.platform == RuntimePlatform.WebGLPlayer)
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
            SaveResolution(desiredResolutionIndex, true);
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
